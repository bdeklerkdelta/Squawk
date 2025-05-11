using Squawker.Domain.Entities;

namespace Squawker.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Squawk> Squawks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
