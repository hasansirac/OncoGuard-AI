using OncoGuard.Domain.Common;

namespace OncoGuard.Domain.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string ActionType { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Description { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
}