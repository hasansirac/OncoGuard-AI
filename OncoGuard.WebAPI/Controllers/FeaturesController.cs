using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.Interfaces.Features;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureEngineeringService _featureEngineeringService;

    public FeaturesController(IFeatureEngineeringService featureEngineeringService)
    {
        _featureEngineeringService = featureEngineeringService;
    }

    [HttpPost("generate/{patientId}")]
    public async Task<IActionResult> GenerateFeatures(int patientId)
    {
        await _featureEngineeringService.GenerateFeaturesAsync(patientId);

        return Ok("Features generated successfully.");
    }
}