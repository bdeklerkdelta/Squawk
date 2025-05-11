using System.ComponentModel.DataAnnotations.Schema;

namespace Squawker.Domain.Common;

/// <summary>
/// Base entity with generic key type support
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public abstract class BaseEntity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
}
