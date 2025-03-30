using GuardMetrics.Models;
using MathNet.Numerics.Statistics;
using Microsoft.ML;
using StackExchange.Redis;

namespace GuardMetrics.Services;

public class MetricAnalyzer : IMetricAnalyzer
{
    private readonly AnalysisSettings _settings;
    private readonly VirusTotalService _virusTotalService;
    private readonly TelegramNotificationService _telegramService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<MetricAnalyzer> _logger;
    private readonly AnomalyDetectionService _anomalyDetection;

    public MetricAnalyzer(
        AnalysisSettings settings,
        VirusTotalService virusTotalService,
        TelegramNotificationService telegramService,
        IConnectionMultiplexer redis,
        AnomalyDetectionService anomalyDetection,
        ILogger<MetricAnalyzer> logger)
    {
        _settings = settings;
        _virusTotalService = virusTotalService;
        _telegramService = telegramService;
        _redis = redis;
        _anomalyDetection = anomalyDetection;
        _logger = logger;
    }

    public async Task<bool> IsProcessSuspicious(ProcessMetric metric)
    {
        // Проверка по имени процесса
        if (_settings.SuspiciousProcessNames.Any(name => 
            metric.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _telegramService.SendThreatAlertAsync(
                metric.ProcessName,
                "Подозрительное имя процесса",
                0.8,
                "Рекомендуется проверить процесс и его происхождение");
            return true;
        }

        // Проверка нагрузки на CPU с помощью ML.NET
        if (metric.CpuUsagePercent > _settings.CpuUsageThreshold)
        {
            var (isAnomaly, score) = _anomalyDetection.DetectCpuAnomaly((float)metric.CpuUsagePercent);
            if (isAnomaly)
            {
                await _telegramService.SendAnomalyAlertAsync(
                    "CPU",
                    $"Аномальная нагрузка на CPU: {metric.CpuUsagePercent}%",
                    score);
                return true;
            }
        }

        // Проверка использования памяти
        var memoryGb = metric.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0);
        var (isMemoryAnomaly, memoryScore) = _anomalyDetection.DetectMemoryAnomaly((float)memoryGb);
        if (isMemoryAnomaly)
        {
            await _telegramService.SendAnomalyAlertAsync(
                "Память",
                $"Аномальное использование памяти: {memoryGb:F2} GB",
                memoryScore);
            return true;
        }

        return false;
    }

    public async Task<bool> IsNetworkActivitySuspicious(NetworkMetric metric)
    {
        // Проверка подозрительных портов
        if (_settings.SuspiciousNetworkPorts.Contains(metric.RemotePort?.ToString()))
        {
            await _telegramService.SendThreatAlertAsync(
                metric.ProcessName,
                "Подозрительное сетевое подключение",
                0.7,
                $"Обнаружено подключение к порту {metric.RemotePort}, который часто используется для майнинга");
            return true;
        }

        // Проверка аномального объема трафика с помощью ML.NET
        var totalTrafficMb = (metric.BytesSent + metric.BytesReceived) / (1024.0 * 1024.0);
        var (isTrafficAnomaly, trafficScore) = _anomalyDetection.DetectNetworkAnomaly((float)totalTrafficMb);
        if (isTrafficAnomaly)
        {
            await _telegramService.SendAnomalyAlertAsync(
                "Сеть",
                $"Аномальный объем трафика: {totalTrafficMb:F2} MB",
                trafficScore);
            return true;
        }

        return false;
    }

    public async Task<double> CalculateProcessAnomalyScore(ProcessMetric metric)
    {
        var (isAnomaly, score) = _anomalyDetection.DetectCpuAnomaly((float)metric.CpuUsagePercent);
        return score;
    }

    public async Task<double> CalculateNetworkAnomalyScore(NetworkMetric metric)
    {
        var totalTrafficMb = (metric.BytesSent + metric.BytesReceived) / (1024.0 * 1024.0);
        var (isAnomaly, score) = _anomalyDetection.DetectNetworkAnomaly((float)totalTrafficMb);
        return score;
    }

    public async Task<bool> CheckProcessHashWithVirusTotal(string fileHash)
    {
        var (isMalicious, detections) = await _virusTotalService.CheckFileHashAsync(fileHash);
        return detections >= _settings.MinVirusTotalDetections;
    }

    private async Task UpdateMetricHistory(string key, float value)
    {
        var db = _redis.GetDatabase();
        var history = await db.StringGetAsync(key);
        
        var values = history.HasValue 
            ? history.ToString().Split(',').Select(float.Parse).ToList() 
            : new List<float>();

        values.Add(value);
        if (values.Count > 60) // Храним историю за последний час
        {
            values.RemoveAt(0);
        }

        await db.StringSetAsync(key, string.Join(",", values));
    }
} 