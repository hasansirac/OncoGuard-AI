using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HospitalsController : ControllerBase
{
    private readonly AppDbContext _context;

    public HospitalsController(AppDbContext context)
    {
        _context = context;
    }

    /// Kayit ekraninda: tum hastaneler (test hastaneleri gizli)
    [HttpGet]
    public async Task<IActionResult> GetHospitals()
    {
        var hospitals = await _context.Hospitals
            .Where(h => h.Name != "Test Hospital")
            .Select(h => new
            {
                id = h.Id,
                name = h.Name,
                city = h.City
            })
            .ToListAsync();

        return Ok(hospitals);
    }

    // Kayit ekraninda: secilen hastanenin doktorlari
    [HttpGet("{hospitalId}/doctors")]
    public async Task<IActionResult> GetDoctorsByHospital(int hospitalId)
    {
        var doctors = await _context.Doctors
            .Where(d => d.HospitalId == hospitalId)
            .Select(d => new
            {
                id = d.Id,
                name = d.User.Username,
                email = d.User.Email
            })
            .ToListAsync();

        return Ok(doctors);
    }
}