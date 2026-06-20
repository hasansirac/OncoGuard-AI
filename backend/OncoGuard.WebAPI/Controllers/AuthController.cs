using Microsoft.AspNetCore.Mvc;
using OncoGuard.Application.DTOs.Auth;
using OncoGuard.Application.Interfaces.Auth;

namespace OncoGuard.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register-doctor")]
    public async Task<IActionResult> RegisterDoctor(RegisterDoctorRequest request)
    {
        await _authService.RegisterDoctorAsync(request);

        return Ok("Doctor registered successfully.");
    }

    [HttpPost("register-patient")]
    public async Task<IActionResult> RegisterPatient(RegisterPatientRequest request)
    {
        await _authService.RegisterPatientAsync(request);

        return Ok("Patient registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}