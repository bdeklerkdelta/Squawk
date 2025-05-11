using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Squawker.Application.Common.Interfaces;
using Squawker.Application.Squawks;

namespace Squawker.Application.Squawks.Queries.GetSquawks;

public record GetSquawksQuery : IRequest<List<SquawkDto>>;

public class GetSquawksQueryHandler : IRequestHandler<GetSquawksQuery, List<SquawkDto>>
{
    private readonly ISquawkRepository _repository;
    private readonly IMapper _mapper;
    private readonly ITelemetryService _telemetry;

    public GetSquawksQueryHandler(
        ISquawkRepository repository, 
        IMapper mapper,
        ITelemetryService telemetry)
    {
        _repository = repository;
        _mapper = mapper;
        _telemetry = telemetry;
    }

    public async Task<List<SquawkDto>> Handle(GetSquawksQuery request, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.StartActivity("GetSquawksQuery.Handle", ActivityKind.Internal);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var squawks = await _repository.GetAllSquawksAsync(cancellationToken);
            stopwatch.Stop();
            
            // Record database query performance
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["query.duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["query.result_count"] = squawks.Count()
            });
            
            // Reset stopwatch to measure mapping operation separately
            stopwatch.Restart();
            
            // Convert to DTOs using AutoMapper and order by creation date descending
            var result = squawks
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => _mapper.Map<SquawkDto>(s))
                .ToList();
                
            stopwatch.Stop();
            
            // Record mapping operation performance
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["mapping.duration_ms"] = stopwatch.ElapsedMilliseconds,
                ["result.count"] = result.Count
            });
            
            // Record timespan of newest and oldest squawk to understand data range
            if (result.Any())
            {
                var newestDate = result.First().CreatedAt;
                var oldestDate = result.Last().CreatedAt;
                
                _telemetry.AddAttributes(new Dictionary<string, object?>
                {
                    ["result.newest_date"] = newestDate.ToString("o"),
                    ["result.oldest_date"] = oldestDate.ToString("o"),
                    ["result.date_span_hours"] = (newestDate - oldestDate).TotalHours
                });
            }
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}