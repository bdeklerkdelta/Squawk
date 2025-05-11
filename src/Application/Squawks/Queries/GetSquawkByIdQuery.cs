using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Squawker.Application.Common.Exceptions;
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks;
using Squawker.Domain.Entities;

namespace Squawker.Application.Squawks.Queries.GetSquawkById;

public record GetSquawkByIdQuery(Guid Id) : IRequest<SquawkDto>;

public class GetSquawkByIdQueryHandler : IRequestHandler<GetSquawkByIdQuery, SquawkDto>
{
    private readonly ISquawkRepository _repository;
    private readonly IMapper _mapper;
    private readonly ITelemetryService _telemetry;

    public GetSquawkByIdQueryHandler(
        ISquawkRepository repository, 
        IMapper mapper, 
        ITelemetryService telemetry)
    {
        _repository = repository;
        _mapper = mapper;
        _telemetry = telemetry;
    }

    public async Task<SquawkDto> Handle(GetSquawkByIdQuery request, CancellationToken cancellationToken)
    {
        // Start activity for this operation with appropriate kind
        using var activity = _telemetry.StartActivity("GetSquawkById.Handle", ActivityKind.Internal);
        
        try
        {
            // Record request identifier for correlation
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawk.id"] = request.Id.ToString()
            });
            
            var stopwatch = Stopwatch.StartNew();
            var squawk = await _repository.GetSquawkByIdAsync(request.Id, cancellationToken);
            stopwatch.Stop();
            
            // Track repository query performance
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["repository.duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["squawk.found"] = squawk != null
            });
            
            if (squawk == null)
            {
                _telemetry.RecordEvent("squawk.not_found", new Dictionary<string, object?>
                {
                    ["squawk.id"] = request.Id.ToString()
                });
                
                _telemetry.SetStatus(ActivityStatusCode.Error, "Squawk not found");
                throw new NotFoundException(nameof(Squawk), request.Id.ToString());
            }
            
            // Record squawk metadata (not content) for analysis
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawk.created_at"] = squawk.Created.ToString("o"),
                ["squawk.creator_id"] = squawk.CreatedBy.ToString(),
                ["squawk.content_length"] = squawk.Content.Length
            });
            
            var result = _mapper.Map<SquawkDto>(squawk);
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex) when (!(ex is NotFoundException)) // Already handled NotFoundException above
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}