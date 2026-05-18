using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class CancerTypeConfig : BaseEntity
{
    public CancerType CancerType { get; set; }

    public bool RequiresFrequentInfectionMonitoring { get; set; }

    public bool RequiresNutritionMonitoring { get; set; }

    public bool RequiresHydrationMonitoring { get; set; }

    public bool RequiresPulmonaryMonitoring { get; set; }

    public bool RequiresImmunotherapyMonitoring { get; set; }

    public bool IsHighCachexiaRisk { get; set; }

    public string? RecommendedLabPanel { get; set; }

    public string? ClinicalNotes { get; set; }
}