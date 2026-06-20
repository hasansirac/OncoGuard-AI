using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class ClinicalReport : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? LabCycleId { get; set; }
    public LabCycle? LabCycle { get; set; }

    public DateTime ReportDate { get; set; }

    public string ReportTitle { get; set; } = null!;

    public string ReportContent { get; set; } = null!;

    public bool GeneratedByAI { get; set; }

    public bool ReviewedByDoctor { get; set; }

    public string? DoctorComment { get; set; }
}