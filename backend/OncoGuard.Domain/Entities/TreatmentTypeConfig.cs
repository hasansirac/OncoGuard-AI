using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class TreatmentTypeConfig : BaseEntity
{
    public TreatmentType TreatmentType { get; set; }

    public bool RequiresInfectionMonitoring { get; set; }

    public bool RequiresLiverMonitoring { get; set; }

    public bool RequiresKidneyMonitoring { get; set; }

    public bool RequiresEndocrineMonitoring { get; set; }

    public bool RequiresCardiacMonitoring { get; set; }

    public bool RequiresPulmonaryMonitoring { get; set; }

    public bool HasHighNeutropeniaRisk { get; set; }

    public bool HasHighCachexiaRisk { get; set; }

    public bool HasHighDehydrationRisk { get; set; }

    public string? RecommendedSupportiveCare { get; set; }

    public string? ClinicalNotes { get; set; }
}
