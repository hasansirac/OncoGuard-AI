namespace OncoGuard.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserId { get; set; }

    // Rolune gore dolar: doktorsa DoctorId, hastaysa PatientId
    public int? DoctorId { get; set; }
    public int? PatientId { get; set; }
}