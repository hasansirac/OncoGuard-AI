using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.DailyLogs;
using OncoGuard.Application.Interfaces.DailyLogs;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DailyLogsController : ControllerBase
{
    private const int DefaultCycleLengthDays = 21;
    private const int MinCycleLengthDays = 7;
    private const int MaxCycleLengthDays = 35;

    private readonly IDailyLogService _dailyLogService;
    private readonly AppDbContext _context;

    public DailyLogsController(IDailyLogService dailyLogService, AppDbContext context)
    {
        _dailyLogService = dailyLogService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDailyLog(CreateDailyLogRequest request)
    {
        try
        {
            var dailyLogId = await _dailyLogService.CreateDailyLogAsync(request);

            return Ok(new
            {
                dailyLogId,
                message = "Daily log saved successfully. If a record already existed for this date, it was updated."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // Android date board:
    // Returns the full planned monitoring cycle calendar, usually 21 days.
    // Today/past dates can be created or updated; future dates are visible but locked.
    [HttpGet("patient/{patientId:int}/calendar")]
    public async Task<IActionResult> GetPatientDailyEntryCalendar(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == patientId, cancellationToken);

        if (!patientExists)
            return NotFound(new { patientId, message = "Patient not found." });

        var activeCycle = await _context.LabCycles
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            return BadRequest(new
            {
                patientId,
                message = "No active lab cycle exists. The doctor must create a baseline lab cycle before daily monitoring dates are opened."
            });
        }

        var today = DateTime.Now.Date;
        var cycleStart = activeCycle.StartDate.Date;
        var plannedEnd = GetExpectedCycleEnd(cycleStart, activeCycle.EndDate);
        var cycleLengthDays = (plannedEnd - cycleStart).Days + 1;

        var existingLogs = await _context.DailyLogs
            .AsNoTracking()
            .Where(d => d.PatientId == patientId && d.LogDate.Date >= cycleStart && d.LogDate.Date <= plannedEnd)
            .Select(d => new { d.Id, Date = d.LogDate.Date })
            .ToListAsync(cancellationToken);

        var logMap = existingLogs
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().Id);

        var days = new List<object>();
        for (var date = cycleStart; date <= plannedEnd; date = date.AddDays(1))
        {
            var hasLog = logMap.TryGetValue(date, out var dailyLogId);
            var isFuture = date > today;
            var canEdit = !isFuture;
            var cycleDay = (date - cycleStart).Days + 1;

            days.Add(new
            {
                date = date.ToString("yyyy-MM-dd"),
                displayLabel = GetDisplayLabel(date, today, cycleDay, cycleLengthDays),
                cycleDay,
                cycleLengthDays,
                isToday = date == today,
                isFuture,
                canEdit,
                hasLog,
                dailyLogId = hasLog ? dailyLogId : (int?)null,
                status = isFuture ? "Upcoming" : (hasLog ? "Filled" : "Missing")
            });
        }

        return Ok(new
        {
            patientId,
            activeCycleId = activeCycle.Id,
            startDate = cycleStart.ToString("yyyy-MM-dd"),
            endDate = plannedEnd.ToString("yyyy-MM-dd"),
            cycleLengthDays,
            currentCycleDay = today < cycleStart ? 0 : Math.Min(cycleLengthDays, (today - cycleStart).Days + 1),
            days,
            message = "Daily monitoring cycle calendar loaded. Past/current dates can be created or updated. Future dates are visible but locked until that day arrives."
        });
    }

    // Android date form prefill:
    // Returns the saved values for a selected date, or hasLog=false if the day is empty.
    [HttpGet("patient/{patientId:int}/date/{date}")]
    public async Task<IActionResult> GetPatientDailyLogByDate(
        int patientId,
        string date,
        CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest(new
            {
                patientId,
                date,
                message = "Invalid date format. Use yyyy-MM-dd."
            });
        }

        var logDate = parsedDate.Date;
        var today = DateTime.Now.Date;

        var activeCycle = await _context.LabCycles
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.Status == CycleStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            return BadRequest(new
            {
                patientId,
                date = logDate.ToString("yyyy-MM-dd"),
                message = "No active lab cycle exists. The doctor must create a baseline lab cycle first."
            });
        }

        var cycleStart = activeCycle.StartDate.Date;
        var plannedEnd = GetExpectedCycleEnd(cycleStart, activeCycle.EndDate);

        if (logDate < cycleStart || logDate > plannedEnd)
        {
            return BadRequest(new
            {
                patientId,
                date = logDate.ToString("yyyy-MM-dd"),
                cycleStart = cycleStart.ToString("yyyy-MM-dd"),
                plannedCycleEndDate = plannedEnd.ToString("yyyy-MM-dd"),
                message = "Selected date is outside the active monitoring cycle."
            });
        }

        if (logDate > today)
        {
            return BadRequest(new
            {
                patientId,
                date = logDate.ToString("yyyy-MM-dd"),
                message = "Future daily entries are locked. The patient can enter only today or previous monitoring dates."
            });
        }

        var dailyLog = await _context.DailyLogs
            .AsNoTracking()
            .Where(d => d.PatientId == patientId && d.LogDate.Date == logDate)
            .OrderByDescending(d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (dailyLog == null)
        {
            return Ok(new
            {
                patientId,
                date = logDate.ToString("yyyy-MM-dd"),
                hasLog = false,
                canEdit = true,
                message = "No daily entry exists for this date yet. Saving the form will create it."
            });
        }

        var symptom = await _context.SymptomLogs.AsNoTracking()
            .FirstOrDefaultAsync(s => s.DailyLogId == dailyLog.Id, cancellationToken);
        var hydration = await _context.HydrationLogs.AsNoTracking()
            .FirstOrDefaultAsync(h => h.DailyLogId == dailyLog.Id, cancellationToken);
        var medication = await _context.MedicationLogs.AsNoTracking()
            .FirstOrDefaultAsync(m => m.DailyLogId == dailyLog.Id, cancellationToken);
        var vitals = await _context.VitalSignsLogs.AsNoTracking()
            .FirstOrDefaultAsync(v => v.DailyLogId == dailyLog.Id, cancellationToken);
        var nutrition = await _context.NutritionSummaries.AsNoTracking()
            .FirstOrDefaultAsync(n => n.DailyLogId == dailyLog.Id, cancellationToken);

        return Ok(new
        {
            patientId,
            date = logDate.ToString("yyyy-MM-dd"),
            hasLog = true,
            canEdit = true,
            dailyLogId = dailyLog.Id,
            generalConditionScore = dailyLog.GeneralConditionScore,
            patientNote = dailyLog.PatientNote,

            bodyTemperature = symptom?.BodyTemperature,
            fatigue = symptom?.Fatigue,
            pain = symptom?.Pain,
            nausea = symptom?.Nausea,
            vomitingCount = symptom?.VomitingCount,
            diarrheaCount = symptom?.DiarrheaCount,
            cough = symptom?.Cough,
            dyspnea = symptom?.Dyspnea,
            hasDizziness = symptom?.HasDizziness,

            waterIntakeMl = hydration?.WaterIntakeMl,
            tookMainMedication = medication?.TookMainMedication,
            missedDoseCount = medication?.MissedDoseCount,
            oxygenSaturation = vitals?.OxygenSaturation,

            protein = nutrition?.TotalProtein,
            calories = nutrition?.TotalCalories,
            message = "Saved daily entry loaded. Saving again will update this date instead of creating a duplicate."
        });
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

    private static string GetDisplayLabel(DateTime date, DateTime today, int cycleDay, int cycleLengthDays)
    {
        var suffix = date == today ? "Today" : date > today ? "Upcoming" : "Past";
        return $"Day {cycleDay}/{cycleLengthDays} · {date:dd.MM.yyyy} ({suffix})";
    }
}
