using GuardMetrics.Data;
using GuardMetrics.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuardMetrics.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(ApplicationDbContext context, ILogger<MetricsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SecurityMetric>>> GetMetrics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? source = null,
        [FromQuery] MetricType? type = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var query = _context.SecurityMetrics.AsQueryable();

            if (from.HasValue)
                query = query.Where(m => m.Timestamp >= from.Value);

            if (to.HasValue)
                query = query.Where(m => m.Timestamp <= to.Value);

            if (!string.IsNullOrEmpty(source))
                query = query.Where(m => m.Source == source);

            if (type.HasValue)
                query = query.Where(m => m.Type == type.Value);

            var metrics = await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            _logger.LogInformation("Получено {Count} метрик", metrics.Count);
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении метрик");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SecurityMetric>> CreateMetric(SecurityMetric metric)
    {
        try
        {
            if (metric.Timestamp == default)
                metric.Timestamp = DateTime.UtcNow;

            if (string.IsNullOrEmpty(metric.AgentId))
                metric.AgentId = User.Identity?.Name ?? "unknown";

            _context.SecurityMetrics.Add(metric);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создана новая метрика типа {Type} от агента {AgentId}", 
                metric.Type, metric.AgentId);

            return CreatedAtAction(nameof(GetMetrics), new { id = metric.Id }, metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании метрики");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult> CreateMetricsBatch(List<SecurityMetric> metrics)
    {
        try
        {
            var now = DateTime.UtcNow;
            var agentId = User.Identity?.Name ?? "unknown";

            foreach (var metric in metrics)
            {
                if (metric.Timestamp == default)
                    metric.Timestamp = now;

                if (string.IsNullOrEmpty(metric.AgentId))
                    metric.AgentId = agentId;

                _context.SecurityMetrics.Add(metric);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Создано {Count} метрик пакетно от агента {AgentId}", 
                metrics.Count, agentId);

            return Ok(new { count = metrics.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании пакета метрик");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
} 