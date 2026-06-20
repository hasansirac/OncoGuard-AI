using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.RiskPredictions;
using OncoGuard.Application.Interfaces.RiskPredictions;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.RiskPredictions;

public class PatientRiskEvaluationService : IPatientRiskEvaluationService
{
    private readonly AppDbContext _context;
    private readonly IRiskPredictionClient _client;

    public PatientRiskEvaluationService(AppDbContext context, IRiskPredictionClient client)
    {
        _context = context;
        _client = client;
    }

    public async Task<PredictRiskResponse> EvaluatePatientAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        // 1) Hasta profili
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient == null)
            throw new Exception("Patient not found.");

        // 1.1) Klinik profil: ECOG, komorbidite, evre, metastaz vb.
        var cancerProfile = await _context.CancerProfiles
            .Where(cp => cp.PatientId == patientId)
            .OrderByDescending(cp => cp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        // 1.2) Tedavi planı: önceki nötropeni, G-CSF, doz azaltımı, delay vb.
        var treatmentPlan = await _context.TreatmentPlans
            .Where(tp => tp.PatientId == patientId)
            .OrderByDescending(tp => tp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        // 2) Aktif lab cycle + baseline lab
        var activeCycle = await _context.LabCycles
            .Where(c => c.PatientId == patientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
            throw new Exception("No active lab cycle for this patient.");

        var baselineLab = await _context.LabResults
            .FirstOrDefaultAsync(l => l.Id == activeCycle.BaselineLabId, cancellationToken);

        if (baselineLab == null)
            throw new Exception("Baseline lab result not found for the active cycle.");

        // CycleNumber: TreatmentPlan varsa onu kullan; yoksa LabCycle sırasına göre hesapla.
        var calculatedCycleNumber = await _context.LabCycles
            .CountAsync(
                c => c.PatientId == patientId && c.StartDate <= activeCycle.StartDate,
                cancellationToken);

        if (calculatedCycleNumber < 1)
            calculatedCycleNumber = 1;

        var cycleNumber = treatmentPlan?.CycleNumber > 0
            ? treatmentPlan.CycleNumber
            : calculatedCycleNumber;

        var cycleStart = activeCycle.StartDate.Date;
        var plannedCycleEnd = activeCycle.EndDate?.Date ?? cycleStart.AddDays(20);
        var cycleLengthDays = Math.Max(1, (plannedCycleEnd - cycleStart).Days + 1);

        var cycleDay = (DateTime.Now.Date - cycleStart).Days + 1;
        if (cycleDay < 1)
            cycleDay = 1;
        if (cycleDay > cycleLengthDays)
            cycleDay = cycleLengthDays;

        // 3) Aktif lab cycle icindeki günlük loglar
        // Risk tahmini aktif baseline'a bagli oldugu icin eski cycle loglari AI'ya gonderilmez.
        // AI yine LogDate alanlarini kullanarak 3/7/30 gunluk gercek takvim pencerelerini hesaplar.
        var todayEndExclusive = DateTime.Now.Date.AddDays(1);
        var plannedEndExclusive = plannedCycleEnd.AddDays(1);
        var queryEndExclusive = plannedEndExclusive < todayEndExclusive
            ? plannedEndExclusive
            : todayEndExclusive;

        var dailyLogs = await _context.DailyLogs
            .Where(d =>
                d.PatientId == patientId &&
                d.LogDate >= cycleStart &&
                d.LogDate < queryEndExclusive)
            .OrderBy(d => d.LogDate)
            .ToListAsync(cancellationToken);

        // 4) FastAPI /predict-from-raw request
        var request = new RawPredictRequest
        {
            Patient = new Dictionary<string, object?>
            {
                // Temel hasta bilgileri
                { "Age", patient.Age },
                { "Gender", patient.Gender.ToString() },
                { "WeightKg", patient.Weight },
                { "HeightCm", patient.Height },

                // Klinik profil alanları
                { "ECOG", cancerProfile?.EcogPerformanceScore },
                { "Stage", cancerProfile?.Stage },
                { "HasMetastasis", cancerProfile?.HasMetastasis },
                { "Comorbidities", cancerProfile?.Comorbidities },
                { "SmokingStatus", cancerProfile?.SmokingStatus },
                { "AlcoholStatus", cancerProfile?.AlcoholStatus },
                { "TreatmentGoal", cancerProfile?.TreatmentGoal },

                // Cycle ve tedavi alanları
                { "CycleNumber", cycleNumber },
                { "CycleDay", cycleDay },
                { "CancerType", (cancerProfile?.CancerType ?? patient.CancerType).ToString() },
                { "TreatmentType", (treatmentPlan?.TreatmentType ?? patient.TreatmentType).ToString() },

                // Tedavi geçmişi / risk artırıcı klinik alanlar
                { "HasPreviousNeutropenia", treatmentPlan?.HasPreviousNeutropenia ?? false },
                { "HadPreviousTreatmentDelay", treatmentPlan?.HadPreviousTreatmentDelay ?? false },
                { "HasDoseReduction", treatmentPlan?.HasDoseReduction ?? false },
                { "UsesGcsfSupport", treatmentPlan?.UsesGcsfSupport ?? false },

                // Şu an model feature listesinde doğrudan yok; ama payload'da durması zararsızdır.
                // İleride rule engine veya açıklama katmanı için kullanılabilir.
                { "UsesAntibiotic", treatmentPlan?.UsesAntibiotic ?? false },
                { "UsesSteroid", treatmentPlan?.UsesSteroid ?? false },
                { "UsesAntiemetic", treatmentPlan?.UsesAntiemetic ?? false },
                { "UsesPainkiller", treatmentPlan?.UsesPainkiller ?? false }
            },

            BaselineLab = new Dictionary<string, object?>
            {
                { "Anc", baselineLab.Anc },
                { "Wbc", baselineLab.Wbc },
                { "Crp", baselineLab.Crp },
                { "Albumin", baselineLab.Albumin },
                { "Creatinine", baselineLab.Creatinine },
                { "Ast", baselineLab.Ast },
                { "Alt", baselineLab.Alt },
                { "Platelet", baselineLab.Platelet },
                { "Hemoglobin", baselineLab.Hemoglobin },
                { "TotalBilirubin", baselineLab.TotalBilirubin },
                { "Tsh", baselineLab.Tsh },
                { "FreeT4", baselineLab.FreeT4 }
            },

            DailyLogs = new List<Dictionary<string, object?>>(),

            // Backend ANC/WBC değerleri x10^3/uL olarak tutuluyorsa thousand kalmalı.
            WbcAncUnit = "thousand"
        };

        foreach (var log in dailyLogs)
        {
            var symptom = await _context.SymptomLogs
                .FirstOrDefaultAsync(s => s.DailyLogId == log.Id, cancellationToken);

            var hydration = await _context.HydrationLogs
                .FirstOrDefaultAsync(h => h.DailyLogId == log.Id, cancellationToken);

            var medication = await _context.MedicationLogs
                .FirstOrDefaultAsync(m => m.DailyLogId == log.Id, cancellationToken);

            var vitals = await _context.VitalSignsLogs
                .FirstOrDefaultAsync(v => v.DailyLogId == log.Id, cancellationToken);

            var nutrition = await _context.NutritionSummaries
                .FirstOrDefaultAsync(n => n.DailyLogId == log.Id, cancellationToken);

            request.DailyLogs.Add(new Dictionary<string, object?>
            {
                // En kritik alan: AI gerçek gün sayısını LogDate ile hesaplar.
                { "LogDate", log.LogDate },

                // Semptomlar
                { "BodyTemperature", symptom?.BodyTemperature ?? vitals?.BodyTemperature },
                { "Fatigue", symptom != null ? (int)symptom.Fatigue : 0 },
                { "Pain", symptom != null ? (int)symptom.Pain : 0 },
                { "Nausea", symptom != null ? (int)symptom.Nausea : 0 },
                { "VomitingCount", symptom?.VomitingCount ?? 0 },
                { "DiarrheaCount", symptom?.DiarrheaCount ?? 0 },
                { "Cough", symptom != null ? (int)symptom.Cough : 0 },
                { "Dyspnea", symptom != null ? (int)symptom.Dyspnea : 0 },
                { "SkinRash", symptom != null ? (int)symptom.SkinRash : 0 },
                { "HasDizziness", symptom?.HasDizziness ?? hydration?.HasDizziness ?? false },
                { "HasConfusion", symptom?.HasConfusion ?? false },
                { "HasBleedingOrBruising", symptom?.HasBleedingOrBruising ?? false },

                // Beslenme / hidrasyon
                { "TotalProtein", nutrition?.TotalProtein },
                { "TotalCalories", nutrition?.TotalCalories },
                { "WaterIntakeMl", hydration?.WaterIntakeMl ?? nutrition?.TotalWaterMl },
                { "MealCompletionRatio", nutrition?.MealCompletionRatio },

                // Şimdilik günlük kilo ayrı girilmiyorsa hasta kilosunu gönderiyoruz.
                { "WeightKg", patient.Weight },

                // İlaç uyumu
                { "TookMainMedication", medication?.TookMainMedication ?? true },
                { "MissedDoseCount", medication?.MissedDoseCount ?? 0 },

                // Vital / aktivite
                { "OxygenSaturation", vitals?.OxygenSaturation },
                { "ActivityLevel", log.GeneralConditionScore }
            });
        }

        // 5) FastAPI'ye gönder
        var result = await _client.PredictFromRawAsync(request, cancellationToken);

        // 6) Dönen riskleri RiskScores tablosuna kaydet
        await SaveRiskScoresAsync(patient.Id, activeCycle.Id, result, cancellationToken);

        return result;
    }

    private static readonly Dictionary<string, RiskType> RiskNameMap = new()
    {
        { "InfectionRisk", RiskType.Infection },
        { "FebrileNeutropeniaRisk", RiskType.FebrileNeutropenia },
        { "MalnutritionRisk", RiskType.Malnutrition },
        { "CachexiaRisk", RiskType.Cachexia },
        { "DehydrationRisk", RiskType.Dehydration },
        { "RenalToxicityRisk", RiskType.RenalToxicity },
        { "HepaticToxicityRisk", RiskType.HepaticToxicity },
        { "ImmunotherapyAdverseEventRisk", RiskType.ImmunotherapyAdverseEvent },
        { "TreatmentDelayRisk", RiskType.TreatmentDelay },
        { "OverallClinicalDeteriorationRisk", RiskType.OverallDeterioration },
    };

    private async Task SaveRiskScoresAsync(
        int patientId,
        int labCycleId,
        PredictRiskResponse result,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        foreach (var kvp in result.Predictions)
        {
            var pred = kvp.Value;

            if (!RiskNameMap.TryGetValue(pred.Risk, out var riskType))
                continue;

            // Monitoring durumundaki riskler henüz hesaplanmadı.
            if (pred.Status == "monitoring" || pred.LevelBackend == null)
                continue;

            var level = (RiskLevel)pred.LevelBackend;

            double confidence = 0;
            if (pred.Probabilities != null && pred.Probabilities.Count > 0)
                confidence = pred.Probabilities.Values.Max();

            bool isCritical = level == RiskLevel.Red;

            _context.RiskScores.Add(new RiskScore
            {
                PatientId = patientId,
                LabCycleId = labCycleId,
                CalculationDate = now,
                RiskType = riskType,
                RiskLevel = level,
                Score = pred.LevelBackend.Value,
                Confidence = confidence,
                IsCritical = isCritical,
                RequiresDoctorReview = level == RiskLevel.Red || level == RiskLevel.Orange,
                Summary = $"{pred.Risk}: {pred.Label}"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
