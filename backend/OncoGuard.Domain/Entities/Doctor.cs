using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class Doctor : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}