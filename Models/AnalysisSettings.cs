namespace GuardMetrics.Models;

public class AnalysisSettings
{
    public string[] SuspiciousProcessNames { get; set; } = new[]
    {
        "xmrig",
        "minerd",
        "ethminer",
        "cgminer",
        "bfgminer",
        "zcash",
        "nheqminer"
    };

    public double CpuUsageThreshold { get; set; } = 90.0;
    public long MemoryUsageThresholdBytes { get; set; } = 1024 * 1024 * 1024; // 1 GB
    public int MinVirusTotalDetections { get; set; } = 5;
    public double AnomalyScoreThreshold { get; set; } = 0.95;
    
    public string[] SuspiciousNetworkPorts { get; set; } = new[]
    {
        "3333", // Common mining pool port
        "14444", // Common mining pool port
        "7777", // Common mining pool port
        "8888", // Common mining pool port
        "9999" // Common mining pool port
    };
} 