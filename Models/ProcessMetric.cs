using System.ComponentModel.DataAnnotations;

namespace GuardMetrics.Models;

public class ProcessMetric
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string ProcessName { get; set; } = string.Empty;
    
    [Required]
    public string ExecutablePath { get; set; } = string.Empty;
    
    [Required]
    public string FileHash { get; set; } = string.Empty;
    
    [Required]
    public double CpuUsagePercent { get; set; }
    
    [Required]
    public long MemoryUsageBytes { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    public string? CommandLine { get; set; }
    
    public int? ParentProcessId { get; set; }
} 