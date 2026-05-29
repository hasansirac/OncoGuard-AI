using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.Interfaces.RiskExplanations;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RiskExplanationsController : ControllerBase
{
    private readonly IRiskExplanationService _riskExplanationService;

    public RiskExplanationsController(IRiskExplanationService riskExplanationService)
    {
        _riskExplanationService = riskExplanationService;
    }

    [HttpPost("generate/{riskScoreId}")]
    public async Task<IActionResult> GenerateExplanation(int riskScoreId)
    {
        await _riskExplanationService.GenerateExplanationAsync(riskScoreId);

        return Ok("Risk explanation generated successfully.");
    }
}