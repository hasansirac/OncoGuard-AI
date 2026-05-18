using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.DTOs.DailyLogs;
using OncoGuard.Application.Interfaces.DailyLogs;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DailyLogsController : ControllerBase
{
    private readonly IDailyLogService _dailyLogService;

    public DailyLogsController(IDailyLogService dailyLogService)
    {
        _dailyLogService = dailyLogService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDailyLog(CreateDailyLogRequest request)
    {
        await _dailyLogService.CreateDailyLogAsync(request);

        return Ok("Daily log created successfully.");
    }
}