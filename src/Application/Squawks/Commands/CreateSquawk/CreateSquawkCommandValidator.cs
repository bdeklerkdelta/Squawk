using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Squawker.Application.Common.Interfaces;
using System.Text.RegularExpressions;
using System.Diagnostics.Metrics;

namespace Squawker.Application.Squawks.Commands.CreateSquawk;

public class CreateSquawkCommandValidator : AbstractValidator<CreateSquawkCommand>
{
    private readonly ISquawkRepository _repository;
    private readonly IDateTime _dateTime;
    private readonly ITelemetryService _telemetry;
    
    public CreateSquawkCommandValidator(
        ISquawkRepository repository,
        IDateTime dateTime,
        ITelemetryService telemetry)
    {
        _repository = repository;
        _dateTime = dateTime;
        _telemetry = telemetry;
        
        // Initialize metrics
        _telemetry.CreateCounter<int>("squawk.banned_term.count", 
            description: "Number of squawk attempts containing banned terms");
        _telemetry.CreateCounter<int>("squawk.duplicate.count", 
            description: "Number of duplicate squawk attempts");

        // Content requirements
        RuleFor(v => v.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(400).WithMessage("Content must not exceed 400 characters.");

        // Check for banned terms
        RuleFor(v => v.Content)
            .Must(NotContainBannedTerms).WithMessage("Content cannot contain references to 'Tweet' or 'Twitter'.");

        // Validate UserId is not empty
        RuleFor(v => v.UserId)
            .NotEqual(Guid.Empty).WithMessage("User ID is required.");

        // Prevent duplicate squawks
        RuleFor(v => v)
            .MustAsync(NotBeDuplicateSquawk).WithMessage("You've already posted this exact content. Duplicate squawks are not allowed.");
    }

    private bool NotContainBannedTerms(string content)
    {
        var hasBannedTerms = content.Contains("Tweet", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("Twitter", StringComparison.OrdinalIgnoreCase);
                        
        if (hasBannedTerms)
        {
            // Use abstracted method instead of direct counter
            _telemetry.RecordCounterIncrement(
                "squawk.banned_term.count",
                1,
                new Dictionary<string, object?> { ["validation"] = "banned_term" }
            );
        }
        
        return !hasBannedTerms;
    }

    private async Task<bool> NotBeDuplicateSquawk(CreateSquawkCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty)
            return false; // UserId is required

        // Check if this user has posted the exact same content before using repository
        bool isDuplicate = await _repository.HasUserPostedDuplicateContentAsync(command.UserId, command.Content, cancellationToken);
        
        if (isDuplicate)
        {
            // Use the abstracted telemetry method instead of direct counter access
            _telemetry.RecordCounterIncrement(
                "squawk.duplicate.count",
                1,
                new Dictionary<string, object?> { ["user_id"] = command.UserId.ToString() }
            );
            
            // Continue to record the event for additional context in Azure Monitor
            _telemetry.RecordEvent("duplicate_squawk_attempt", new Dictionary<string, object?>
            {
                ["user_id"] = command.UserId.ToString(),
                ["content_length"] = command.Content?.Length ?? 0
            });
        }
        
        return !isDuplicate;
    }
}