using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly AppDbContext _context;

    public SeedController(AppDbContext context)
    {
        _context = context;
    }

    // Tek tikla: dolu bir test hastasi olusturur (profil + aktif cycle + baseline lab
    // + son 7 gun log). Donen patientId'yi evaluate/{id} ile test edebilirsin.
    [HttpPost("test-patient")]
    public async Task<IActionResult> SeedTestPatient()
    {
        // 1) User -> Hospital -> Doctor (Patient'in zorunlu FK'leri)
        var hospital = new Hospital { Name = "Test Hospital", City = "Istanbul" };
        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Username = $"testpatient_{Guid.NewGuid():N}",
            Email = $"testpatient_{Guid.NewGuid():N}@example.com",
            PasswordHash = "seed",
            RoleId = 1
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            HospitalId = hospital.Id
        };
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        // 2) Patient (kotu durumda bir hasta: dusuk ANC, atesli, kilo kaybi)
        var patient = new Patient
        {
            UserId = user.Id,
            HospitalId = hospital.Id,
            DoctorId = doctor.Id,
            Age = 62,
            Height = 170,
            Weight = 68,
            Gender = Gender.Male,
            CancerType = CancerType.Colon,
            TreatmentType = TreatmentType.Chemotherapy
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // 3) Baseline LabResult (dusuk ANC = febril notropeni riski)
        var lab = new LabResult
        {
            PatientId = patient.Id,
            LabDate = DateTime.Now.Date.AddDays(-7),
            Anc = 0.8,
            Wbc = 3.2,
            Crp = 45,
            Albumin = 3.1,
            Creatinine = 1.1,
            Ast = 30,
            Alt = 28,
            Platelet = 140,
            Hemoglobin = 10.5,
            TotalBilirubin = 0.8,
            Tsh = 2.0,
            FreeT4 = 1.2
        };
        _context.LabResults.Add(lab);
        await _context.SaveChangesAsync();

        // 4) Aktif LabCycle (baseline = yukaridaki lab)
        var cycle = new LabCycle
        {
            PatientId = patient.Id,
            BaselineLabId = lab.Id,
            StartDate = DateTime.Now.Date.AddDays(-7),
            Status = CycleStatus.Active
        };
        _context.LabCycles.Add(cycle);
        await _context.SaveChangesAsync();

        // 5) Son 7 gun: her gun DailyLog + Symptom + Hydration + Medication + Vitals + Nutrition
        for (int i = 6; i >= 0; i--)
        {
            var logDate = DateTime.Now.Date.AddDays(-i);

            var daily = new DailyLog
            {
                PatientId = patient.Id,
                LogDate = logDate,
                GeneralConditionScore = 3,
                IsCompleted = true,
                IsMissingLog = false
            };
            _context.DailyLogs.Add(daily);
            await _context.SaveChangesAsync();

            _context.SymptomLogs.Add(new SymptomLog
            {
                DailyLogId = daily.Id,
                BodyTemperature = 38.5,          // atesli
                Fatigue = SymptomGrade.Moderate,
                Pain = SymptomGrade.Mild,
                Nausea = SymptomGrade.Moderate,
                VomitingCount = 1,
                DiarrheaCount = 0,
                Cough = SymptomGrade.Mild,
                Dyspnea = SymptomGrade.None,
                SkinRash = SymptomGrade.None,
                HasBleedingOrBruising = false,
                HasDizziness = true,
                HasConfusion = false
            });

            _context.HydrationLogs.Add(new HydrationLog
            {
                DailyLogId = daily.Id,
                WaterIntakeMl = 1300,
                DryMouth = SymptomGrade.Mild,
                UrinationCount = 4,
                HasDizziness = true,
                HasVomitingOrDiarrhea = true
            });

            _context.MedicationLogs.Add(new MedicationLog
            {
                DailyLogId = daily.Id,
                TookMainMedication = true,
                MissedDoseCount = 0,
                UsedAntibiotic = false,
                UsedSteroid = false,
                UsedAntiemetic = true,
                UsedPainkiller = false,
                HadSideEffect = false
            });

            _context.VitalSignsLogs.Add(new VitalSignsLog
            {
                DailyLogId = daily.Id,
                SystolicBloodPressure = 110,
                DiastolicBloodPressure = 70,
                HeartRate = 95,
                OxygenSaturation = 95,
                BodyTemperature = 38.5
            });

            _context.NutritionSummaries.Add(new NutritionSummary
            {
                DailyLogId = daily.Id,
                TotalCalories = 1300,
                TotalProtein = 35,
                TotalCarbohydrate = 150,
                TotalFat = 40,
                TotalWaterMl = 1300,
                ProteinTarget = 88,      // ~1.3 g/kg * 68 kg
                CalorieTarget = 1870,    // ~27.5 kcal/kg * 68 kg
                ProteinRatio = 0.40,
                CalorieRatio = 0.70,
                MealCompletionRatio = 0.5,
                IsProteinDeficient = true,
                IsCalorieDeficient = true
            });

            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            message = "Test patient seeded successfully.",
            patientId = patient.Id,
            note = $"Now call POST /api/AiPredictionTest/evaluate/{patient.Id}"
        });
    }
}