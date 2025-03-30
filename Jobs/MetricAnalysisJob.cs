using GuardMetrics.Data;
using GuardMetrics.Services;
using Microsoft.EntityFrameworkCore;

namespace GuardMetrics.Jobs;

public class MetricAnalysisJob
{
    private readonly ApplicationDbContext _context;
    private readonly IMetricAnalyzer _analyzer;
    private readonly ILogger<MetricAnalysisJob> _logger;

    public MetricAnalysisJob(
        ApplicationDbContext context,
        IMetricAnalyzer analyzer,
        ILogger<MetricAnalysisJob> logger)
    {
        _context = context;
        _analyzer = analyzer;
        _logger = logger;
    }

    public async Task AnalyzeLatestMetricsAsync()
    {
        try
        {
            // Получаем метрики за последние 5 минут
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            // Анализируем процессы
            var processes = await _context.ProcessMetrics
                .Where(p => p.Timestamp >= cutoffTime)
                .ToListAsync();

            foreach (var process in processes)
            {
                if (await _analyzer.IsProcessSuspicious(process))
                {
                    _logger.LogWarning("Suspicious process detected: {ProcessName}", process.ProcessName);
                    
                    // Проверяем хэш в VirusTotal
                    if (await _analyzer.CheckProcessHashWithVirusTotal(process.FileHash))
                    {
                        _logger.LogCritical("Malicious process detected: {ProcessName}", process.ProcessName);
                    }
                }
            }

            // Анализируем сетевую активность
            var networkMetrics = await _context.NetworkMetrics
                .Where(n => n.Timestamp >= cutoffTime)
                .ToListAsync();

            foreach (var metric in networkMetrics)
            {
                if (await _analyzer.IsNetworkActivitySuspicious(metric))
                {
                    _logger.LogWarning("Suspicious network activity detected: {ProcessName} {RemoteAddress}:{RemotePort}",
                        metric.ProcessName, metric.RemoteAddress, metric.RemotePort);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during metric analysis");
        }
    }
} 