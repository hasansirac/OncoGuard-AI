using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.Labs;
using OncoGuard.Application.Interfaces.Labs;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LabsController : ControllerBase
{
    private const int DefaultCycleLengthDays = 21;
    private const int MinCycleLengthDays = 7;
    private const int MaxCycleLengthDays = 35;

    private readonly ILabService _labService;
    private readonly AppDbContext _context;

    public LabsController(ILabService labService, AppDbContext context)
    {
        _labService = labService;
        _context = context;
    }

    // Creates a NEW active lab cycle. If another cycle is active, LabService closes it first.
    // Important: This is NOT used for typo correction. For correction, use PUT /active-baseline.
    [HttpPost]
    public async Task<IActionResult> AddLabResult(CreateLabResultRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequiredBaselineFields(request);
        if (validation.Count > 0)
        {
            return BadRequest(new
            {
                message = "Required baseline lab values are missing or invalid.",
                missingItems = validation
            });
        }

        var activeCycle = await _context.LabCycles
            .AsNoTracking()
            .Where(c => c.PatientId == request.PatientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle != null && request.LabDate.Date <= activeCycle.StartDate.Date)
        {
            return BadRequest(new
            {
                message = "New lab cycle date must be after the current active cycle start date. If you only need to correct a wrong baseline date or typo, use Correct Current Baseline instead of Start New Lab Cycle.",
                currentCycleId = activeCycle.Id,
                currentCycleStartDate = activeCycle.StartDate.ToString("yyyy-MM-dd"),
                requestedLabDate = request.LabDate.ToString("yyyy-MM-dd")
            });
        }

        var cycleLengthDays = NormalizeCycleLength(request.CycleLengthDays);
        request.CycleLengthDays = cycleLengthDays;

        await _labService.AddLabResultAsync(request);

        return Ok(new
        {
            message = "Lab result saved and a new active baseline lab cycle was started. The patient daily monitoring calendar now follows the planned cycle window.",
            patientId = request.PatientId,
            mode = activeCycle == null ? "first_baseline_cycle" : "new_cycle",
            cycleStartDate = request.LabDate.Date.ToString("yyyy-MM-dd"),
            plannedCycleEndDate = request.LabDate.Date.AddDays(cycleLengthDays - 1).ToString("yyyy-MM-dd"),
            cycleLengthDays
        });
    }

    // Shows current active lab cycle and baseline values.
    [HttpGet("patient/{patientId:int}/active-cycle")]
    public async Task<IActionResult> GetActiveCycle(int patientId, CancellationToken cancellationToken)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == patientId, cancellationToken);

        if (!patientExists)
            return NotFound(new { patientId, hasActiveCycle = false, message = "Patient not found." });

        var activeCycle = await _context.LabCycles
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            return Ok(new
            {
                patientId,
                hasActiveCycle = false,
                activeCycle = (object?)null,
                baselineLab = (object?)null,
                cycleLengthDays = DefaultCycleLengthDays,
                plannedCycleEndDate = (string?)null,
                message = "No active baseline lab cycle exists. The doctor must create one before daily monitoring starts."
            });
        }

        var baselineLab = await _context.LabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == activeCycle.BaselineLabId, cancellationToken);

        var plannedEndDate = GetExpectedCycleEnd(activeCycle.StartDate.Date, activeCycle.EndDate);
        var cycleLengthDays = (plannedEndDate - activeCycle.StartDate.Date).Days + 1;
        var today = DateTime.Now.Date;
        var currentCycleDay = today < activeCycle.StartDate.Date
            ? 0
            : Math.Min(cycleLengthDays, (today - activeCycle.StartDate.Date).Days + 1);

        return Ok(new
        {
            patientId,
            hasActiveCycle = baselineLab != null,
            activeCycle,
            baselineLab,
            cycleStartDate = activeCycle.StartDate.Date.ToString("yyyy-MM-dd"),
            plannedCycleEndDate = plannedEndDate.ToString("yyyy-MM-dd"),
            cycleLengthDays,
            currentCycleDay,
            message = baselineLab != null
                ? "Active baseline lab cycle found. Daily monitoring dates are generated from the cycle start date through the planned cycle end date."
                : "Active cycle exists, but the baseline lab result could not be found."
        });
    }

    // Corrects the CURRENT active baseline lab result without starting a new cycle.
    // Use this only for typo/correction, not for new follow-up lab results.
    [HttpPut("patient/{patientId:int}/active-baseline")]
    public async Task<IActionResult> CorrectActiveBaseline(int patientId, CreateLabResultRequest request, CancellationToken cancellationToken)
    {
        var activeCycle = await _context.LabCycles
            .Where(c => c.PatientId == patientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            return BadRequest(new
            {
                patientId,
                message = "No active lab cycle exists. Create a baseline lab cycle first."
            });
        }

        var baselineLab = await _context.LabResults
            .FirstOrDefaultAsync(l => l.Id == activeCycle.BaselineLabId, cancellationToken);

        if (baselineLab == null)
        {
            return BadRequest(new
            {
                patientId,
                message = "Active cycle exists, but baseline lab result was not found."
            });
        }

        request.PatientId = patientId;
        var validation = ValidateRequiredBaselineFields(request);
        if (validation.Count > 0)
        {
            return BadRequest(new
            {
                message = "Required baseline lab values are missing or invalid.",
                missingItems = validation
            });
        }

        var cycleLengthDays = NormalizeCycleLength(request.CycleLengthDays);

        baselineLab.Wbc = request.Wbc;
        baselineLab.Anc = request.Anc;
        baselineLab.Lymphocyte = request.Lymphocyte;
        baselineLab.Monocyte = request.Monocyte;
        baselineLab.Crp = request.Crp;
        baselineLab.Esr = request.Esr;
        baselineLab.Procalcitonin = request.Procalcitonin;
        baselineLab.Hemoglobin = request.Hemoglobin;
        baselineLab.Hematocrit = request.Hematocrit;
        baselineLab.Platelet = request.Platelet;
        baselineLab.Mcv = request.Mcv;
        baselineLab.Rdw = request.Rdw;
        baselineLab.Creatinine = request.Creatinine;
        baselineLab.Urea = request.Urea;
        baselineLab.Egfr = request.Egfr;
        baselineLab.Sodium = request.Sodium;
        baselineLab.Potassium = request.Potassium;
        baselineLab.Magnesium = request.Magnesium;
        baselineLab.Calcium = request.Calcium;
        baselineLab.Chloride = request.Chloride;
        baselineLab.Ast = request.Ast;
        baselineLab.Alt = request.Alt;
        baselineLab.Alp = request.Alp;
        baselineLab.Ggt = request.Ggt;
        baselineLab.TotalBilirubin = request.TotalBilirubin;
        baselineLab.DirectBilirubin = request.DirectBilirubin;
        baselineLab.Ldh = request.Ldh;
        baselineLab.Albumin = request.Albumin;
        baselineLab.TotalProtein = request.TotalProtein;
        baselineLab.Prealbumin = request.Prealbumin;
        baselineLab.Ferritin = request.Ferritin;
        baselineLab.Iron = request.Iron;
        baselineLab.TransferrinSaturation = request.TransferrinSaturation;
        baselineLab.VitaminB12 = request.VitaminB12;
        baselineLab.Folate = request.Folate;
        baselineLab.VitaminD = request.VitaminD;
        baselineLab.Zinc = request.Zinc;
        baselineLab.Selenium = request.Selenium;
        baselineLab.Tsh = request.Tsh;
        baselineLab.FreeT4 = request.FreeT4;
        baselineLab.Glucose = request.Glucose;
        baselineLab.MorningCortisol = request.MorningCortisol;
        baselineLab.Amylase = request.Amylase;
        baselineLab.Lipase = request.Lipase;
        baselineLab.LabDate = request.LabDate.Date;
        baselineLab.Notes = request.Notes;
        baselineLab.UpdatedDate = DateTime.UtcNow;

        activeCycle.StartDate = request.LabDate.Date;
        activeCycle.EndDate = request.LabDate.Date.AddDays(cycleLengthDays - 1);
        activeCycle.CycleSummary = "Active baseline lab result was corrected. No new cycle was created. Planned monitoring window was recalculated.";
        activeCycle.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            patientId,
            labCycleId = activeCycle.Id,
            baselineLabId = baselineLab.Id,
            mode = "correct_current_baseline",
            cycleStartDate = activeCycle.StartDate.ToString("yyyy-MM-dd"),
            plannedCycleEndDate = activeCycle.EndDate?.ToString("yyyy-MM-dd"),
            cycleLengthDays,
            message = "Current active baseline lab result corrected successfully. No new lab cycle was created."
        });
    }

    private static List<string> ValidateRequiredBaselineFields(CreateLabResultRequest request)
    {
        var missing = new List<string>();

        if (request.PatientId <= 0) missing.Add("Patient ID");
        if (request.LabDate == default || request.LabDate.Year < 1900) missing.Add("Lab date");
        else if (request.LabDate.Date > DateTime.Now.Date) missing.Add("Lab date cannot be in the future");

        var cycleLengthDays = request.CycleLengthDays ?? DefaultCycleLengthDays;
        if (cycleLengthDays < MinCycleLengthDays || cycleLengthDays > MaxCycleLengthDays)
            missing.Add($"Cycle length must be between {MinCycleLengthDays} and {MaxCycleLengthDays} days");

        AddMissingIfNullOrInvalid(request.Anc, "ANC", missing);
        AddMissingIfNullOrInvalid(request.Wbc, "WBC", missing);
        AddMissingIfNullOrInvalid(request.Lymphocyte, "Lymphocyte", missing);
        AddMissingIfNullOrInvalid(request.Monocyte, "Monocyte", missing);
        AddMissingIfNullOrInvalid(request.Crp, "CRP", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Esr, "ESR", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Procalcitonin, "Procalcitonin", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Hemoglobin, "Hemoglobin", missing);
        AddMissingIfNullOrInvalid(request.Hematocrit, "Hematocrit", missing);
        AddMissingIfNullOrInvalid(request.Platelet, "Platelet", missing);
        AddMissingIfNullOrInvalid(request.Mcv, "MCV", missing);
        AddMissingIfNullOrInvalid(request.Rdw, "RDW", missing);
        AddMissingIfNullOrInvalid(request.Creatinine, "Creatinine", missing);
        AddMissingIfNullOrInvalid(request.Urea, "Urea", missing);
        AddMissingIfNullOrInvalid(request.Egfr, "eGFR", missing);
        AddMissingIfNullOrInvalid(request.Sodium, "Sodium", missing);
        AddMissingIfNullOrInvalid(request.Potassium, "Potassium", missing);
        AddMissingIfNullOrInvalid(request.Magnesium, "Magnesium", missing);
        AddMissingIfNullOrInvalid(request.Calcium, "Calcium", missing);
        AddMissingIfNullOrInvalid(request.Chloride, "Chloride", missing);
        AddMissingIfNullOrInvalid(request.Ast, "AST", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Alt, "ALT", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Alp, "ALP", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Ggt, "GGT", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.TotalBilirubin, "Total Bilirubin", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.DirectBilirubin, "Direct Bilirubin", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Ldh, "LDH", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Albumin, "Albumin", missing);
        AddMissingIfNullOrInvalid(request.TotalProtein, "Total Protein", missing);
        AddMissingIfNullOrInvalid(request.Prealbumin, "Prealbumin", missing);
        AddMissingIfNullOrInvalid(request.Ferritin, "Ferritin", missing);
        AddMissingIfNullOrInvalid(request.Iron, "Iron", missing);
        AddMissingIfNullOrInvalid(request.TransferrinSaturation, "Transferrin Saturation", missing);
        AddMissingIfNullOrInvalid(request.VitaminB12, "Vitamin B12", missing);
        AddMissingIfNullOrInvalid(request.Folate, "Folate", missing);
        AddMissingIfNullOrInvalid(request.VitaminD, "Vitamin D", missing);
        AddMissingIfNullOrInvalid(request.Zinc, "Zinc", missing);
        AddMissingIfNullOrInvalid(request.Selenium, "Selenium", missing);
        AddMissingIfNullOrInvalid(request.Tsh, "TSH", missing);
        AddMissingIfNullOrInvalid(request.FreeT4, "Free T4", missing);
        AddMissingIfNullOrInvalid(request.Glucose, "Glucose", missing);
        AddMissingIfNullOrInvalid(request.MorningCortisol, "Morning Cortisol", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Amylase, "Amylase", missing, allowZero: true);
        AddMissingIfNullOrInvalid(request.Lipase, "Lipase", missing, allowZero: true);

        return missing;
    }

    private static int NormalizeCycleLength(int? cycleLengthDays)
    {
        var value = cycleLengthDays ?? DefaultCycleLengthDays;
        if (value < MinCycleLengthDays || value > MaxCycleLengthDays)
            return DefaultCycleLengthDays;
        return value;
    }

    private static DateTime GetExpectedCycleEnd(DateTime startDate, DateTime? storedEndDate)
    {
        if (storedEndDate.HasValue)
        {
            var end = storedEndDate.Value.Date;
            var length = (end - startDate.Date).Days + 1;
            if (length >= MinCycleLengthDays && length <= MaxCycleLengthDays)
                return end;
        }
        return startDate.Date.AddDays(DefaultCycleLengthDays - 1);
    }

    private static void AddMissingIfNullOrInvalid(double? value, string name, List<string> missing, bool allowZero = false)
    {
        if (value == null)
        {
            missing.Add(name);
            return;
        }

        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0 || (!allowZero && value.Value == 0))
        {
            missing.Add(name + " must be a valid positive value");
        }
    }
}
