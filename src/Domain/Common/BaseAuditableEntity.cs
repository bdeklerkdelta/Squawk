namespace Squawker.Domain.Common;

public abstract class BaseAuditableEntity<TKey> : BaseEntity<TKey> where TKey : IEquatable<TKey>
{
    public DateTimeOffset Created { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset LastModified { get; set; }

    public Guid LastModifiedBy { get; set; }
}
