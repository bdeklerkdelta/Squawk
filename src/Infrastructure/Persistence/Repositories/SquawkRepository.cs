using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Squawker.Application.Common.Interfaces;
using Squawker.Domain.Entities;
using Squawker.Infrastructure.Data;
using System.Diagnostics;
using System.Linq;

namespace Squawker.Infrastructure.Persistence.Repositories;

public class SquawkRepository : ISquawkRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITelemetryService _telemetry;

    public SquawkRepository(
        ApplicationDbContext context,
        ITelemetryService telemetry)
    {
        _context = context;
        _telemetry = telemetry;
    }

    public async Task<Guid> CreateSquawkAsync(Squawk squawk, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.CreateSquawk", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "insert",
            ["db.table"] = "Squawks",
            ["squawk.id"] = squawk.Id.ToString()
        });
        
        try
        {
            _context.Squawks.Add(squawk);
            await _context.SaveChangesAsync(cancellationToken);
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return squawk.Id;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<Squawk>> GetAllSquawksAsync(CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.GetAllSquawks", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "select",
            ["db.table"] = "Squawks"
        });
        
        try
        {
            var squawks = await _context.Squawks.AsNoTracking().ToListAsync(cancellationToken);
            
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["result.count"] = squawks.Count
            });
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return squawks;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<Squawk?> GetSquawkByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.GetSquawkById", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "select",
            ["db.table"] = "Squawks",
            ["squawk.id"] = id.ToString()
        });
        
        try
        {
            var squawk = await _context.Squawks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["result.found"] = squawk != null
            });
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return squawk;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteSquawkAsync(Guid id, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.DeleteSquawk", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "delete",
            ["db.table"] = "Squawks",
            ["squawk.id"] = id.ToString()
        });
        
        try
        {
            var squawk = await _context.Squawks.FindAsync(new object[] { id }, cancellationToken);
            
            if (squawk == null)
            {
                _telemetry.RecordEvent("squawk.delete.not_found", new Dictionary<string, object?>
                {
                    ["squawk.id"] = id.ToString()
                });
                _telemetry.SetStatus(ActivityStatusCode.Ok, "Squawk not found");
                return false;
            }

            _context.Squawks.Remove(squawk);
            await _context.SaveChangesAsync(cancellationToken);
            
            _telemetry.RecordEvent("squawk.deleted", new Dictionary<string, object?>
            {
                ["squawk.id"] = id.ToString()
            });
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return true;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<Squawk?> GetMostRecentSquawkByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.GetMostRecentSquawkByUser", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "select",
            ["db.table"] = "Squawks",
            ["user.id"] = userId.ToString()
        });
        
        try
        {
            var squawk = await _context.Squawks
                .Where(s => s.CreatedBy == userId)
                .OrderByDescending(s => s.Created)
                .FirstOrDefaultAsync(cancellationToken);
                
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["result.found"] = squawk != null
            });
            
            if (squawk != null)
            {
                _telemetry.AddAttributes(new Dictionary<string, object?>
                {
                    ["squawk.created_at"] = squawk.Created.ToString("o")
                });
            }
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return squawk;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> HasUserPostedDuplicateContentAsync(Guid userId, string content, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("SquawkRepository.HasUserPostedDuplicateContent", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["db.operation"] = "select",
            ["db.table"] = "Squawks",
            ["user.id"] = userId.ToString(),
            ["content.length"] = content.Length
        });
        
        try
        {
            bool isDuplicate = await _context.Squawks
                .AnyAsync(s => 
                    s.CreatedBy == userId && 
                    s.Content == content,
                    cancellationToken);
                    
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["result.is_duplicate"] = isDuplicate
            });
            
            if (isDuplicate)
            {
                _telemetry.RecordEvent("squawk.duplicate_detected", new Dictionary<string, object?>
                {
                    ["user.id"] = userId.ToString()
                });
            }
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return isDuplicate;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}