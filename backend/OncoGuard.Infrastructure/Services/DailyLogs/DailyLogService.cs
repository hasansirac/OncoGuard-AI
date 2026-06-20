using System.Linq;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.DailyLogs;
using OncoGuard.Application.Interfaces.DailyLogs;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.DailyLogs;

public class DailyLogService : IDailyLogService
{
    private const int DefaultCycleLengthDays = 21;
    private const int MinCycleLengthDays = 7;
    private const int MaxCycleLengthDays = 35;
    private readonly AppDbContext _context;

    public DailyLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateDailyLogAsync(CreateDailyLogRequest request)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == request.PatientId);

        if (!patientExists)
            throw new Exception("Patient not found.");

        var missingItems = await GetMissingDailyEntryPrerequisitesAsync(request.PatientId);

        if (missingItems.Count > 0)
        {
            throw new InvalidOperationException(
                "Daily data entry is not enabled yet. Doctor must complete: " + string.Join(", ", missingItems));
        }

        // We compare by calendar date, not by exact hour/minute.
        // Example: 2026-06-17 10:00 and 2026-06-17 21:00 are the same daily log.
        var logDate = request.LogDate.Date;
        var nextDate = logDate.AddDays(1);

        await EnsureLogDateIsInsideActiveCycleAsync(request.PatientId, logDate);

        var dailyLog = await _context.DailyLogs
            .FirstOrDefaultAsync(x =>
                x.PatientId == request.PatientId &&
                x.LogDate >= logDate &&
                x.LogDate < nextDate);

        if (dailyLog == null)
        {
            dailyLog = new DailyLog
            {
                PatientId = request.PatientId,
                LogDate = logDate,
                GeneralConditionScore = request.GeneralConditionScore,
                PatientNote = request.PatientNote
            };

            await _context.DailyLogs.AddAsync(dailyLog);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Same patient + same date exists, so update the existing daily record.
            dailyLog.LogDate = logDate;
            dailyLog.GeneralConditionScore = request.GeneralConditionScore;
            dailyLog.PatientNote = request.PatientNote;
        }

        // -------- SymptomLog upsert --------
        var symptomLog = await _context.SymptomLogs
            .FirstOrDefaultAsync(x => x.DailyLogId == dailyLog.Id);

        if (symptomLog == null)
        {
            symptomLog = new SymptomLog
            {
                DailyLogId = dailyLog.Id
            };

            await _context.SymptomLogs.AddAsync(symptomLog);
        }

        symptomLog.BodyTemperature = request.BodyTemperature;
        symptomLog.Fatigue = request.Fatigue;
        symptomLog.Pain = request.Pain;
        symptomLog.Nausea = request.Nausea;
        symptomLog.VomitingCount = request.VomitingCount;
        symptomLog.DiarrheaCount = request.DiarrheaCount;
        symptomLog.Constipation = request.Constipation;
        symptomLog.Cough = request.Cough;
        symptomLog.Dyspnea = request.Dyspnea;
        symptomLog.MouthSore = request.MouthSore;
        symptomLog.SwallowingDifficulty = request.SwallowingDifficulty;
        symptomLog.SkinRash = request.SkinRash;
        symptomLog.HasBleedingOrBruising = request.HasBleedingOrBruising;
        symptomLog.HasDizziness = request.HasDizziness;
        symptomLog.HasConfusion = request.HasConfusion;
        symptomLog.OtherSymptoms = request.OtherSymptoms;

        // -------- HydrationLog upsert --------
        var hydrationLog = await _context.HydrationLogs
            .FirstOrDefaultAsync(x => x.DailyLogId == dailyLog.Id);

        if (hydrationLog == null)
        {
            hydrationLog = new HydrationLog
            {
                DailyLogId = dailyLog.Id
            };

            await _context.HydrationLogs.AddAsync(hydrationLog);
        }

        hydrationLog.WaterIntakeMl = request.WaterIntakeMl;
        hydrationLog.DryMouth = request.DryMouth;
        hydrationLog.UrineColor = request.UrineColor;
        hydrationLog.UrinationCount = request.UrinationCount;

        // -------- MedicationLog upsert --------
        var medicationLog = await _context.MedicationLogs
            .FirstOrDefaultAsync(x => x.DailyLogId == dailyLog.Id);

        if (medicationLog == null)
        {
            medicationLog = new MedicationLog
            {
                DailyLogId = dailyLog.Id
            };

            await _context.MedicationLogs.AddAsync(medicationLog);
        }

        medicationLog.TookMainMedication = request.TookMainMedication;
        medicationLog.MissedDoseCount = request.MissedDoseCount;
        medicationLog.UsedAntibiotic = request.UsedAntibiotic;
        medicationLog.UsedSteroid = request.UsedSteroid;
        medicationLog.UsedAntiemetic = request.UsedAntiemetic;
        medicationLog.UsedPainkiller = request.UsedPainkiller;
        medicationLog.HadSideEffect = request.HadSideEffect;
        medicationLog.SideEffectDescription = request.SideEffectDescription;

        // -------- VitalSignsLog upsert --------
        var vitalSignsLog = await _context.VitalSignsLogs
            .FirstOrDefaultAsync(x => x.DailyLogId == dailyLog.Id);

        if (vitalSignsLog == null)
        {
            vitalSignsLog = new VitalSignsLog
            {
                DailyLogId = dailyLog.Id
            };

            await _context.VitalSignsLogs.AddAsync(vitalSignsLog);
        }

        vitalSignsLog.SystolicBloodPressure = request.SystolicBloodPressure;
        vitalSignsLog.DiastolicBloodPressure = request.DiastolicBloodPressure;
        vitalSignsLog.HeartRate = request.HeartRate;
        vitalSignsLog.OxygenSaturation = request.OxygenSaturation;

        await _context.SaveChangesAsync();

        return dailyLog.Id;
    }

    private async Task<List<string>> GetMissingDailyEntryPrerequisitesAsync(int patientId)
    {
        var missing = new List<string>();

        var cancerProfile = await _context.CancerProfiles
            .Where(cp => cp.PatientId == patientId)
            .OrderByDescending(cp => cp.CreatedDate)
            .FirstOrDefaultAsync();

        if (cancerProfile == null)
        {
            missing.Add("Clinical profile");
        }
        else
        {
            if (!Enum.IsDefined(typeof(CancerType), cancerProfile.CancerType)) missing.Add("Cancer type");
            if (string.IsNullOrWhiteSpace(cancerProfile.Stage)) missing.Add("Cancer stage");
            if (cancerProfile.DiagnosisDate == default || cancerProfile.DiagnosisDate.Year < 1900) missing.Add("Diagnosis date");
            if (!IsValidComorbidities(cancerProfile.Comorbidities)) missing.Add("Valid comorbidities");
            if (string.IsNullOrWhiteSpace(cancerProfile.SmokingStatus)) missing.Add("Smoking status");
            if (string.IsNullOrWhiteSpace(cancerProfile.AlcoholStatus)) missing.Add("Alcohol status");
            if (cancerProfile.EcogPerformanceScore < 0 || cancerProfile.EcogPerformanceScore > 4) missing.Add("ECOG performance score");
            if (string.IsNullOrWhiteSpace(cancerProfile.TreatmentGoal)) missing.Add("Treatment goal");
        }

        var treatmentPlan = await _context.TreatmentPlans
            .Where(tp => tp.PatientId == patientId)
            .OrderByDescending(tp => tp.CreatedDate)
            .FirstOrDefaultAsync();

        if (treatmentPlan == null)
        {
            missing.Add("Treatment plan");
        }
        else
        {
            if (!Enum.IsDefined(typeof(TreatmentType), treatmentPlan.TreatmentType)) missing.Add("Treatment type");
            if (string.IsNullOrWhiteSpace(treatmentPlan.ProtocolName)) missing.Add("Treatment protocol name");
            if (treatmentPlan.CycleNumber <= 0) missing.Add("Treatment cycle number");
            if (treatmentPlan.LastTreatmentDate == default || treatmentPlan.LastTreatmentDate.Year < 1900) missing.Add("Last treatment date");
            if (treatmentPlan.NextPlannedTreatmentDate == null || treatmentPlan.NextPlannedTreatmentDate.Value == default || treatmentPlan.NextPlannedTreatmentDate.Value.Year < 1900) missing.Add("Next planned treatment date");
        }

        var activeCycle = await _context.LabCycles
            .Where(lc => lc.PatientId == patientId && lc.Status == CycleStatus.Active)
            .OrderByDescending(lc => lc.StartDate)
            .FirstOrDefaultAsync();

        if (activeCycle == null)
        {
            missing.Add("Baseline lab cycle");
            return missing;
        }

        var baselineLab = await _context.LabResults
            .FirstOrDefaultAsync(lr => lr.Id == activeCycle.BaselineLabId);

        if (baselineLab == null)
        {
            missing.Add("Baseline lab result");
            return missing;
        }

        if (baselineLab.LabDate == default || baselineLab.LabDate.Year < 1900) missing.Add("Baseline lab date");
        if (baselineLab.Anc == null) missing.Add("Baseline ANC");
        if (baselineLab.Wbc == null) missing.Add("Baseline WBC");
        if (baselineLab.Lymphocyte == null) missing.Add("Baseline lymphocyte");
        if (baselineLab.Monocyte == null) missing.Add("Baseline monocyte");
        if (baselineLab.Crp == null) missing.Add("Baseline CRP");
        if (baselineLab.Esr == null) missing.Add("Baseline ESR");
        if (baselineLab.Procalcitonin == null) missing.Add("Baseline procalcitonin");
        if (baselineLab.Hemoglobin == null) missing.Add("Baseline hemoglobin");
        if (baselineLab.Hematocrit == null) missing.Add("Baseline hematocrit");
        if (baselineLab.Platelet == null) missing.Add("Baseline platelet");
        if (baselineLab.Mcv == null) missing.Add("Baseline MCV");
        if (baselineLab.Rdw == null) missing.Add("Baseline RDW");
        if (baselineLab.Creatinine == null) missing.Add("Baseline creatinine");
        if (baselineLab.Urea == null) missing.Add("Baseline urea");
        if (baselineLab.Egfr == null) missing.Add("Baseline eGFR");
        if (baselineLab.Sodium == null) missing.Add("Baseline sodium");
        if (baselineLab.Potassium == null) missing.Add("Baseline potassium");
        if (baselineLab.Magnesium == null) missing.Add("Baseline magnesium");
        if (baselineLab.Calcium == null) missing.Add("Baseline calcium");
        if (baselineLab.Chloride == null) missing.Add("Baseline chloride");
        if (baselineLab.Ast == null) missing.Add("Baseline AST");
        if (baselineLab.Alt == null) missing.Add("Baseline ALT");
        if (baselineLab.Alp == null) missing.Add("Baseline ALP");
        if (baselineLab.Ggt == null) missing.Add("Baseline GGT");
        if (baselineLab.TotalBilirubin == null) missing.Add("Baseline total bilirubin");
        if (baselineLab.DirectBilirubin == null) missing.Add("Baseline direct bilirubin");
        if (baselineLab.Ldh == null) missing.Add("Baseline LDH");
        if (baselineLab.Albumin == null) missing.Add("Baseline albumin");
        if (baselineLab.TotalProtein == null) missing.Add("Baseline total protein");
        if (baselineLab.Prealbumin == null) missing.Add("Baseline prealbumin");
        if (baselineLab.Ferritin == null) missing.Add("Baseline ferritin");
        if (baselineLab.Iron == null) missing.Add("Baseline iron");
        if (baselineLab.TransferrinSaturation == null) missing.Add("Baseline transferrin saturation");
        if (baselineLab.VitaminB12 == null) missing.Add("Baseline vitamin B12");
        if (baselineLab.Folate == null) missing.Add("Baseline folate");
        if (baselineLab.VitaminD == null) missing.Add("Baseline vitamin D");
        if (baselineLab.Zinc == null) missing.Add("Baseline zinc");
        if (baselineLab.Selenium == null) missing.Add("Baseline selenium");
        if (baselineLab.Tsh == null) missing.Add("Baseline TSH");
        if (baselineLab.FreeT4 == null) missing.Add("Baseline Free T4");
        if (baselineLab.Glucose == null) missing.Add("Baseline glucose");
        if (baselineLab.MorningCortisol == null) missing.Add("Baseline morning cortisol");
        if (baselineLab.Amylase == null) missing.Add("Baseline amylase");
        if (baselineLab.Lipase == null) missing.Add("Baseline lipase");

        return missing;
    }


    private async Task EnsureLogDateIsInsideActiveCycleAsync(int patientId, DateTime logDate)
    {
        var today = DateTime.Now.Date;
        if (logDate > today)
            throw new InvalidOperationException("Future daily entries are locked. The patient can enter only today or previous monitoring dates.");

        var activeCycle = await _context.LabCycles
            .Where(lc => lc.PatientId == patientId && lc.Status == CycleStatus.Active)
            .OrderByDescending(lc => lc.StartDate)
            .FirstOrDefaultAsync();

        if (activeCycle == null)
            throw new InvalidOperationException("No active lab cycle exists. Doctor must create a baseline lab cycle first.");

        var cycleStart = activeCycle.StartDate.Date;
        var plannedEnd = GetExpectedCycleEnd(cycleStart, activeCycle.EndDate);

        if (logDate < cycleStart || logDate > plannedEnd)
        {
            throw new InvalidOperationException(
                $"Selected date is outside the active monitoring cycle ({cycleStart:yyyy-MM-dd} - {plannedEnd:yyyy-MM-dd}).");
        }
    }

    private static DateTime GetExpectedCycleEnd(DateTime startDate, DateTime? storedEndDate)
    {
        if (storedEndDate.HasValue)
        {
            var end = storedEndDate.Value.Date;
            var length = (end - startDate.Date).Days + 1;
            if (length >= MinCycleLengthDays && length <= MaxCycleLengthDays)
                return end;
        }
        return startDate.Date.AddDays(DefaultCycleLengthDays - 1);
    }

    private static readonly string[] AllowedComorbidities =
    {
        "none",
        "diabetes",
        "hypertension",
        "chronic kidney disease",
        "heart failure",
        "copd",
        "liver disease"
    };

    private static bool IsValidComorbidities(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var selected = input
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => NormalizeOneComorbidity(x.Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count == 0)
            return false;

        if (selected.Any(x => !AllowedComorbidities.Contains(x, StringComparer.OrdinalIgnoreCase)))
            return false;

        if (selected.Contains("none", StringComparer.OrdinalIgnoreCase) && selected.Count > 1)
            return false;

        return true;
    }

    private static string NormalizeOneComorbidity(string value)
    {
        var v = value.Trim().ToLowerInvariant();

        return v switch
        {
            "none" or "no" or "no known comorbidity" or "no comorbidity" => "none",
            "diabetes" or "diabetes mellitus" or "dm" => "diabetes",
            "hypertension" or "htn" or "high blood pressure" => "hypertension",
            "chronic kidney disease" or "ckd" or "renal disease" or "kidney disease" => "chronic kidney disease",
            "heart failure" or "chf" or "cardiac failure" => "heart failure",
            "copd" or "asthma" or "chronic obstructive pulmonary disease" => "copd",
            "liver disease" or "cirrhosis" or "hepatic disease" => "liver disease",
            _ => v
        };
    }

}
