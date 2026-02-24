using System;

namespace ProceduralGraph;

/// <summary>
/// Defines a contract for logging messages and exceptions to a configurable output or logging system.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs the specified exception to the configured output or logging system.
    /// </summary>
    /// <param name="exception">The exception to log. Cannot be <see langword="null"/>.</param>
    /// <param name="context">An optional context object related to the warning.</param>
    void LogException(Exception exception, object? context = default);

    /// <summary>
    /// Logs an informational message to the configured output or logging system.
    /// </summary>
    /// <param name="message">The message to log. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="context">An optional context object related to the warning.</param>
    void LogInfo(string message, object? context = default);

    /// <summary>
    /// Logs a warning message to the configured output or logging system.
    /// </summary>
    /// <param name="message">The warning message to log. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="context">An optional context object related to the warning.</param>
    void LogWarning(string message, object? context = default);

    /// <summary>
    /// Logs an error message to the configured output or logging system.
    /// </summary>
    /// <param name="message">The error message to log. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="context">An optional context object related to the error.</param>
    void LogError(string message, object? context = default);
}