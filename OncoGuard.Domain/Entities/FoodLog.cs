using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class FoodLog : BaseEntity
{
    public int DailyLogId { get; set; }
    public DailyLog DailyLog { get; set; } = null!;

    public string FoodName { get; set; } = null!;

    public double AmountGram { get; set; }

    public double Calories { get; set; }

    public double Protein { get; set; }

    public double Carbohydrate { get; set; }

    public double Fat { get; set; }

    public double? Fiber { get; set; }

    public double? Iron { get; set; }

    public double? VitaminB12 { get; set; }

    public double? Folate { get; set; }

    public double? VitaminD { get; set; }

    public double? Zinc { get; set; }

    public double? Magnesium { get; set; }

    public double? Selenium { get; set; }

    public string? Source { get; set; }
}