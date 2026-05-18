using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class RiskScore : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? LabCycleId { get; set; }
    public LabCycle? LabCycle { get; set; }

    public DateTime CalculationDate { get; set; }

    public RiskType RiskType { get; set; }

    public RiskLevel RiskLevel { get; set; }

    public double Score { get; set; }

    public double Confidence { get; set; }

    public bool IsCritical { get; set; }

    public bool RequiresDoctorReview { get; set; }

    public string? Summary { get; set; }

    public string? Recommendation { get; set; }
}
