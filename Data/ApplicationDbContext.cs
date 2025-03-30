using GuardMetrics.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GuardMetrics.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProcessMetric> ProcessMetrics { get; set; } = null!;
    public DbSet<NetworkMetric> NetworkMetrics { get; set; } = null!;
    public DbSet<SecurityMetric> SecurityMetrics { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessMetric>()
            .HasIndex(p => p.Timestamp);

        modelBuilder.Entity<NetworkMetric>()
            .HasIndex(n => n.Timestamp);

        // Настройка для сохранения Dictionary как JSON
        modelBuilder.Entity<SecurityMetric>()
            .Property(m => m.Metadata)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }) : null,
                v => v != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions { WriteIndented = false }) : null);
    }
} 