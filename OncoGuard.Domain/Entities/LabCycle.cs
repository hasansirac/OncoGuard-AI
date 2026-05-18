using OncoGuard.Domain.Common;
using OncoGuard.Domain.Enums;

namespace OncoGuard.Domain.Entities;

public class LabCycle : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int BaselineLabId { get; set; }
    public LabResult BaselineLab { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CycleStatus Status { get; set; }

    public int? PreviousCycleId { get; set; }
    public LabCycle? PreviousCycle { get; set; }

    public string? CycleSummary { get; set; }
}