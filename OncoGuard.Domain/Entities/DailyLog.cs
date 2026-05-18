using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class DailyLog : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime LogDate { get; set; }

    public int GeneralConditionScore { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsMissingLog { get; set; }

    public string? PatientNote { get; set; }
}
