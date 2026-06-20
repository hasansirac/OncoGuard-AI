using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicalProfilesController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly string[] AllowedComorbidities =
    {
        "none",
        "diabetes",
        "hypertension",
        "chronic kidney disease",
        "heart failure",
        "copd",
        "liver disease"
    };

    private static readonly string[] AllowedStages = { "Stage I", "Stage II", "Stage III", "Stage IV" };
    private static readonly string[] AllowedSmokingStatuses = { "Never smoker", "Former smoker", "Current smoker" };
    private static readonly string[] AllowedAlcoholStatuses = { "No alcohol", "Occasional", "Regular" };
    private static readonly string[] AllowedTreatmentGoals = { "Curative", "Palliative", "Adjuvant", "Neoadjuvant", "Maintenance" };

    public ClinicalProfilesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        var cancerProfile = await _context.CancerProfiles
            .AsNoTracking()
            .Where(cp => cp.PatientId == patientId)
            .OrderByDescending(cp => cp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        var treatmentPlan = await _context.TreatmentPlans
            .AsNoTracking()
            .Where(tp => tp.PatientId == patientId)
            .OrderByDescending(tp => tp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            patientId,
            status = new
            {
                hasClinicalProfile = cancerProfile != null,
                hasTreatmentPlan = treatmentPlan != null,
                mode = cancerProfile != null && treatmentPlan != null ? "editing_existing" : "creating_new",
                lastClinicalUpdate = cancerProfile?.UpdatedDate ?? cancerProfile?.CreatedDate,
                lastTreatmentUpdate = treatmentPlan?.UpdatedDate ?? treatmentPlan?.CreatedDate
            },
            cancerProfile,
            treatmentPlan
        });
    }

    [HttpPost("upsert")]
    public async Task<IActionResult> Upsert(UpsertClinicalProfileRequest request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);

        if (patient == null)
            return NotFound(new { message = "Patient not found." });

        var validation = ValidateRequest(request);
        if (validation.Count > 0)
        {
            return BadRequest(new
            {
                message = "Clinical profile cannot be saved because required doctor-entered fields are missing or invalid.",
                missingOrInvalidItems = validation,
                allowedComorbidities = AllowedComorbidities,
                allowedStages = AllowedStages,
                allowedSmokingStatuses = AllowedSmokingStatuses,
                allowedAlcoholStatuses = AllowedAlcoholStatuses,
                allowedTreatmentGoals = AllowedTreatmentGoals
            });
        }

        var normalizedComorbidities = NormalizeComorbidities(request.Comorbidities, out var comorbidityError);
        if (normalizedComorbidities == null)
        {
            return BadRequest(new
            {
                message = comorbidityError,
                allowedValues = AllowedComorbidities
            });
        }

        var now = DateTime.UtcNow;

        var cancerProfile = await _context.CancerProfiles
            .Where(cp => cp.PatientId == request.PatientId)
            .OrderByDescending(cp => cp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        var clinicalMode = cancerProfile == null ? "created_new_clinical_profile" : "updated_existing_clinical_profile";

        if (cancerProfile == null)
        {
            cancerProfile = new CancerProfile
            {
                PatientId = request.PatientId,
                CreatedDate = now
            };

            _context.CancerProfiles.Add(cancerProfile);
        }
        else
        {
            cancerProfile.UpdatedDate = now;
        }

        cancerProfile.CancerType = (CancerType)request.CancerType!.Value;
        cancerProfile.Stage = request.Stage!.Trim();
        cancerProfile.HasMetastasis = request.HasMetastasis!.Value;
        cancerProfile.DiagnosisDate = request.DiagnosisDate!.Value;
        cancerProfile.Comorbidities = normalizedComorbidities;
        cancerProfile.SmokingStatus = request.SmokingStatus!.Trim();
        cancerProfile.AlcoholStatus = request.AlcoholStatus!.Trim();
        cancerProfile.EcogPerformanceScore = request.EcogPerformanceScore!.Value;
        cancerProfile.TreatmentGoal = request.TreatmentGoal!.Trim();

        var treatmentPlan = await _context.TreatmentPlans
            .Where(tp => tp.PatientId == request.PatientId)
            .OrderByDescending(tp => tp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        var treatmentMode = treatmentPlan == null ? "created_new_treatment_plan" : "updated_existing_treatment_plan";

        if (treatmentPlan == null)
        {
            treatmentPlan = new TreatmentPlan
            {
                PatientId = request.PatientId,
                CreatedDate = now
            };

            _context.TreatmentPlans.Add(treatmentPlan);
        }
        else
        {
            treatmentPlan.UpdatedDate = now;
        }

        treatmentPlan.TreatmentType = (TreatmentType)request.TreatmentType!.Value;
        treatmentPlan.ProtocolName = request.ProtocolName!.Trim();
        treatmentPlan.CycleNumber = request.CycleNumber!.Value;
        treatmentPlan.LastTreatmentDate = request.LastTreatmentDate!.Value;
        treatmentPlan.NextPlannedTreatmentDate = request.NextPlannedTreatmentDate!.Value;
        treatmentPlan.HasPreviousNeutropenia = request.HasPreviousNeutropenia!.Value;
        treatmentPlan.UsesGcsfSupport = request.UsesGcsfSupport!.Value;
        treatmentPlan.UsesAntibiotic = request.UsesAntibiotic!.Value;
        treatmentPlan.UsesSteroid = request.UsesSteroid!.Value;
        treatmentPlan.UsesAntiemetic = request.UsesAntiemetic!.Value;
        treatmentPlan.UsesPainkiller = request.UsesPainkiller!.Value;
        treatmentPlan.HasDoseReduction = request.HasDoseReduction!.Value;
        treatmentPlan.HadPreviousTreatmentDelay = request.HadPreviousTreatmentDelay!.Value;

        patient.CancerType = (CancerType)request.CancerType!.Value;
        patient.TreatmentType = (TreatmentType)request.TreatmentType!.Value;
        patient.UpdatedDate = now;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Clinical profile and treatment plan saved successfully. These doctor-entered values will be used in AI risk prediction.",
            patientId = request.PatientId,
            cancerProfileId = cancerProfile.Id,
            treatmentPlanId = treatmentPlan.Id,
            clinicalMode,
            treatmentMode,
            comorbidities = normalizedComorbidities,
            readyForDailyEntryClinicalPart = true
        });
    }

    private static List<string> ValidateRequest(UpsertClinicalProfileRequest request)
    {
        var missing = new List<string>();

        if (request.CancerType == null || !Enum.IsDefined(typeof(CancerType), request.CancerType.Value))
            missing.Add("Cancer type");

        if (string.IsNullOrWhiteSpace(request.Stage) || !AllowedStages.Contains(request.Stage.Trim(), StringComparer.OrdinalIgnoreCase))
            missing.Add("Cancer stage");

        if (request.HasMetastasis == null)
            missing.Add("Metastasis status");

        if (request.DiagnosisDate == null || request.DiagnosisDate.Value == default || request.DiagnosisDate.Value.Year < 1900)
            missing.Add("Diagnosis date");

        if (string.IsNullOrWhiteSpace(request.Comorbidities))
            missing.Add("Comorbidities");

        if (string.IsNullOrWhiteSpace(request.SmokingStatus) || !AllowedSmokingStatuses.Contains(request.SmokingStatus.Trim(), StringComparer.OrdinalIgnoreCase))
            missing.Add("Smoking status");

        if (string.IsNullOrWhiteSpace(request.AlcoholStatus) || !AllowedAlcoholStatuses.Contains(request.AlcoholStatus.Trim(), StringComparer.OrdinalIgnoreCase))
            missing.Add("Alcohol status");

        if (request.EcogPerformanceScore == null || request.EcogPerformanceScore.Value < 0 || request.EcogPerformanceScore.Value > 4)
            missing.Add("ECOG performance score");

        if (string.IsNullOrWhiteSpace(request.TreatmentGoal) || !AllowedTreatmentGoals.Contains(request.TreatmentGoal.Trim(), StringComparer.OrdinalIgnoreCase))
            missing.Add("Treatment goal");

        if (request.TreatmentType == null || !Enum.IsDefined(typeof(TreatmentType), request.TreatmentType.Value))
            missing.Add("Treatment type");

        if (string.IsNullOrWhiteSpace(request.ProtocolName))
            missing.Add("Treatment protocol name");

        if (request.CycleNumber == null || request.CycleNumber.Value <= 0)
            missing.Add("Treatment cycle number");

        if (request.LastTreatmentDate == null || request.LastTreatmentDate.Value == default || request.LastTreatmentDate.Value.Year < 1900)
            missing.Add("Last treatment date");

        if (request.NextPlannedTreatmentDate == null || request.NextPlannedTreatmentDate.Value == default || request.NextPlannedTreatmentDate.Value.Year < 1900)
            missing.Add("Next planned treatment date");

        if (request.HasPreviousNeutropenia == null) missing.Add("Previous neutropenia status");
        if (request.UsesGcsfSupport == null) missing.Add("G-CSF support status");
        if (request.UsesAntibiotic == null) missing.Add("Antibiotic use status");
        if (request.UsesSteroid == null) missing.Add("Steroid use status");
        if (request.UsesAntiemetic == null) missing.Add("Antiemetic use status");
        if (request.UsesPainkiller == null) missing.Add("Painkiller use status");
        if (request.HasDoseReduction == null) missing.Add("Dose reduction status");
        if (request.HadPreviousTreatmentDelay == null) missing.Add("Previous treatment delay status");

        return missing;
    }

    private static string? NormalizeComorbidities(string? input, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "At least one comorbidity option must be selected. Select 'none' if the patient has no known comorbidity.";
            return null;
        }

        var selected = input
            .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => NormalizeOneComorbidity(x.Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count == 0)
        {
            error = "At least one valid comorbidity option must be selected.";
            return null;
        }

        var invalid = selected
            .Where(x => !AllowedComorbidities.Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (invalid.Count > 0)
        {
            error = "Invalid comorbidity value(s): " + string.Join(", ", invalid) +
                    ". Use only the predefined comorbidity options.";
            return null;
        }

        if (selected.Contains("none", StringComparer.OrdinalIgnoreCase) && selected.Count > 1)
        {
            error = "If 'none' is selected, no other comorbidity should be selected.";
            return null;
        }

        return string.Join(", ", selected);
    }

    private static string NormalizeOneComorbidity(string value)
    {
        var v = value.Trim().ToLowerInvariant();

        return v switch
        {
            "none" or "no" or "no known comorbidity" or "no comorbidity" => "none",
            "diabetes" or "diabetes mellitus" or "dm" => "diabetes",
            "hypertension" or "htn" or "high blood pressure" => "hypertension",
            "chronic kidney disease" or "ckd" or "renal disease" or "kidney disease" => "chronic kidney disease",
            "heart failure" or "chf" or "cardiac failure" => "heart failure",
            "copd" or "asthma" or "chronic obstructive pulmonary disease" => "copd",
            "liver disease" or "cirrhosis" or "hepatic disease" => "liver disease",
            _ => v
        };
    }
}

public class UpsertClinicalProfileRequest
{
    public int PatientId { get; set; }

    public int? CancerType { get; set; }
    public string? Stage { get; set; }
    public bool? HasMetastasis { get; set; }
    public DateTime? DiagnosisDate { get; set; }
    public string? Comorbidities { get; set; }
    public string? SmokingStatus { get; set; }
    public string? AlcoholStatus { get; set; }
    public int? EcogPerformanceScore { get; set; }
    public string? TreatmentGoal { get; set; }

    public int? TreatmentType { get; set; }
    public string? ProtocolName { get; set; }
    public int? CycleNumber { get; set; }
    public DateTime? LastTreatmentDate { get; set; }
    public DateTime? NextPlannedTreatmentDate { get; set; }
    public bool? HasPreviousNeutropenia { get; set; }
    public bool? UsesGcsfSupport { get; set; }
    public bool? UsesAntibiotic { get; set; }
    public bool? UsesSteroid { get; set; }
    public bool? UsesAntiemetic { get; set; }
    public bool? UsesPainkiller { get; set; }
    public bool? HasDoseReduction { get; set; }
    public bool? HadPreviousTreatmentDelay { get; set; }
}