using System.ComponentModel.DataAnnotations;

namespace GuardMetrics.Models;

public class NetworkMetric
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string ProcessName { get; set; } = string.Empty;
    
    [Required]
    public string LocalAddress { get; set; } = string.Empty;
    
    [Required]
    public int LocalPort { get; set; }
    
    public string? RemoteAddress { get; set; }
    
    public int? RemotePort { get; set; }
    
    [Required]
    public string Protocol { get; set; } = string.Empty;
    
    public long BytesSent { get; set; }
    
    public long BytesReceived { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    public string? State { get; set; }
} 