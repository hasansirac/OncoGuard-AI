using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.DTOs.FoodLogs;
using OncoGuard.Application.Interfaces.FoodLogs;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FoodLogsController : ControllerBase
{
    private readonly IFoodLogService _foodLogService;

    public FoodLogsController(IFoodLogService foodLogService)
    {
        _foodLogService = foodLogService;
    }

    [HttpPost]
    public async Task<IActionResult> AddFoodLog(CreateFoodLogRequest request)
    {
        await _foodLogService.AddFoodLogAsync(request);

        return Ok("Food log added and nutrition summary updated successfully.");
    }
}