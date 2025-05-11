namespace Squawker.Application.Common.Interfaces;

/// <summary>
/// Interface for abstracting DateTime operations to improve testability
/// </summary>
public interface IDateTime
{
    /// <summary>
    /// Gets the current date and time
    /// </summary>
    DateTimeOffset Now { get; }
}