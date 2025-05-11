using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Squawker.Application.Common.Interfaces;

namespace Squawker.Infrastructure.Services;

public class OpenTelemetryService : ITelemetryService
{
    private readonly Meter _meter;
    private readonly Dictionary<string, object> _counters = new();
    private const string ActivitySourceName = "Squawker.Application";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);
    
    public OpenTelemetryService(string meterName = "Squawker.Metrics")
    {
        _meter = new Meter(meterName);
    }
    
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(name, kind);
    }
    
    public void RecordEvent(string name, IDictionary<string, object?>? attributes = null)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, 
            new ActivityTagsCollection(attributes?.Select(kvp => 
                new KeyValuePair<string, object?>(kvp.Key, kvp.Value)) ?? Array.Empty<KeyValuePair<string, object?>>())));
    }
    
    public void AddAttributes(IDictionary<string, object?> attributes)
    {
        if (Activity.Current == null) return;
        
        foreach (var (key, value) in attributes)
        {
            Activity.Current.SetTag(key, value);
        }
    }
    
    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        if (Activity.Current == null) return;
        
        Activity.Current.SetStatus(status);
        if (description != null)
        {
            Activity.Current.SetTag("status.description", description);
        }
    }
    
    public void RecordException(Exception exception)
    {
        if (Activity.Current == null) return;
        
        // Add exception details as tags to the current activity
        Activity.Current.SetTag("exception.type", exception.GetType().FullName);
        Activity.Current.SetTag("exception.message", exception.Message);
        Activity.Current.SetTag("exception.stacktrace", exception.StackTrace);
        
        // Set activity status to error
        Activity.Current.SetStatus(ActivityStatusCode.Error);
        
        // Add exception as an event for better visibility
        var attributes = new Dictionary<string, object?>
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message
        };
        
        Activity.Current.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, 
            new ActivityTagsCollection(attributes)));
    }
    
    public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        string key = $"{typeof(T).Name}:{name}";
        if (!_counters.TryGetValue(key, out var existingCounter))
        {
            var counter = _meter.CreateCounter<T>(name, unit, description);
            _counters[key] = counter;
            return counter;
        }
        
        return (Counter<T>)existingCounter;
    }
    
    public void RecordCounterIncrement(string name, int value, IDictionary<string, object?>? tags = null)
    {
        // Get or create the counter dynamically
        var counter = GetOrCreateCounter<int>(name);
        
        // Create the tag list if needed
        if (tags != null && tags.Count > 0)
        {
            var tagList = new TagList();
            foreach (var tag in tags.Where(t => t.Key != null && t.Value != null))
            {
                tagList.Add(tag.Key, tag.Value);
            }
            counter.Add(value, tagList);
        }
        else
        {
            counter.Add(value);
        }
    }
    
    private Counter<T> GetOrCreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        string key = $"{typeof(T).Name}:{name}";
        if (!_counters.TryGetValue(key, out var existingCounter))
        {
            var counter = _meter.CreateCounter<T>(name, unit, description);
            _counters[key] = counter;
            return counter;
        }
        
        return (Counter<T>)existingCounter;
    }
    
    public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        return _meter.CreateHistogram<T>(name, unit, description);
    }
}