using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.DTOs.Labs;
using OncoGuard.Application.Interfaces.Labs;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LabsController : ControllerBase
{
    private readonly ILabService _labService;

    public LabsController(ILabService labService)
    {
        _labService = labService;
    }

    [HttpPost]
    public async Task<IActionResult> AddLabResult(CreateLabResultRequest request)
    {
        await _labService.AddLabResultAsync(request);

        return Ok("Lab result added and new lab cycle started successfully.");
    }
}