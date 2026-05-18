using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = null!;

    public ICollection<User> Users { get; set; } = new List<User>();
}