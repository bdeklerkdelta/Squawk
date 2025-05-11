using Microsoft.AspNetCore.Http.HttpResults;
using Squawker.Application.Squawks.Commands.CreateSquawk;
using Squawker.Application.Squawks.Queries.GetSquawkById;
using Squawker.Application.Squawks.Queries.GetSquawks;
using Squawker.Application.Squawks;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Squawker.Application.Common.Interfaces;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Squawker.Web.Endpoints;

public class Squawks : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .WithTags("Squawks")
            .WithOpenApi()
            .MapGet(GetSquawks)
            .MapGet(GetSquawkById, "{id}")
            .MapPost(CreateSquawk);
    }

    /// <summary>
    /// Returns a list of all squawks in the system
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="telemetry">Telemetry service for monitoring</param>
    /// <returns>List of squawks</returns>
    /// <response code="200">Returns the list of squawks</response>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SquawkDto>))]
    public async Task<Ok<List<SquawkDto>>> GetSquawks(
        ISender sender,
        [FromServices] ITelemetryService telemetry)
    {
        using var activity = telemetry.StartActivity("Squawks.GetSquawks", ActivityKind.Server);
        
        try
        {
            var result = await sender.Send(new GetSquawksQuery());
            
            // Add result metrics to telemetry
            telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawks.count"] = result.Count
            });
            
            telemetry.SetStatus(ActivityStatusCode.Ok);
            return TypedResults.Ok(result);
        }
        catch (Exception ex)
        {
            telemetry.RecordException(ex);
            telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific squawk by its unique identifier
    /// </summary>
    /// <param name="sender">MediatR sender</param>
    /// <param name="id">The unique identifier of the squawk</param>
    /// <param name="telemetry">Telemetry service for monitoring</param>
    /// <returns>The requested squawk or a 404 response</returns>
    /// <response code="200">Returns the requested squawk</response>
    /// <response code="404">If the squawk is not found</response>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SquawkDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Results<Ok<SquawkDto>, NotFound>> GetSquawkById(
        ISender sender,
        [FromRoute] Guid id,
        [FromServices] ITelemetryService telemetry)
    {
        using var activity = telemetry.StartActivity("Squawks.GetSquawkById", ActivityKind.Server);
        telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["squawk.id"] = id.ToString()
        });
        
        try
        {
            var result = await sender.Send(new GetSquawkByIdQuery(id));
            telemetry.SetStatus(ActivityStatusCode.Ok);
            return TypedResults.Ok(result);
        }
        catch (NotFoundException)
        {
            telemetry.RecordEvent("squawk.not_found", new Dictionary<string, object?>
            {
                ["squawk.id"] = id.ToString()
            });
            telemetry.SetStatus(ActivityStatusCode.Error, "Squawk not found");
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            telemetry.RecordException(ex);
            telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates a new squawk with rate limiting (one squawk per 20 seconds per user)
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands</param>
    /// <param name="command">The squawk creation details including content and user ID</param>
    /// <param name="rateLimiterCache">Service for rate limiting</param>
    /// <param name="telemetry">Telemetry service for monitoring</param>
    /// <returns>A newly created squawk with its unique identifier</returns>
    /// <response code="201">Returns the newly created squawk ID</response>
    /// <response code="400">If the squawk data is invalid (e.g., empty content, banned terms)</response>
    /// <response code="429">If the rate limit is exceeded (one squawk per 20 seconds)</response>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<Results<Created<Guid>, ProblemHttpResult>> CreateSquawk(
        ISender sender, 
        [FromBody] CreateSquawkCommand command,
        [FromServices] IRateLimiterCache rateLimiterCache,
        [FromServices] ITelemetryService telemetry)
    {
        // Start a new span for this operation
        using var activity = telemetry.StartActivity(
            "Squawks.CreateSquawk",
            ActivityKind.Server);
        
        try 
        {
            // Add attributes for better context
            telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawk.user_id"] = command.UserId.ToString(),
                ["squawk.content_length"] = command.Content?.Length ?? 0
            });

            var userId = command.UserId.ToString();
            var key = $"squawk-rate-limit:{userId}";
            
            // Check if user has posted recently
            if (await rateLimiterCache.GetAsync(key) != null)
            {
                // Track rate limit exceeded event
                telemetry.RecordEvent("squawk.rate_limit.exceeded", new Dictionary<string, object?>
                {
                    ["user_id"] = userId
                });
                
                telemetry.SetStatus(ActivityStatusCode.Error, "Rate limit exceeded");
                
                return TypedResults.Problem(
                    detail: "You can only post one squawk every 20 seconds. Please try again shortly.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Rate limit exceeded",
                    type: "https://tools.ietf.org/html/rfc6585#section-4");
            }
            
            // Set rate limit with 20 second expiration
            await rateLimiterCache.SetAsync(key, "1", TimeSpan.FromSeconds(20));
            
            // Record successful rate limit check
            telemetry.RecordEvent("squawk.rate_limit.passed");
            
            // Process the command
            var id = await sender.Send(command);
            
            // Record successful squawk creation
            telemetry.RecordEvent("squawk.created", new Dictionary<string, object?>
            {
                ["squawk_id"] = id.ToString(),
                ["user_id"] = userId
            });
            
            telemetry.SetStatus(ActivityStatusCode.Ok);
            
            return TypedResults.Created($"/{nameof(Squawks)}/{id}", id);
        }
        catch (Exception ex)
        {
            // Record exception in telemetry
            telemetry.RecordException(ex);
            telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}