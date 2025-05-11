using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Squawker.Application.Common.Interfaces;

public interface ITelemetryService
{
    // Creates a diagnostic activity for tracking requests/operations
    Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);
    
    // Record events with attributes
    void RecordEvent(string name, IDictionary<string, object?>? attributes = null);
    
    // Add attributes to the current span
    void AddAttributes(IDictionary<string, object?> attributes);
    
    // Set status of the current span
    void SetStatus(ActivityStatusCode status, string? description = null);
    
    // Record exceptions
    void RecordException(Exception exception);
    
    void RecordCounterIncrement(string name, int value, IDictionary<string, object?>? tags = null);
     // Method for creating counters
    Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct;
    
    // Create histogram metrics
    Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct;
}