using OncoGuard.Domain.Common;
using System.Numerics;

namespace OncoGuard.Domain.Entities;

public class Hospital : BaseEntity
{
    public string Name { get; set; } = null!;

    public string City { get; set; } = null!;

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}