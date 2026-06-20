using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.DTOs.Auth;
using OncoGuard.Application.Interfaces.Auth;
using OncoGuard.Domain.Entities;
using OncoGuard.Infrastructure.Persistence;

namespace OncoGuard.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task RegisterDoctorAsync(RegisterDoctorRequest request)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            throw new Exception("This email is already registered.");

        var hospitalExists = await _context.Hospitals
            .AnyAsync(h => h.Id == request.HospitalId);

        if (!hospitalExists)
            throw new Exception("Selected hospital does not exist.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = 2
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            HospitalId = request.HospitalId
        };

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync();
    }

    public async Task RegisterPatientAsync(RegisterPatientRequest request)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            throw new Exception("This email is already registered.");

        var hospitalExists = await _context.Hospitals
            .AnyAsync(h => h.Id == request.HospitalId);

        if (!hospitalExists)
            throw new Exception("Selected hospital does not exist.");

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

        if (doctor == null)
            throw new Exception("Selected doctor does not exist.");

        if (doctor.HospitalId != request.HospitalId)
            throw new Exception("Selected doctor does not belong to the selected hospital.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = 3
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var patient = new Patient
        {
            UserId = user.Id,
            HospitalId = request.HospitalId,
            DoctorId = request.DoctorId,
            Age = request.Age,
            Gender = request.Gender,
            Height = request.Height,
            Weight = request.Weight,
            CancerType = request.CancerType,
            TreatmentType = request.TreatmentType
        };

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            throw new Exception("Invalid email or password.");

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
            throw new Exception("Invalid email or password.");

        var roleName = user.Role?.Name ?? "Unknown";
        var token = _jwtTokenService.GenerateToken(user, roleName);

        var response = new LoginResponse
        {
            Token = token,
            Role = roleName,
            UserId = user.Id
        };

        // Rolune gore DoctorId / PatientId doldur
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == user.Id);
        if (doctor != null)
            response.DoctorId = doctor.Id;

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (patient != null)
            response.PatientId = patient.Id;

        return response;
    }
}