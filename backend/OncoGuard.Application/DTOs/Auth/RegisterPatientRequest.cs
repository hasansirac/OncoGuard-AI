using OncoGuard.Domain.Enums;

namespace OncoGuard.Application.DTOs.Auth;

public class RegisterPatientRequest
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int HospitalId { get; set; }

    public int DoctorId { get; set; }

    public int Age { get; set; }

    public Gender Gender { get; set; }

    public double Height { get; set; }

    public double Weight { get; set; }

    public CancerType CancerType { get; set; }

    public TreatmentType TreatmentType { get; set; }
}
