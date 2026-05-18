using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class CalculatedFeature : BaseEntity
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? LabCycleId { get; set; }
    public LabCycle? LabCycle { get; set; }

    public DateTime FeatureDate { get; set; }

    // Nutrition features
    public double? ProteinRatio { get; set; }
    public double? CalorieRatio { get; set; }
    public double? ProteinDeficit3Day { get; set; }
    public double? ProteinDeficit7Day { get; set; }
    public double? CalorieDeficit7Day { get; set; }
    public double? AppetiteTrend7Day { get; set; }
    public int? MealSkippingFrequency { get; set; }

    // Weight / cachexia features
    public double? WeightLoss7DayPercent { get; set; }
    public double? WeightLoss30DayPercent { get; set; }
    public double? WeightLoss90DayPercent { get; set; }
    public double? CrpAlbuminRatio { get; set; }
    public double? AlbuminTrend { get; set; }
    public double? FunctionalDeclineScore { get; set; }

    // Infection / immune features
    public double? AncTrend { get; set; }
    public double? WbcTrend { get; set; }
    public double? LymphocyteTrend { get; set; }
    public bool FeverFlag { get; set; }
    public bool FeverAndLowAncFlag { get; set; }
    public bool CrpIncreaseFlag { get; set; }
    public bool PreviousNeutropeniaFlag { get; set; }
    public double? InfectionSymptomScore { get; set; }

    // Hydration features
    public double? WaterRatio { get; set; }
    public double? VomitingDiarrheaFluidLossScore { get; set; }
    public double? CreatinineTrend { get; set; }
    public double? UreaCreatinineRatio { get; set; }
    public double? ElectrolyteImbalanceScore { get; set; }
    public double? DizzinessDryMouthScore { get; set; }

    // Organ toxicity features
    public double? AstTrend { get; set; }
    public double? AltTrend { get; set; }
    public double? BilirubinTrend { get; set; }
    public bool EgfrDropFlag { get; set; }
    public bool TshAbnormalFlag { get; set; }
    public bool FreeT4AbnormalFlag { get; set; }
    public double? DiarrheaTrend { get; set; }
    public double? SkinRashTrend { get; set; }
    public double? DyspneaTrend { get; set; }

    // Medication / missing data features
    public double? MedicationAdherenceScore { get; set; }
    public int? MissedDoseCount3Day { get; set; }
    public int? MissedDoseCount7Day { get; set; }
    public bool AntibioticUseFlag { get; set; }
    public bool SteroidUseFlag { get; set; }
    public bool AntiemeticUseFlag { get; set; }

    public int MissingLogCount3Day { get; set; }
    public int MissingLogCount7Day { get; set; }
    public bool MissingNutritionFlag { get; set; }
    public bool MissingSymptomFlag { get; set; }
    public bool MissingMedicationFlag { get; set; }
    public double? PatientBurdenScore { get; set; }
}