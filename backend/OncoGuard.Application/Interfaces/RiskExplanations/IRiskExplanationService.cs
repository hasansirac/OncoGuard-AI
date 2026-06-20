namespace OncoGuard.Application.Interfaces.RiskExplanations;

public interface IRiskExplanationService
{
    Task GenerateExplanationAsync(int riskScoreId);
}
