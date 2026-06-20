using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class HydrationLog : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public double WaterIntakeMl { get; set; }

    public SymptomGrade DryMouth { get; set; }

    public string? UrineColor { get; set; }

    public int UrinationCount { get; set; }

    public bool HasDizziness { get; set; }

    public bool HasVomitingOrDiarrhea { get; set; }
}