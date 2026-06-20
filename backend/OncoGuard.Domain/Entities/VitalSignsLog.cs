using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class VitalSignsLog : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public double? SystolicBloodPressure { get; set; }

    public double? DiastolicBloodPressure { get; set; }

    public double? HeartRate { get; set; }

    public double? OxygenSaturation { get; set; }

    public double? BodyTemperature { get; set; }
}