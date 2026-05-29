namespace OncoGuard.Application.Interfaces.Rules;

public interface IRuleEngineService
{
    Task EvaluateRulesAsync(int patientId);
}