using System.Net;
using System.Net.Http.Json;
using OncoGuard.Application.DTOs.RiskPredictions;
using OncoGuard.Application.Interfaces.RiskPredictions;

namespace OncoGuard.Infrastructure.Services.RiskPredictions;

// FastAPI'den donen hatayi (ozellikle 422 insufficient_data) tasiyan ozel exception.
public class RiskPredictionException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public RiskPredictionException(int statusCode, string responseBody)
        : base($"AI service returned status {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public class RiskPredictionClient : IRiskPredictionClient
{
    private readonly HttpClient _httpClient;

    public RiskPredictionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PredictRiskResponse> PredictFromRawAsync(
        RawPredictRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/predict-from-raw",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            // 422 (insufficient_data) ve diger hatalari, durum kodu + govde ile firlat.
            throw new RiskPredictionException((int)response.StatusCode, errorBody);
        }

        var result = await response.Content.ReadFromJsonAsync<PredictRiskResponse>(
            cancellationToken: cancellationToken);

        if (result == null)
            throw new RiskPredictionException(500, "AI service returned empty response.");

        return result;
    }
}