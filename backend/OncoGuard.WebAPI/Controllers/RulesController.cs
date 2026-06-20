using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.Interfaces.Rules;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RulesController : ControllerBase
{
    private readonly IRuleEngineService _ruleEngineService;

    public RulesController(IRuleEngineService ruleEngineService)
    {
        _ruleEngineService = ruleEngineService;
    }

    [HttpPost("evaluate/{patientId}")]
    public async Task<IActionResult> EvaluateRules(int patientId)
    {
        await _ruleEngineService.EvaluateRulesAsync(patientId);

        return Ok("Rule engine evaluated patient data successfully.");
    }
}