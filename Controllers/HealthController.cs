using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using GuardMetrics.Data;
using Microsoft.EntityFrameworkCore;

namespace GuardMetrics.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ApplicationDbContext _dbContext;

    public class HealthStatus
    {
        public string Status { get; set; } = "Unhealthy";
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ComponentHealth
    {
        public string Status { get; set; } = "Unhealthy";
        public string? Message { get; set; }
    }

    public HealthController(
        ILogger<HealthController> logger,
        IConnectionMultiplexer redis,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _redis = redis;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<HealthStatus>> Get()
    {
        var health = new HealthStatus();
        var isHealthy = true;

        try
        {
            // Проверка Redis
            var redisHealth = await CheckRedisHealthAsync();
            health.Components["redis"] = redisHealth;
            isHealthy &= redisHealth.Status == "Healthy";

            // Проверка PostgreSQL
            var dbHealth = await CheckDatabaseHealthAsync();
            health.Components["postgresql"] = dbHealth;
            isHealthy &= dbHealth.Status == "Healthy";

            health.Status = isHealthy ? "Healthy" : "Unhealthy";
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            health.Status = "Unhealthy";
            health.Components["error"] = new ComponentHealth 
            { 
                Status = "Unhealthy",
                Message = "Internal error during health check"
            };
            return StatusCode(500, health);
        }
    }

    private async Task<ComponentHealth> CheckRedisHealthAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var ping = await db.PingAsync();
            
            return new ComponentHealth
            {
                Status = "Healthy",
                Message = $"Redis responded in {ping.TotalMilliseconds}ms"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return new ComponentHealth
            {
                Status = "Unhealthy",
                Message = ex.Message
            };
        }
    }

    private async Task<ComponentHealth> CheckDatabaseHealthAsync()
    {
        try
        {
            // Проверяем возможность подключения к базе данных
            await _dbContext.Database.CanConnectAsync();
            
            return new ComponentHealth
            {
                Status = "Healthy",
                Message = "Successfully connected to PostgreSQL"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL health check failed");
            return new ComponentHealth
            {
                Status = "Unhealthy",
                Message = ex.Message
            };
        }
    }
} 