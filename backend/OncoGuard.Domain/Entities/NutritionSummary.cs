using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class NutritionSummary : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public double TotalCalories { get; set; }

    public double TotalProtein { get; set; }

    public double TotalCarbohydrate { get; set; }

    public double TotalFat { get; set; }

    public double TotalWaterMl { get; set; }

    public double ProteinTarget { get; set; }

    public double CalorieTarget { get; set; }

    public double ProteinRatio { get; set; }

    public double CalorieRatio { get; set; }

    public double MealCompletionRatio { get; set; }

    public bool IsProteinDeficient { get; set; }

    public bool IsCalorieDeficient { get; set; }
}
