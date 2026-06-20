using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class RiskWeight : BaseEntity
{
    public RiskType RiskType { get; set; }

    public CancerType? CancerType { get; set; }

    public TreatmentType? TreatmentType { get; set; }

    public string FeatureName { get; set; } = null!;

    public double WeightValue { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }
}