using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class TreatmentPlan : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public TreatmentType TreatmentType { get; set; }

    public string ProtocolName { get; set; } = null!;

    public int CycleNumber { get; set; }

    public DateTime LastTreatmentDate { get; set; }

    public DateTime? NextPlannedTreatmentDate { get; set; }

    public bool HasPreviousNeutropenia { get; set; }

    public bool UsesGcsfSupport { get; set; }

    public bool UsesAntibiotic { get; set; }

    public bool UsesSteroid { get; set; }

    public bool UsesAntiemetic { get; set; }

    public bool UsesPainkiller { get; set; }

    public bool HasDoseReduction { get; set; }

    public bool HadPreviousTreatmentDelay { get; set; }
}