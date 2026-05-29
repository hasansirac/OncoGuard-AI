using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.RiskExplanations;
using OncoGuard.Domain.Entities;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.RiskExplanations;

public class RiskExplanationService : IRiskExplanationService
{
    private readonly AppDbContext _context;

    public RiskExplanationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task GenerateExplanationAsync(int riskScoreId)
    {
        var riskScore = await _context.RiskScores
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == riskScoreId);

        if (riskScore == null)
            throw new Exception("RiskScore not found.");

        var explanations = new List<RiskExplanation>();

        explanations.Add(new RiskExplanation
        {
            RiskScoreId = riskScore.Id,
            ExplanationTitle = "Risk Level",
            ExplanationText = $"Risk level evaluated as {riskScore.RiskLevel}.",
            ContributionScore = 0.25,
            IsPrimaryReason = false,
            SuggestedAction = "Continue monitoring based on risk level."
        });

        explanations.Add(new RiskExplanation
        {
            RiskScoreId = riskScore.Id,
            ExplanationTitle = "Risk Type",
            ExplanationText = $"Detected dominant risk type: {riskScore.RiskType}.",
            ContributionScore = 0.25,
            IsPrimaryReason = false,
            SuggestedAction = "Review the dominant clinical risk category."
        });

        if (riskScore.IsCritical)
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = "Critical Risk",
                ExplanationText = "Patient condition is considered clinically critical.",
                ContributionScore = 1.0,
                IsPrimaryReason = true,
                SuggestedAction = "Urgent clinical review is recommended."
            });
        }

        if (riskScore.RequiresDoctorReview)
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = "Doctor Review Required",
                ExplanationText = "Doctor review is recommended due to elevated risk.",
                ContributionScore = 0.8,
                IsPrimaryReason = true,
                SuggestedAction = "Clinician should review the patient's latest clinical data."
            });
        }

        if (!string.IsNullOrWhiteSpace(riskScore.Summary))
        {
            explanations.Add(new RiskExplanation
            {
                RiskScoreId = riskScore.Id,
                ExplanationTitle = "Rule Summary",
                ExplanationText = riskScore.Summary,
                ContributionScore = 0.9,
                IsPrimaryReason = true,
                SuggestedAction = riskScore.Recommendation ?? "Clinical review may be considered."
            });
        }

        await _context.RiskExplanations.AddRangeAsync(explanations);
        await _context.SaveChangesAsync();
    }
}