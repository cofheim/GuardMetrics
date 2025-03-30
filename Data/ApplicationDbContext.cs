using GuardMetrics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;

namespace GuardMetrics.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<ProcessMetric> ProcessMetrics { get; set; } = null!;
    public DbSet<NetworkMetric> NetworkMetrics { get; set; } = null!;
    public DbSet<SecurityMetric> SecurityMetrics { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("Host=localhost") && !IsDatabaseAvailable())
            {
                // Если PostgreSQL недоступен, использовать SQLite
                optionsBuilder.UseSqlite("Data Source=guardmetrics.db");
            }
        }
    }

    private bool IsDatabaseAvailable()
    {
        try
        {
            // Простая проверка подключения к PostgreSQL
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            connection.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Индексы для улучшения запросов
        modelBuilder.Entity<ProcessMetric>()
            .HasIndex(p => p.Timestamp);

        modelBuilder.Entity<NetworkMetric>()
            .HasIndex(n => n.Timestamp);

        // Конвертер для словаря метаданных
        modelBuilder.Entity<SecurityMetric>()
            .Property(e => e.Metadata)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }) : null,
                v => v != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions { WriteIndented = false }) : null);

        // Настраиваем модели
        modelBuilder.Entity<ProcessMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessName).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });

        modelBuilder.Entity<NetworkMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessName).IsRequired();
            entity.Property(e => e.LocalAddress).IsRequired();
            entity.Property(e => e.Protocol).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });

        modelBuilder.Entity<SecurityMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentId).IsRequired();
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });
    }
} 