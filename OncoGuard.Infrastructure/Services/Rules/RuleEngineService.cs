using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.Rules;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.Rules;

public class RuleEngineService : IRuleEngineService
{
    private readonly AppDbContext _context;

    public RuleEngineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task EvaluateRulesAsync(int patientId)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId);

        if (!patientExists)
            throw new Exception("Patient not found.");

        var activeCycle = await _context.LabCycles
            .FirstOrDefaultAsync(c => c.PatientId == patientId && c.Status == CycleStatus.Active);

        var latestLab = await _context.LabResults
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.LabDate)
            .FirstOrDefaultAsync();

        var latestDailyLog = await _context.DailyLogs
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.LogDate)
            .FirstOrDefaultAsync();

        if (latestDailyLog == null)
            throw new Exception("No daily log found for this patient.");

        var symptomLog = await _context.SymptomLogs
            .FirstOrDefaultAsync(s => s.DailyLogId == latestDailyLog.Id);

        var hydrationLog = await _context.HydrationLogs
            .FirstOrDefaultAsync(h => h.DailyLogId == latestDailyLog.Id);

        var nutritionSummary = await _context.NutritionSummaries
            .FirstOrDefaultAsync(n => n.DailyLogId == latestDailyLog.Id);

        var triggeredRules = new List<string>();

        var highestRiskLevel = RiskLevel.Green;
        var dominantRiskType = RiskType.OverallDeterioration;

        if (symptomLog?.BodyTemperature >= 38.0 && latestLab?.Anc <= 1.5)
        {
            triggeredRules.Add("High fever with low ANC detected. Possible febrile neutropenia risk.");
            highestRiskLevel = RiskLevel.Red;
            dominantRiskType = RiskType.FebrileNeutropenia;
        }

        if (hydrationLog != null && symptomLog != null)
        {
            var lowWater = hydrationLog.WaterIntakeMl < 1200;
            var fluidLoss = symptomLog.VomitingCount >= 2 || symptomLog.DiarrheaCount >= 3;

            if (lowWater && fluidLoss && highestRiskLevel < RiskLevel.Orange)
            {
                triggeredRules.Add("Low water intake with vomiting or diarrhea detected. Dehydration risk increased.");
                highestRiskLevel = RiskLevel.Orange;
                dominantRiskType = RiskType.Dehydration;
            }
        }

        if (nutritionSummary != null)
        {
            if (nutritionSummary.IsProteinDeficient &&
                nutritionSummary.IsCalorieDeficient &&
                highestRiskLevel < RiskLevel.Yellow)
            {
                triggeredRules.Add("Protein and calorie intake are below target. Malnutrition risk increased.");
                highestRiskLevel = RiskLevel.Yellow;
                dominantRiskType = RiskType.Malnutrition;
            }
        }

        if (latestLab?.Crp >= 20 && highestRiskLevel < RiskLevel.Yellow)
        {
            triggeredRules.Add("CRP is elevated. Inflammatory or infection-related risk should be reviewed.");
            highestRiskLevel = RiskLevel.Yellow;
            dominantRiskType = RiskType.Infection;
        }

        if (symptomLog != null && symptomLog.Dyspnea >= SymptomGrade.Severe)
        {
            triggeredRules.Add("Severe dyspnea detected. Clinical review is recommended.");
            highestRiskLevel = RiskLevel.Red;
            dominantRiskType = RiskType.OverallDeterioration;
        }

        if (!triggeredRules.Any())
        {
            triggeredRules.Add("No critical rule was triggered.");
        }

        var riskScoreValue = highestRiskLevel switch
        {
            RiskLevel.Green => 0.2,
            RiskLevel.Yellow => 0.5,
            RiskLevel.Orange => 0.75,
            RiskLevel.Red => 0.95,
            _ => 0.2
        };

        var riskScore = new RiskScore
        {
            PatientId = patientId,
            LabCycleId = activeCycle?.Id,
            CalculationDate = DateTime.UtcNow,
            RiskType = dominantRiskType,
            RiskLevel = highestRiskLevel,
            Score = riskScoreValue,
            Confidence = 1.0,
            IsCritical = highestRiskLevel == RiskLevel.Red,
            RequiresDoctorReview = highestRiskLevel == RiskLevel.Orange || highestRiskLevel == RiskLevel.Red,
            Summary = string.Join(" ", triggeredRules),
            Recommendation = highestRiskLevel == RiskLevel.Orange || highestRiskLevel == RiskLevel.Red
                ? "Clinical review is recommended."
                : "Continue monitoring."
        };

        await _context.RiskScores.AddAsync(riskScore);
        await _context.SaveChangesAsync();

        if (highestRiskLevel == RiskLevel.Orange || highestRiskLevel == RiskLevel.Red)
        {
            var alert = new AlertNotification
            {
                PatientId = patientId,
                RiskScoreId = riskScore.Id,
                RiskType = dominantRiskType,
                Severity = highestRiskLevel == RiskLevel.Red
                    ? AlertSeverity.Critical
                    : AlertSeverity.High,
                Message = string.Join(" ", triggeredRules),
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TriggeredByRule = true,
                TriggeredByModel = false
            };

            await _context.AlertNotifications.AddAsync(alert);
            await _context.SaveChangesAsync();
        }
    }
}