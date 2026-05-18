using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class ModelVersion : BaseEntity
{
    public string ModelName { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime TrainingDate { get; set; }

    public string? DatasetVersion { get; set; }

    public double? ValidationAccuracy { get; set; }

    public double? ValidationF1Score { get; set; }

    public bool IsActive { get; set; }
}