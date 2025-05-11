using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Squawker.Application.Common.Interfaces;

namespace Squawker.Application.Common.Behaviours;

public class TelemetryBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ITelemetryService _telemetry;

    public TelemetryBehaviour(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // Start activity/span for the request
        using var activity = _telemetry.StartActivity(
            $"MediatR.{requestName}",
            ActivityKind.Internal);
        
        try
        {
            // Record request type
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["request.type"] = request.GetType().Name
            });
            
            // Process the request
            var response = await next();
            
            // Mark as successful
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            // Record exception details
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            throw;
        }
    }
}