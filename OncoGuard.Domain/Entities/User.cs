using OncoGuard.Domain.Common;
using System.Numerics;

namespace OncoGuard.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Doctor? Doctor { get; set; }

    public Patient? Patient { get; set; }
}