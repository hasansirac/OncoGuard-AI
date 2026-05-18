using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.DailyLogs;
using OncoGuard.Application.Interfaces.DailyLogs;
using OncoGuard.Domain.Entities;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.DailyLogs;

public class DailyLogService : IDailyLogService
{
    private readonly AppDbContext _context;

    public DailyLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateDailyLogAsync(CreateDailyLogRequest request)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == request.PatientId);

        if (!patientExists)
            throw new Exception("Patient not found.");

        var dailyLog = new DailyLog
        {
            PatientId = request.PatientId,
            LogDate = request.LogDate,
            GeneralConditionScore = request.GeneralConditionScore,
            PatientNote = request.PatientNote
        };

        await _context.DailyLogs.AddAsync(dailyLog);
        await _context.SaveChangesAsync();

        var symptomLog = new SymptomLog
        {
            DailyLogId = dailyLog.Id,

            BodyTemperature = request.BodyTemperature,
            Fatigue = request.Fatigue,
            Pain = request.Pain,
            Nausea = request.Nausea,
            VomitingCount = request.VomitingCount,
            DiarrheaCount = request.DiarrheaCount,
            Constipation = request.Constipation,
            Cough = request.Cough,
            Dyspnea = request.Dyspnea,
            MouthSore = request.MouthSore,
            SwallowingDifficulty = request.SwallowingDifficulty,
            SkinRash = request.SkinRash,
            HasBleedingOrBruising = request.HasBleedingOrBruising,
            HasDizziness = request.HasDizziness,
            HasConfusion = request.HasConfusion,
            OtherSymptoms = request.OtherSymptoms
        };

        await _context.SymptomLogs.AddAsync(symptomLog);

        var hydrationLog = new HydrationLog
        {
            DailyLogId = dailyLog.Id,

            WaterIntakeMl = request.WaterIntakeMl,
            DryMouth = request.DryMouth,
            UrineColor = request.UrineColor,
            UrinationCount = request.UrinationCount
        };

        await _context.HydrationLogs.AddAsync(hydrationLog);

        var medicationLog = new MedicationLog
        {
            DailyLogId = dailyLog.Id,

            TookMainMedication = request.TookMainMedication,
            MissedDoseCount = request.MissedDoseCount,
            UsedAntibiotic = request.UsedAntibiotic,
            UsedSteroid = request.UsedSteroid,
            UsedAntiemetic = request.UsedAntiemetic,
            UsedPainkiller = request.UsedPainkiller,
            HadSideEffect = request.HadSideEffect,
            SideEffectDescription = request.SideEffectDescription
        };

        await _context.MedicationLogs.AddAsync(medicationLog);

        var vitalSignsLog = new VitalSignsLog
        {
            DailyLogId = dailyLog.Id,

            SystolicBloodPressure = request.SystolicBloodPressure,
            DiastolicBloodPressure = request.DiastolicBloodPressure,
            HeartRate = request.HeartRate,
            OxygenSaturation = request.OxygenSaturation
        };

        await _context.VitalSignsLogs.AddAsync(vitalSignsLog);

        await _context.SaveChangesAsync();
    }
}
