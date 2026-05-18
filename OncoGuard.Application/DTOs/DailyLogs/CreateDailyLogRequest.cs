using OncoGuard.Domain.Enums;

namespace OncoGuard.Application.DTOs.DailyLogs;

public class CreateDailyLogRequest
{
    public int PatientId { get; set; }

    public DateTime LogDate { get; set; }

    public int GeneralConditionScore { get; set; }

    public string? PatientNote { get; set; }

    // Symptom data
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

    // Hydration data
    public double WaterIntakeMl { get; set; }
    public SymptomGrade DryMouth { get; set; }
    public string? UrineColor { get; set; }
    public int UrinationCount { get; set; }

    // Medication data
    public bool TookMainMedication { get; set; }
    public int MissedDoseCount { get; set; }
    public bool UsedAntibiotic { get; set; }
    public bool UsedSteroid { get; set; }
    public bool UsedAntiemetic { get; set; }
    public bool UsedPainkiller { get; set; }
    public bool HadSideEffect { get; set; }
    public string? SideEffectDescription { get; set; }

    // Vital signs
    public double? SystolicBloodPressure { get; set; }
    public double? DiastolicBloodPressure { get; set; }
    public double? HeartRate { get; set; }
    public double? OxygenSaturation { get; set; }
}