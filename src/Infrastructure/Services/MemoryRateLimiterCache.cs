using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Squawker.Application.Common.Interfaces;

namespace Squawker.Infrastructure.Services;

public class MemoryRateLimiterCache : IRateLimiterCache
{
    private readonly IMemoryCache _cache;
    private readonly ITelemetryService _telemetry;
    
    public MemoryRateLimiterCache(IMemoryCache cache, ITelemetryService telemetry)
    {
        _cache = cache;
        _telemetry = telemetry;
    }
    
    public Task<string?> GetAsync(string key)
    {
        using var activity = _telemetry.StartActivity("RateLimiterCache.Get", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["cache.key"] = key
        });
        
        try
        {
            bool found = _cache.TryGetValue(key, out string? value);
            
            // Record cache hit/miss metrics
            _telemetry.AddAttributes(new Dictionary<string, object?>
            {
                ["cache.hit"] = found
            });
            
            if (found)
            {
                _telemetry.RecordEvent("rate_limiter_cache.hit", new Dictionary<string, object?> 
                {
                    ["cache.key"] = key
                });
            }
            else
            {
                _telemetry.RecordEvent("rate_limiter_cache.miss", new Dictionary<string, object?> 
                {
                    ["cache.key"] = key
                });
            }
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return Task.FromResult(found ? value : null);
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
    
    public Task SetAsync(string key, string value, TimeSpan expiration)
    {
        using var activity = _telemetry.StartActivity("RateLimiterCache.Set", ActivityKind.Internal);
        _telemetry.AddAttributes(new Dictionary<string, object?>
        {
            ["cache.key"] = key,
            ["cache.expiration_ms"] = expiration.TotalMilliseconds
        });
        
        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration);
                
            _cache.Set(key, value, cacheEntryOptions);
            
            _telemetry.RecordEvent("rate_limiter_cache.set", new Dictionary<string, object?> 
            {
                ["cache.key"] = key,
                ["cache.expiration_ms"] = expiration.TotalMilliseconds
            });
            
            _telemetry.SetStatus(ActivityStatusCode.Ok);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _telemetry.RecordException(ex);
            _telemetry.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}