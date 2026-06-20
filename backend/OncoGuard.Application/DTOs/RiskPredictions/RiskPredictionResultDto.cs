using System.Text.Json.Serialization;

namespace OncoGuard.Application.DTOs.RiskPredictions;

// FastAPI'den donen tek bir riskin sonucu.
// NOT: Bir risk yeterli gunluk veriye ulasmadiysa "monitoring" doner;
// bu durumda level/label/probabilities null olur (gun esigi - literatur temelli).
public class RiskPredictionResultDto
{
    [JsonPropertyName("risk")]
    public string Risk { get; set; } = string.Empty;

    // "ok" = hesaplandi, "monitoring" = yeterli gun yok, izleniyor
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("level_ai")]
    public int? LevelAi { get; set; }

    [JsonPropertyName("level_backend")]
    public int? LevelBackend { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("probabilities")]
    public Dictionary<string, double>? Probabilities { get; set; }

    // Gun esigi bilgisi (izleniyor durumunda dashboard'da gosterilebilir)
    [JsonPropertyName("days_logged")]
    public int? DaysLogged { get; set; }

    [JsonPropertyName("days_required")]
    public int? DaysRequired { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
