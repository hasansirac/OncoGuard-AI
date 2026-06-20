using OncoGuard.Application.DTOs.RiskPredictions;

namespace OncoGuard.Application.Interfaces.RiskPredictions;

public interface IPatientRiskEvaluationService
{
    // Bir hastanin DB'deki verisini toplar, FastAPI'ye gonderir, 10 risk doner.
    Task<PredictRiskResponse> EvaluatePatientAsync(
        int patientId,
        CancellationToken cancellationToken = default);
}