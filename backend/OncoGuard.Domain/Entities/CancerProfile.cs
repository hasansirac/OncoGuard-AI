using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class CancerProfile : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public CancerType CancerType { get; set; }

    public string Stage { get; set; } = null!;

    public bool HasMetastasis { get; set; }

    public DateTime DiagnosisDate { get; set; }

    public string? Comorbidities { get; set; }

    public string? SmokingStatus { get; set; }

    public string? AlcoholStatus { get; set; }

    public int EcogPerformanceScore { get; set; }

    public string TreatmentGoal { get; set; } = null!;
}