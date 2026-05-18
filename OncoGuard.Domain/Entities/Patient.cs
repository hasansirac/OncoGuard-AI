using OncoGuard.Domain.Enums;
using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class Patient : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int Age { get; set; }


    public double Height { get; set; }

    public double Weight { get; set; }

    public Gender Gender { get; set; }

    public CancerType CancerType { get; set; }

    public TreatmentType TreatmentType { get; set; }
}