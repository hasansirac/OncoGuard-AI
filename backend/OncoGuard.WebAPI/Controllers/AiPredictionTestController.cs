using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.DTOs.RiskPredictions;
using OncoGuard.Application.Interfaces.RiskPredictions;
using OncoGuard.Infrastructure.Services.RiskPredictions;
using Microsoft.AspNetCore.Authorization;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiPredictionTestController : ControllerBase
{
    private readonly IRiskPredictionClient _riskPredictionClient;
    private readonly IPatientRiskEvaluationService _evaluationService;

    public AiPredictionTestController(
        IRiskPredictionClient riskPredictionClient,
        IPatientRiskEvaluationService evaluationService)
    {
        _riskPredictionClient = riskPredictionClient;
        _evaluationService = evaluationService;
    }

    // GECICI TEST: DB'den veri toplamadan, sabit ornek ham veriyle FastAPI'yi cagirir.
    // Amac: backend -> FastAPI koprusu calisiyor mu gormek.
    [HttpPost("test")]
    public async Task<IActionResult> TestPrediction()
    {
        var request = new RawPredictRequest
        {
            Patient = new Dictionary<string, object?>
            {
                { "Age", 62 }, { "ECOG", 1 }, { "WeightKg", 70 }, { "HeightCm", 170 },
                { "CycleDay", 10 }, { "CycleNumber", 2 },
                { "PreviousNeutropenia", true }, { "PreviousTreatmentDelay", false },
                { "PreviousSevereToxicity", false }, { "DoseReductionFlag", false },
                { "GCSFUseFlag", true }, { "HasDiabetes", true },
                { "CancerType", "Colon" }, { "TreatmentType", "Chemotherapy" }
            },
            BaselineLab = new Dictionary<string, object?>
            {
                { "Anc", 0.8 }, { "Wbc", 3.2 }, { "Crp", 45 }, { "Albumin", 3.1 },
                { "Creatinine", 1.1 }, { "Ast", 30 }, { "Alt", 28 }, { "Platelet", 140 },
                { "Hemoglobin", 10.5 }, { "TotalBilirubin", 0.8 }, { "Tsh", 2.0 }, { "FreeT4", 1.2 }
            },
            DailyLogs = new List<Dictionary<string, object?>>()
        };

        // 7 gunluk benzer log ekle (atesli, dusuk beslenme - kotu hasta)
        for (int i = 0; i < 7; i++)
        {
            request.DailyLogs.Add(new Dictionary<string, object?>
            {
                { "BodyTemperature", 38.5 }, { "Fatigue", 3 }, { "VomitingCount", 1 },
                { "TotalProtein", 35 }, { "TotalCalories", 1300 }, { "WaterIntakeMl", 1300 },
                { "AppetiteScore", 2 }, { "MealCompletionRatio", 0.5 }, { "WeightKg", 68 },
                { "TookMainMedication", true }, { "MissedDoseCount", 0 },
                { "OxygenSaturation", 95 }, { "ActivityLevel", 3 }
            });
        }

        var result = await _riskPredictionClient.PredictFromRawAsync(request);

        return Ok(result);
    }

    // YENI: gercek hastanin DB verisiyle tahmin
    [HttpPost("evaluate/{patientId}")]
    public async Task<IActionResult> EvaluatePatient(int patientId)
    {
        try
        {
            var result = await _evaluationService.EvaluatePatientAsync(patientId);
            return Ok(result);
        }
        catch (RiskPredictionException ex) when (ex.StatusCode == 422)
        {
            // FastAPI "yetersiz veri" dedi: 500 yerine 422 olarak, gercek mesajla goster.
            return StatusCode(422, new
            {
                status = "insufficient_data",
                message = "AI service declined to predict: not enough data.",
                aiResponse = ex.ResponseBody
            });
        }
        catch (RiskPredictionException ex)
        {
            return StatusCode(502, new
            {
                message = "AI service error.",
                statusCode = ex.StatusCode,
                aiResponse = ex.ResponseBody
            });
        }
        catch (Exception ex)
        {
            // DB'de hasta/cycle yoksa buraya duser ("Patient not found" vb.)
            return BadRequest(new { message = ex.Message });
        }
    }
}
