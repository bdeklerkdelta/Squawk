using System;
using System.Threading.Tasks;

namespace Squawker.Application.Common.Interfaces;

/// <summary>
/// Interface for abstracting rate limiting cache operations
/// </summary>
public interface IRateLimiterCache
{
    /// <summary>
    /// Gets a value from the cache using the specified key
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <returns>The cached value or null if not found</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Sets a value in the cache with the specified key and expiration
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">The expiration time</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SetAsync(string key, string value, TimeSpan expiration);
}