using System.Text.Json.Serialization;

namespace OncoGuard.Application.DTOs.RiskPredictions;

// FastAPI /predict-from-raw'a giden ana gövde.
public class RawPredictRequest
{
    [JsonPropertyName("patient")]
    public Dictionary<string, object?> Patient { get; set; } = new();

    [JsonPropertyName("BaselineLab")]
    public Dictionary<string, object?> BaselineLab { get; set; } = new();

    [JsonPropertyName("DailyLogs")]
    public List<Dictionary<string, object?>> DailyLogs { get; set; } = new();

    // Backend ANC/WBC'yi x10^3/uL tutuyorsa "thousand" kalmali.
    [JsonPropertyName("wbc_anc_unit")]
    public string WbcAncUnit { get; set; } = "thousand";
}