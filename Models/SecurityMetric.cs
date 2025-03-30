using System.ComponentModel.DataAnnotations;

namespace GuardMetrics.Models;

public enum MetricType
{
    ProcessCreation,
    NetworkConnection,
    FileAccess,
    RegistryAccess,
    UserLogin,
    PrivilegeEscalation,
    SuspiciousActivity,
    MalwareDetection,
    SystemResource,
    Other
}

public class SecurityMetric
{
    [Key]
    public int Id { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    [Required]
    [StringLength(50)]
    public string AgentId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;
    
    public MetricType Type { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    public double? Value { get; set; }
    
    [StringLength(2000)]
    public string? Description { get; set; }
    
    public bool IsAnomaly { get; set; }
    
    public double? AnomalyScore { get; set; }
    
    public Dictionary<string, string>? Metadata { get; set; }
} 