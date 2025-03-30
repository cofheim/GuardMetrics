using GuardMetrics.Models;

namespace GuardMetrics.Services;

public interface IMetricAnalyzer
{
    Task<bool> IsProcessSuspicious(ProcessMetric metric);
    Task<bool> IsNetworkActivitySuspicious(NetworkMetric metric);
    Task<double> CalculateProcessAnomalyScore(ProcessMetric metric);
    Task<double> CalculateNetworkAnomalyScore(NetworkMetric metric);
    Task<bool> CheckProcessHashWithVirusTotal(string fileHash);
} 