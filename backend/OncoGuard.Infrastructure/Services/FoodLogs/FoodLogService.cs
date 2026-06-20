using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.FoodLogs;
using OncoGuard.Application.Interfaces.FoodLogs;
using OncoGuard.Domain.Entities;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.FoodLogs;

public class FoodLogService : IFoodLogService
{
    private readonly AppDbContext _context;

    public FoodLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddFoodLogAsync(CreateFoodLogRequest request)
    {
        var dailyLog = await _context.DailyLogs
            .Include(d => d.Patient)
            .FirstOrDefaultAsync(d => d.Id == request.DailyLogId);

        if (dailyLog == null)
            throw new Exception("Daily log not found.");

        var foodName = string.IsNullOrWhiteSpace(request.FoodName)
            ? "Daily nutrition summary"
            : request.FoodName.Trim();

        // Same DailyLog + same FoodName should be updated, not inserted again.
        var foodLog = await _context.FoodLogs
            .FirstOrDefaultAsync(f =>
                f.DailyLogId == request.DailyLogId &&
                f.FoodName == foodName);

        if (foodLog == null)
        {
            foodLog = new FoodLog
            {
                DailyLogId = request.DailyLogId,
                FoodName = foodName
            };

            await _context.FoodLogs.AddAsync(foodLog);
        }

        // Update food log values.
        foodLog.AmountGram = request.AmountGram;

        foodLog.Calories = request.Calories;
        foodLog.Protein = request.Protein;
        foodLog.Carbohydrate = request.Carbohydrate;
        foodLog.Fat = request.Fat;

        foodLog.Fiber = request.Fiber;
        foodLog.Iron = request.Iron;
        foodLog.VitaminB12 = request.VitaminB12;
        foodLog.Folate = request.Folate;
        foodLog.VitaminD = request.VitaminD;
        foodLog.Zinc = request.Zinc;
        foodLog.Magnesium = request.Magnesium;
        foodLog.Selenium = request.Selenium;

        foodLog.Source = request.Source;

        await _context.SaveChangesAsync();

        // Recalculate NutritionSummary from all FoodLogs of this DailyLog.
        // This is safer than += because updates should not inflate totals.
        var allFoodLogsForDay = await _context.FoodLogs
            .Where(f => f.DailyLogId == request.DailyLogId)
            .ToListAsync();

        var nutritionSummary = await _context.NutritionSummaries
            .FirstOrDefaultAsync(n => n.DailyLogId == request.DailyLogId);

        if (nutritionSummary == null)
        {
            nutritionSummary = new NutritionSummary
            {
                DailyLogId = request.DailyLogId
            };

            await _context.NutritionSummaries.AddAsync(nutritionSummary);
        }

        nutritionSummary.TotalCalories = allFoodLogsForDay.Sum(f => f.Calories);
        nutritionSummary.TotalProtein = allFoodLogsForDay.Sum(f => f.Protein);
        nutritionSummary.TotalCarbohydrate = allFoodLogsForDay.Sum(f => f.Carbohydrate);
        nutritionSummary.TotalFat = allFoodLogsForDay.Sum(f => f.Fat);

        var hydrationLog = await _context.HydrationLogs
            .FirstOrDefaultAsync(h => h.DailyLogId == request.DailyLogId);

        nutritionSummary.TotalWaterMl = hydrationLog?.WaterIntakeMl ?? 0;

        var patientWeight = dailyLog.Patient.Weight;

        nutritionSummary.ProteinTarget = patientWeight * 1.2;
        nutritionSummary.CalorieTarget = patientWeight * 25;

        nutritionSummary.ProteinRatio =
            nutritionSummary.ProteinTarget > 0
                ? nutritionSummary.TotalProtein / nutritionSummary.ProteinTarget
                : 0;

        nutritionSummary.CalorieRatio =
            nutritionSummary.CalorieTarget > 0
                ? nutritionSummary.TotalCalories / nutritionSummary.CalorieTarget
                : 0;

        nutritionSummary.MealCompletionRatio =
            (Math.Min(nutritionSummary.ProteinRatio, 1) +
             Math.Min(nutritionSummary.CalorieRatio, 1)) / 2;

        nutritionSummary.IsProteinDeficient = nutritionSummary.ProteinRatio < 0.8;
        nutritionSummary.IsCalorieDeficient = nutritionSummary.CalorieRatio < 0.8;

        await _context.SaveChangesAsync();
    }
}