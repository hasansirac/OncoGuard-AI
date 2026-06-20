using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Domain.Enums;
using OncoGuard.Domain.Entities;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientDailyEntryStatusController : ControllerBase
{
    private readonly AppDbContext _context;

    private static readonly string[] AllowedComorbidities =
    {
        "none", "diabetes", "hypertension", "chronic kidney disease", "heart failure", "copd", "liver disease"
    };
    private static readonly string[] AllowedStages = { "Stage I", "Stage II", "Stage III", "Stage IV" };
    private static readonly string[] AllowedSmokingStatuses = { "Never smoker", "Former smoker", "Current smoker" };
    private static readonly string[] AllowedAlcoholStatuses = { "No alcohol", "Occasional", "Regular" };
    private static readonly string[] AllowedTreatmentGoals = { "Curative", "Palliative", "Adjuvant", "Neoadjuvant", "Maintenance" };

    public PatientDailyEntryStatusController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetStatus(int patientId, CancellationToken cancellationToken)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == patientId, cancellationToken);

        if (!patientExists)
        {
            return NotFound(new
            {
                patientId,
                canEnterDailyData = false,
                missingItems = new[] { "Patient not found" },
                message = "Patient not found."
            });
        }

        var missingItems = await GetMissingItemsAsync(patientId, cancellationToken);
        var canEnterDailyData = missingItems.Count == 0;

        return Ok(new
        {
            patientId,
            canEnterDailyData,
            missingItems,
            doctorRequiredSections = new[]
            {
                "Complete clinical profile",
                "Complete treatment plan/history",
                "Complete active baseline lab cycle"
            },
            message = canEnterDailyData
                ? "Daily data entry is enabled. The doctor has completed all required clinical profile, treatment plan and baseline lab fields."
                : "Daily data entry is waiting for doctor completion. The doctor must complete all missing items first."
        });
    }

    private async Task<List<string>> GetMissingItemsAsync(int patientId, CancellationToken cancellationToken)
    {
        var missing = new List<string>();

        var cancerProfile = await _context.CancerProfiles
            .Where(cp => cp.PatientId == patientId)
            .OrderByDescending(cp => cp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (cancerProfile == null)
        {
            missing.Add("Clinical profile");
        }
        else
        {
            if (!Enum.IsDefined(typeof(CancerType), cancerProfile.CancerType)) missing.Add("Cancer type");
            if (string.IsNullOrWhiteSpace(cancerProfile.Stage) || !AllowedStages.Contains(cancerProfile.Stage.Trim(), StringComparer.OrdinalIgnoreCase)) missing.Add("Cancer stage");
            if (cancerProfile.DiagnosisDate == default || cancerProfile.DiagnosisDate.Year < 1900) missing.Add("Diagnosis date");
            if (!IsValidComorbidities(cancerProfile.Comorbidities)) missing.Add("Valid comorbidities");
            if (string.IsNullOrWhiteSpace(cancerProfile.SmokingStatus) || !AllowedSmokingStatuses.Contains(cancerProfile.SmokingStatus.Trim(), StringComparer.OrdinalIgnoreCase)) missing.Add("Smoking status");
            if (string.IsNullOrWhiteSpace(cancerProfile.AlcoholStatus) || !AllowedAlcoholStatuses.Contains(cancerProfile.AlcoholStatus.Trim(), StringComparer.OrdinalIgnoreCase)) missing.Add("Alcohol status");
            if (cancerProfile.EcogPerformanceScore < 0 || cancerProfile.EcogPerformanceScore > 4) missing.Add("ECOG performance score");
            if (string.IsNullOrWhiteSpace(cancerProfile.TreatmentGoal) || !AllowedTreatmentGoals.Contains(cancerProfile.TreatmentGoal.Trim(), StringComparer.OrdinalIgnoreCase)) missing.Add("Treatment goal");
        }

        var treatmentPlan = await _context.TreatmentPlans
            .Where(tp => tp.PatientId == patientId)
            .OrderByDescending(tp => tp.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (treatmentPlan == null)
        {
            missing.Add("Treatment plan");
        }
        else
        {
            if (!Enum.IsDefined(typeof(TreatmentType), treatmentPlan.TreatmentType)) missing.Add("Treatment type");
            if (string.IsNullOrWhiteSpace(treatmentPlan.ProtocolName)) missing.Add("Treatment protocol name");
            if (treatmentPlan.CycleNumber <= 0) missing.Add("Treatment cycle number");
            if (treatmentPlan.LastTreatmentDate == default || treatmentPlan.LastTreatmentDate.Year < 1900) missing.Add("Last treatment date");
            if (treatmentPlan.NextPlannedTreatmentDate == null || treatmentPlan.NextPlannedTreatmentDate.Value == default || treatmentPlan.NextPlannedTreatmentDate.Value.Year < 1900) missing.Add("Next planned treatment date");
        }

        var activeCycle = await _context.LabCycles
            .Where(lc => lc.PatientId == patientId && lc.Status == CycleStatus.Active)
            .OrderByDescending(lc => lc.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCycle == null)
        {
            missing.Add("Baseline lab cycle");
            return missing;
        }

        var baselineLab = await _context.LabResults
            .FirstOrDefaultAsync(lr => lr.Id == activeCycle.BaselineLabId, cancellationToken);

        if (baselineLab == null)
        {
            missing.Add("Baseline lab result");
            return missing;
        }

        foreach (var item in GetMissingBaselineLabItems(baselineLab))
            missing.Add(item);

        return missing;
    }

    private static List<string> GetMissingBaselineLabItems(LabResult lab)
    {
        var missing = new List<string>();
        if (lab.LabDate == default || lab.LabDate.Year < 1900) missing.Add("Baseline lab date");
        else if (lab.LabDate.Date > DateTime.Now.Date) missing.Add("Baseline lab date cannot be in the future");
        if (lab.Anc == null) missing.Add("Baseline ANC");
        if (lab.Wbc == null) missing.Add("Baseline WBC");
        if (lab.Lymphocyte == null) missing.Add("Baseline lymphocyte");
        if (lab.Monocyte == null) missing.Add("Baseline monocyte");
        if (lab.Crp == null) missing.Add("Baseline CRP");
        if (lab.Esr == null) missing.Add("Baseline ESR");
        if (lab.Procalcitonin == null) missing.Add("Baseline procalcitonin");
        if (lab.Hemoglobin == null) missing.Add("Baseline hemoglobin");
        if (lab.Hematocrit == null) missing.Add("Baseline hematocrit");
        if (lab.Platelet == null) missing.Add("Baseline platelet");
        if (lab.Mcv == null) missing.Add("Baseline MCV");
        if (lab.Rdw == null) missing.Add("Baseline RDW");
        if (lab.Creatinine == null) missing.Add("Baseline creatinine");
        if (lab.Urea == null) missing.Add("Baseline urea");
        if (lab.Egfr == null) missing.Add("Baseline eGFR");
        if (lab.Sodium == null) missing.Add("Baseline sodium");
        if (lab.Potassium == null) missing.Add("Baseline potassium");
        if (lab.Magnesium == null) missing.Add("Baseline magnesium");
        if (lab.Calcium == null) missing.Add("Baseline calcium");
        if (lab.Chloride == null) missing.Add("Baseline chloride");
        if (lab.Ast == null) missing.Add("Baseline AST");
        if (lab.Alt == null) missing.Add("Baseline ALT");
        if (lab.Alp == null) missing.Add("Baseline ALP");
        if (lab.Ggt == null) missing.Add("Baseline GGT");
        if (lab.TotalBilirubin == null) missing.Add("Baseline total bilirubin");
        if (lab.DirectBilirubin == null) missing.Add("Baseline direct bilirubin");
        if (lab.Ldh == null) missing.Add("Baseline LDH");
        if (lab.Albumin == null) missing.Add("Baseline albumin");
        if (lab.TotalProtein == null) missing.Add("Baseline total protein");
        if (lab.Prealbumin == null) missing.Add("Baseline prealbumin");
        if (lab.Ferritin == null) missing.Add("Baseline ferritin");
        if (lab.Iron == null) missing.Add("Baseline iron");
        if (lab.TransferrinSaturation == null) missing.Add("Baseline transferrin saturation");
        if (lab.VitaminB12 == null) missing.Add("Baseline vitamin B12");
        if (lab.Folate == null) missing.Add("Baseline folate");
        if (lab.VitaminD == null) missing.Add("Baseline vitamin D");
        if (lab.Zinc == null) missing.Add("Baseline zinc");
        if (lab.Selenium == null) missing.Add("Baseline selenium");
        if (lab.Tsh == null) missing.Add("Baseline TSH");
        if (lab.FreeT4 == null) missing.Add("Baseline Free T4");
        if (lab.Glucose == null) missing.Add("Baseline glucose");
        if (lab.MorningCortisol == null) missing.Add("Baseline morning cortisol");
        if (lab.Amylase == null) missing.Add("Baseline amylase");
        if (lab.Lipase == null) missing.Add("Baseline lipase");
        return missing;
    }

    private static bool IsValidComorbidities(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var selected = input.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => NormalizeOneComorbidity(x.Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (selected.Count == 0) return false;
        if (selected.Any(x => !AllowedComorbidities.Contains(x, StringComparer.OrdinalIgnoreCase))) return false;
        if (selected.Contains("none", StringComparer.OrdinalIgnoreCase) && selected.Count > 1) return false;
        return true;
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
