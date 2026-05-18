using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class AlertNotification : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? RiskScoreId { get; set; }
    public RiskScore? RiskScore { get; set; }

    public RiskType RiskType { get; set; }

    public AlertSeverity Severity { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }

    public bool TriggeredByRule { get; set; }

    public bool TriggeredByModel { get; set; }
}