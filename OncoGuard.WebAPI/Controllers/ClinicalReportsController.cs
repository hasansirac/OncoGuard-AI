using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.Interfaces.ClinicalReports;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClinicalReportsController : ControllerBase
{
    private readonly IClinicalReportService _clinicalReportService;

    public ClinicalReportsController(IClinicalReportService clinicalReportService)
    {
        _clinicalReportService = clinicalReportService;
    }

    [HttpPost("generate/{riskScoreId}")]
    public async Task<IActionResult> GenerateReport(int riskScoreId)
    {
        await _clinicalReportService.GenerateClinicalReportAsync(riskScoreId);

        return Ok("Clinical report generated successfully.");
    }
}