namespace Squawker.Domain.Entities;

public class Squawk : BaseAuditableEntity<Guid>
{
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
