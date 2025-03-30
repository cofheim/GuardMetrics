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
        try
        {
            if (metric == null)
            {
                _logger.LogWarning("Получена пустая метрика процесса");
                return false;
            }

            // Проверка по имени процесса
            if (_settings.SuspiciousProcessNames.Any(name => 
                metric.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Обнаружен подозрительный процесс: {ProcessName}", metric.ProcessName);
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
                    _logger.LogWarning("Обнаружена аномальная нагрузка на CPU: {CpuUsage}%, оценка: {Score}", 
                        metric.CpuUsagePercent, score);
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
                _logger.LogWarning("Обнаружено аномальное использование памяти: {MemoryUsage} GB, оценка: {Score}", 
                    memoryGb.ToString("F2"), memoryScore);
                await _telegramService.SendAnomalyAlertAsync(
                    "Память",
                    $"Аномальное использование памяти: {memoryGb:F2} GB",
                    memoryScore);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе подозрительности процесса {ProcessName}", 
                metric?.ProcessName ?? "неизвестный");
            return false;
        }
    }

    public async Task<bool> IsNetworkActivitySuspicious(NetworkMetric metric)
    {
        try
        {
            if (metric == null)
            {
                _logger.LogWarning("Получена пустая сетевая метрика");
                return false;
            }

            // Проверка подозрительных портов
            if (_settings.SuspiciousNetworkPorts.Contains(metric.RemotePort?.ToString()))
            {
                _logger.LogWarning("Обнаружено подозрительное сетевое подключение к порту {Port}", metric.RemotePort);
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
                _logger.LogWarning("Обнаружен аномальный объем трафика: {Traffic} MB, оценка: {Score}", 
                    totalTrafficMb.ToString("F2"), trafficScore);
                await _telegramService.SendAnomalyAlertAsync(
                    "Сеть",
                    $"Аномальный объем трафика: {totalTrafficMb:F2} MB",
                    trafficScore);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при анализе подозрительности сетевой активности {ProcessName}", 
                metric?.ProcessName ?? "неизвестный");
            return false;
        }
    }

    public async Task<double> CalculateProcessAnomalyScore(ProcessMetric metric)
    {
        try
        {
            if (metric == null)
            {
                _logger.LogWarning("Получена пустая метрика процесса для расчета оценки");
                return 0;
            }

            var (isAnomaly, score) = _anomalyDetection.DetectCpuAnomaly((float)metric.CpuUsagePercent);
            await UpdateMetricHistorySafe($"metric:cpu:{metric.ProcessName}", (float)metric.CpuUsagePercent);
            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при расчете оценки аномалии процесса");
            return 0;
        }
    }

    public async Task<double> CalculateNetworkAnomalyScore(NetworkMetric metric)
    {
        try
        {
            if (metric == null)
            {
                _logger.LogWarning("Получена пустая сетевая метрика для расчета оценки");
                return 0;
            }

            var totalTrafficMb = (metric.BytesSent + metric.BytesReceived) / (1024.0 * 1024.0);
            var (isAnomaly, score) = _anomalyDetection.DetectNetworkAnomaly((float)totalTrafficMb);
            await UpdateMetricHistorySafe($"metric:network:{metric.ProcessName}", (float)totalTrafficMb);
            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при расчете оценки аномалии сети");
            return 0;
        }
    }

    public async Task<bool> CheckProcessHashWithVirusTotal(string fileHash)
    {
        try
        {
            if (string.IsNullOrEmpty(fileHash))
            {
                _logger.LogWarning("Получен пустой хэш файла для проверки в VirusTotal");
                return false;
            }

            _logger.LogInformation("Проверка хэша файла в VirusTotal: {FileHash}", fileHash);
            var (isMalicious, detections) = await _virusTotalService.CheckFileHashAsync(fileHash);
            
            if (isMalicious && detections >= _settings.MinVirusTotalDetections)
            {
                _logger.LogWarning("Файл с хэшем {FileHash} помечен вредоносным ({Detections} обнаружений)", 
                    fileHash, detections);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке хэша файла {FileHash} в VirusTotal", fileHash);
            return false;
        }
    }

    private async Task UpdateMetricHistorySafe(string key, float value)
    {
        try
        {
            var db = _redis.GetDatabase();
            if (db == null)
            {
                _logger.LogWarning("Не удалось получить доступ к Redis для обновления истории метрик");
                return;
            }
            
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
            _logger.LogDebug("Обновлена история метрик для {Key}, текущее количество записей: {Count}", 
                key, values.Count);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Не удалось подключиться к Redis для обновления истории метрик");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении истории метрик для {Key}", key);
        }
    }
} 