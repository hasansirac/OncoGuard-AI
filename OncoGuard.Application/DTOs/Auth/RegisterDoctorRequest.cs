namespace OncoGuard.Application.DTOs.Auth;

public class RegisterDoctorRequest
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int HospitalId { get; set; }
}