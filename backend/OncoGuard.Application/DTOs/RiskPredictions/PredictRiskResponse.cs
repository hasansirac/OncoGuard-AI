using System.Text.Json.Serialization;

namespace OncoGuard.Application.DTOs.RiskPredictions;

// FastAPI'nin dondurdugu tam cevap (10 risk).
public class PredictRiskResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predictions")]
    public Dictionary<string, RiskPredictionResultDto> Predictions { get; set; } = new();
}