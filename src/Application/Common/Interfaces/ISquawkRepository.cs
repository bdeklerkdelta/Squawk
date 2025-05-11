using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Squawker.Domain.Entities;

namespace Squawker.Application.Common.Interfaces;

public interface ISquawkRepository
{
    Task<Guid> CreateSquawkAsync(Squawk squawk, CancellationToken cancellationToken);
    Task<IEnumerable<Squawk>> GetAllSquawksAsync(CancellationToken cancellationToken);
    Task<Squawk?> GetSquawkByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteSquawkAsync(Guid id, CancellationToken cancellationToken);
    Task<Squawk?> GetMostRecentSquawkByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasUserPostedDuplicateContentAsync(Guid userId, string content, CancellationToken cancellationToken);
}