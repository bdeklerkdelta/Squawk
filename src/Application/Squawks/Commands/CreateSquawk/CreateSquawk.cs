using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Squawker.Application.Common.Interfaces;
using Squawker.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Squawker.Application.Squawks.Commands.CreateSquawk;

public record CreateSquawkCommand : IRequest<Guid>
{
    [Required]
    [MaxLength(400)]
    [Description("The text content of the squawk")]
    [Display(Description = "Hello world! This is my first squawk!")]
    public string Content { get; init; } = string.Empty;
    
    [Required]
    [Description("The ID of the user creating this squawk")]
    public Guid UserId { get; init; } = Guid.Empty;
}

public class CreateSquawkCommandHandler : IRequestHandler<CreateSquawkCommand, Guid>
{
    private readonly ISquawkRepository _repository;
    private readonly ITelemetryService _telemetry;

    public CreateSquawkCommandHandler(
        ISquawkRepository repository,
        ITelemetryService telemetry)
    {
        _repository = repository;
        _telemetry = telemetry;
    }

    public async Task<Guid> Handle(CreateSquawkCommand request, CancellationToken cancellationToken)
    {
        // Create activity for this operation
        using var activity = _telemetry.StartActivity("CreateSquawk.Handle", System.Diagnostics.ActivityKind.Internal);
        
        try
        {
            // Add contextual information
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawk.user_id"] = request.UserId.ToString(),
                ["squawk.content_length"] = request.Content.Length
            });
            
            var entity = new Squawk
            {
                Id = Guid.NewGuid(),
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.UserId
            };

            // Track ID for correlation
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["squawk.id"] = entity.Id.ToString()
            });

            var result = await _repository.CreateSquawkAsync(entity, cancellationToken);
            
            // Record successful creation
            _telemetry.RecordEvent("squawk.created", new Dictionary<string, object?>
            {
                ["squawk.id"] = result.ToString(),
                ["squawk.user_id"] = request.UserId.ToString()
            });
            
            _telemetry.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}