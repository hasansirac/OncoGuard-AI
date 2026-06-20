using OncoGuard.Application.DTOs.Auth;

namespace OncoGuard.Application.Interfaces.Auth;

public interface IAuthService
{
    Task RegisterDoctorAsync(RegisterDoctorRequest request);

    Task RegisterPatientAsync(RegisterPatientRequest request);

    Task<LoginResponse> LoginAsync(LoginRequest request);
}