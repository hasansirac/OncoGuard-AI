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

        var foodLog = new FoodLog
        {
            DailyLogId = request.DailyLogId,
            FoodName = request.FoodName,
            AmountGram = request.AmountGram,

            Calories = request.Calories,
            Protein = request.Protein,
            Carbohydrate = request.Carbohydrate,
            Fat = request.Fat,

            Fiber = request.Fiber,
            Iron = request.Iron,
            VitaminB12 = request.VitaminB12,
            Folate = request.Folate,
            VitaminD = request.VitaminD,
            Zinc = request.Zinc,
            Magnesium = request.Magnesium,
            Selenium = request.Selenium,

            Source = request.Source
        };

        await _context.FoodLogs.AddAsync(foodLog);

        var nutritionSummary = await _context.NutritionSummaries
            .FirstOrDefaultAsync(n => n.DailyLogId == request.DailyLogId);

        if (nutritionSummary == null)
        {
            nutritionSummary = new NutritionSummary
            {
                DailyLogId = request.DailyLogId,

                TotalCalories = 0,
                TotalProtein = 0,
                TotalCarbohydrate = 0,
                TotalFat = 0,

                TotalWaterMl = 0,
                ProteinTarget = 0,
                CalorieTarget = 0,
                ProteinRatio = 0,
                CalorieRatio = 0,
                MealCompletionRatio = 0,
                IsProteinDeficient = false,
                IsCalorieDeficient = false
            };

            await _context.NutritionSummaries.AddAsync(nutritionSummary);
        }

        nutritionSummary.TotalCalories += request.Calories;
        nutritionSummary.TotalProtein += request.Protein;
        nutritionSummary.TotalCarbohydrate += request.Carbohydrate;
        nutritionSummary.TotalFat += request.Fat;

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

        nutritionSummary.IsProteinDeficient = nutritionSummary.ProteinRatio < 0.8;
        nutritionSummary.IsCalorieDeficient = nutritionSummary.CalorieRatio < 0.8;

        await _context.SaveChangesAsync();
    }
}