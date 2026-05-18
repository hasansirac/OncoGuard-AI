using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class SymptomLog : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public double? BodyTemperature { get; set; }

    public SymptomGrade Fatigue { get; set; }

    public SymptomGrade Pain { get; set; }

    public SymptomGrade Nausea { get; set; }

    public int VomitingCount { get; set; }

    public int DiarrheaCount { get; set; }

    public SymptomGrade Constipation { get; set; }

    public SymptomGrade Cough { get; set; }

    public SymptomGrade Dyspnea { get; set; }

    public SymptomGrade MouthSore { get; set; }

    public SymptomGrade SwallowingDifficulty { get; set; }

    public SymptomGrade SkinRash { get; set; }

    public bool HasBleedingOrBruising { get; set; }

    public bool HasDizziness { get; set; }

    public bool HasConfusion { get; set; }

    public string? OtherSymptoms { get; set; }
}
