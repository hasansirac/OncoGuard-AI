using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.ClinicalReports;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.ClinicalReports;

public class ClinicalReportService : IClinicalReportService
{
    private readonly AppDbContext _context;

    public ClinicalReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task GenerateClinicalReportAsync(int riskScoreId)
    {
        var riskScore = await _context.RiskScores
            .Include(r => r.Patient)
            .Include(r => r.LabCycle)
            .FirstOrDefaultAsync(r => r.Id == riskScoreId);

        if (riskScore == null)
            throw new Exception("RiskScore not found.");

        var explanations = await _context.RiskExplanations
            .Where(e => e.RiskScoreId == riskScoreId)
            .ToListAsync();

        var reportText = $@"
Clinical Risk Report
---------------------

Patient Id: {riskScore.PatientId}
Risk Type: {riskScore.RiskType}
Risk Level: {riskScore.RiskLevel}
Risk Score: {riskScore.Score}

Summary:
{riskScore.Summary}

Recommendations:
{riskScore.Recommendation}

Detailed Explanations:
";

        foreach (var explanation in explanations)
        {
            reportText += $@"

- {explanation.ExplanationTitle}
  {explanation.ExplanationText}
  Suggested Action: {explanation.SuggestedAction}
";
        }

        var clinicalReport = new OncoGuard.Domain.Entities.ClinicalReport
        {
            PatientId = riskScore.PatientId,
            LabCycleId = riskScore.LabCycleId,
            ReportDate = DateTime.UtcNow,
            ReportTitle = "Automated Clinical Risk Report",
            ReportContent = reportText,
            GeneratedByAI = false,
            ReviewedByDoctor = false,
            DoctorComment = null
        };

        await _context.ClinicalReports.AddAsync(clinicalReport);

        await _context.SaveChangesAsync();
    }
}