namespace OncoGuard.Application.Interfaces.ClinicalReports;

public interface IClinicalReportService
{
    Task GenerateClinicalReportAsync(int riskScoreId);
}