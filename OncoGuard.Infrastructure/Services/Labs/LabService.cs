using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.Labs;
using OncoGuard.Application.Interfaces.Labs;
using OncoGuard.Domain.Entities;
using OncoGuard.Domain.Enums;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.Labs;

public class LabService : ILabService
{
    private readonly AppDbContext _context;

    public LabService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddLabResultAsync(CreateLabResultRequest request)
    {
        var patientExists = await _context.Patients
            .AnyAsync(p => p.Id == request.PatientId);

        if (!patientExists)
            throw new Exception("Patient not found.");

        var labResult = new LabResult
        {
            PatientId = request.PatientId,

            Wbc = request.Wbc,
            Anc = request.Anc,
            Lymphocyte = request.Lymphocyte,
            Monocyte = request.Monocyte,
            Crp = request.Crp,
            Esr = request.Esr,
            Procalcitonin = request.Procalcitonin,

            Hemoglobin = request.Hemoglobin,
            Hematocrit = request.Hematocrit,
            Platelet = request.Platelet,
            Mcv = request.Mcv,
            Rdw = request.Rdw,

            Creatinine = request.Creatinine,
            Urea = request.Urea,
            Egfr = request.Egfr,
            Sodium = request.Sodium,
            Potassium = request.Potassium,
            Magnesium = request.Magnesium,
            Calcium = request.Calcium,
            Chloride = request.Chloride,

            Ast = request.Ast,
            Alt = request.Alt,
            Alp = request.Alp,
            Ggt = request.Ggt,
            TotalBilirubin = request.TotalBilirubin,
            DirectBilirubin = request.DirectBilirubin,
            Ldh = request.Ldh,

            Albumin = request.Albumin,
            TotalProtein = request.TotalProtein,
            Prealbumin = request.Prealbumin,
            Ferritin = request.Ferritin,
            Iron = request.Iron,
            TransferrinSaturation = request.TransferrinSaturation,
            VitaminB12 = request.VitaminB12,
            Folate = request.Folate,
            VitaminD = request.VitaminD,
            Zinc = request.Zinc,
            Selenium = request.Selenium,

            Tsh = request.Tsh,
            FreeT4 = request.FreeT4,
            Glucose = request.Glucose,
            MorningCortisol = request.MorningCortisol,
            Amylase = request.Amylase,
            Lipase = request.Lipase,

            LabDate = request.LabDate,
            Notes = request.Notes
        };

        await _context.LabResults.AddAsync(labResult);
        await _context.SaveChangesAsync();

        var activeCycle = await _context.LabCycles
            .FirstOrDefaultAsync(c =>
                c.PatientId == request.PatientId &&
                c.Status == CycleStatus.Active);

        if (activeCycle != null)
        {
            activeCycle.Status = CycleStatus.Closed;
            activeCycle.EndDate = request.LabDate;
            activeCycle.CycleSummary = "Cycle closed after new lab result was entered.";
        }

        var newCycle = new LabCycle
        {
            PatientId = request.PatientId,
            BaselineLabId = labResult.Id,
            StartDate = request.LabDate,
            Status = CycleStatus.Active,
            PreviousCycleId = activeCycle?.Id,
            CycleSummary = "New lab cycle started with this lab result as baseline."
        };

        await _context.LabCycles.AddAsync(newCycle);
        await _context.SaveChangesAsync();
    }
}
