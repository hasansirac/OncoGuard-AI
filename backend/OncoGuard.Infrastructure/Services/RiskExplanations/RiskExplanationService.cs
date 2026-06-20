using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.RiskExplanations;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.RiskExplanations;

public class RiskExplanationService : IRiskExplanationService
{
    private readonly AppDbContext _context;

    public RiskExplanationService(AppDbContext context)
    {
        _context = context;
    }

    // Her risk tipi icin klinik gerekce (hangi veriler bu riski gosterir)
    private static readonly Dictionary<RiskType, (string driver, string action)> ClinicalRationale = new()
    {
        { RiskType.Infection,            ("Elevated fever, low neutrophil count and high CRP indicate infection risk.", "Consider infection workup and monitor temperature/CRP.") },
        { RiskType.FebrileNeutropenia,   ("Concurrent fever and low absolute neutrophil count (ANC) indicate febrile neutropenia.", "Urgent evaluation; consider empirical antibiotics per protocol.") },
        { RiskType.Malnutrition,         ("Insufficient protein intake combined with low albumin indicates malnutrition risk.", "Nutritional support and dietitian referral recommended.") },
        { RiskType.Cachexia,             ("Significant weight loss over time indicates cachexia risk.", "Assess body composition; consider nutritional and metabolic support.") },
        { RiskType.Dehydration,          ("Low fluid intake together with vomiting/diarrhea indicates dehydration risk.", "Assess hydration status; consider fluid replacement.") },
        { RiskType.RenalToxicity,        ("Elevated creatinine grade in clinical context indicates renal toxicity risk.", "Review nephrotoxic agents and hydration; monitor renal function.") },
        { RiskType.HepaticToxicity,      ("Elevated AST/ALT grades indicate hepatic toxicity risk.", "Review hepatotoxic agents; monitor liver function tests.") },
        { RiskType.ImmunotherapyAdverseEvent, ("Immune-mediated symptoms (rash, diarrhea, dyspnea) under immunotherapy indicate an adverse event.", "Evaluate for immune-related adverse events; consider steroids per protocol.") },
        { RiskType.TreatmentDelay,       ("Low neutrophil and/or platelet counts indicate treatment may need to be delayed.", "Re-evaluate treatment timing based on hematologic recovery.") },
        { RiskType.OverallDeterioration, ("Multiple simultaneously elevated risks indicate overall clinical deterioration.", "Comprehensive clinical review recommended.") },
    };

    public async Task GenerateExplanationAsync(int riskScoreId)
    {
        var riskScore = await _context.RiskScores
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == riskScoreId);

        if (riskScore == null)
            throw new Exception("RiskScore not found.");

        var explanations = new List<RiskExplanation>();

        // 1) Risk tipine ozel klinik gerekce (ANA aciklama)
        if (ClinicalRationale.TryGetValue(riskScore.RiskType, out var rationale))
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = $"{riskScore.RiskType} - Clinical Rationale",
                ExplanationText = rationale.driver,
                ContributionScore = riskScore.Confidence,
                IsPrimaryReason = true,
                SuggestedAction = rationale.action
            });
        }

        // 2) Risk seviyesi ozeti
        explanations.Add(new RiskExplanation
        {
            RiskScoreId = riskScore.Id,
            ExplanationTitle = "Risk Level",
            ExplanationText = $"Predicted risk level: {riskScore.RiskLevel} " +
                              $"(model confidence {riskScore.Confidence:P0}).",
            ContributionScore = riskScore.Confidence,
            IsPrimaryReason = false,
            SuggestedAction = "Continue monitoring based on risk level."
        });

        // 3) Kritik durum vurgusu
        if (riskScore.IsCritical)
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = "Critical Risk",
                ExplanationText = "Patient condition is considered clinically critical for this risk.",
                ContributionScore = 1.0,
                IsPrimaryReason = true,
                SuggestedAction = "Urgent clinical review is recommended."
            });
        }
        else if (riskScore.RequiresDoctorReview)
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = "Doctor Review Required",
                ExplanationText = "Doctor review is recommended due to elevated risk.",
                ContributionScore = 0.8,
                IsPrimaryReason = false,
                SuggestedAction = "Clinician should review the patient's latest clinical data."
            });
        }

        await _context.RiskExplanations.AddRangeAsync(explanations);
        await _context.SaveChangesAsync();
    }
}