using OncoGuard.Application.DTOs.RiskPredictions;

namespace OncoGuard.Application.Interfaces.RiskPredictions;

public interface IRiskPredictionClient
{
    Task<PredictRiskResponse> PredictFromRawAsync(
        RawPredictRequest request,
        CancellationToken cancellationToken = default);
}