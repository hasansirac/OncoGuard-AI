using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DoctorsController(AppDbContext context)
    {
        _context = context;
    }

    // Doktor panelinde: bu doktora bagli hastalar
    [HttpGet("{doctorId}/patients")]
    public async Task<IActionResult> GetPatientsByDoctor(int doctorId)
    {
        var patients = await _context.Patients
            .Where(p => p.DoctorId == doctorId)
            .Select(p => new
            {
                id = p.Id,
                name = p.User.Username,
                age = p.Age,
                gender = p.Gender.ToString(),
                cancerType = p.CancerType.ToString(),
                treatmentType = p.TreatmentType.ToString()
            })
            .ToListAsync();

        return Ok(patients);
    }
}