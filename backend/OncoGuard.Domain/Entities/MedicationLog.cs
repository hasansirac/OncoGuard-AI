using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class MedicationLog : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public bool TookMainMedication { get; set; }

    public int MissedDoseCount { get; set; }

    public bool UsedAntibiotic { get; set; }

    public bool UsedSteroid { get; set; }

    public bool UsedAntiemetic { get; set; }

    public bool UsedPainkiller { get; set; }

    public bool HadSideEffect { get; set; }

    public string? SideEffectDescription { get; set; }
}