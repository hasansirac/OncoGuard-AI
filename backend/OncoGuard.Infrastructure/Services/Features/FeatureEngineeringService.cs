using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.Features;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;


namespace OncoGuard.Infrastructure.Services.Features;

public class FeatureEngineeringService : IFeatureEngineeringService
{
    private readonly AppDbContext _context;

    public FeatureEngineeringService(AppDbContext context)
    {
        _context = context;
    }

    public async Task GenerateFeaturesAsync(int patientId)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == patientId);

        if (!patientExists)
            throw new Exception("Patient not found.");

        var activeCycle = await _context.LabCycles
     .FirstOrDefaultAsync(c =>
         c.PatientId == patientId &&
         c.Status == CycleStatus.Active);

        var latestDailyLog = await _context.DailyLogs
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.LogDate)
            .FirstOrDefaultAsync();

        if (latestDailyLog == null)
            throw new Exception("No daily log found for this patient.");

        var nutritionSummary = await _context.NutritionSummaries
            .FirstOrDefaultAsync(n => n.DailyLogId == latestDailyLog.Id);

        var hydrationLog = await _context.HydrationLogs
            .FirstOrDefaultAsync(h => h.DailyLogId == latestDailyLog.Id);

        var medicationLog = await _context.MedicationLogs
            .FirstOrDefaultAsync(m => m.DailyLogId == latestDailyLog.Id);

        var startDate = latestDailyLog.LogDate.Date.AddDays(-6);
        var endDate = latestDailyLog.LogDate.Date.AddDays(1);

        var logsLast7Days = await _context.DailyLogs
            .Where(d =>
                d.PatientId == patientId &&
                d.LogDate >= startDate &&
                d.LogDate < endDate)
            .ToListAsync();

        var missingLogCount7Day = 7 - logsLast7Days
            .Select(d => d.LogDate.Date)
            .Distinct()
            .Count();

        var features = new List<CalculatedFeature>();

        if (nutritionSummary != null)
        {
            features.Add(new CalculatedFeature
            {
                PatientId = patientId,
                LabCycleId = activeCycle?.Id,
                FeatureDate = latestDailyLog.LogDate,
                ProteinRatio = nutritionSummary.ProteinRatio,
                CalorieRatio = nutritionSummary.CalorieRatio
            });

            features.Add(new CalculatedFeature
            {
                PatientId = patientId,
                LabCycleId = activeCycle?.Id,
                FeatureDate = latestDailyLog.LogDate,
                ProteinDeficit7Day = nutritionSummary.IsProteinDeficient ? 1 : 0,
                CalorieDeficit7Day = nutritionSummary.IsCalorieDeficient ? 1 : 0
            });
        }

        if (hydrationLog != null)
        {
            var waterTarget = 2000.0;
            var waterRatio = hydrationLog.WaterIntakeMl / waterTarget;

            features.Add(new CalculatedFeature
            {
                PatientId = patientId,
                LabCycleId = activeCycle?.Id,
                FeatureDate = latestDailyLog.LogDate,
                WaterRatio = waterRatio,
                DizzinessDryMouthScore =
                    (hydrationLog.HasDizziness ? 1 : 0) +
                    ((int)hydrationLog.DryMouth)
            });
        }

        if (medicationLog != null)
        {
            var adherenceScore = medicationLog.TookMainMedication && medicationLog.MissedDoseCount == 0
                ? 1.0
                : 0.0;

            features.Add(new CalculatedFeature
            {
                PatientId = patientId,
                LabCycleId = activeCycle?.Id,
                FeatureDate = latestDailyLog.LogDate,
                MedicationAdherenceScore = adherenceScore,
                MissedDoseCount7Day = medicationLog.MissedDoseCount,
                AntibioticUseFlag = medicationLog.UsedAntibiotic,
                SteroidUseFlag = medicationLog.UsedSteroid,
                AntiemeticUseFlag = medicationLog.UsedAntiemetic
            });
        }

        features.Add(new CalculatedFeature
        {
            PatientId = patientId,
            LabCycleId = activeCycle?.Id,
            FeatureDate = latestDailyLog.LogDate,
            MissingLogCount7Day = missingLogCount7Day,
            MissingNutritionFlag = nutritionSummary == null,
            MissingMedicationFlag = medicationLog == null
        });

        await _context.CalculatedFeatures.AddRangeAsync(features);
        await _context.SaveChangesAsync();
    }
}