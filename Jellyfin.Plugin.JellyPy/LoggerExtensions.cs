using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy;

/// <summary>
/// Extension methods for ILogger that respect the EnableVerboseLogging configuration setting.
/// When verbose logging is enabled, debug-level messages are upgraded to Information level
/// so they appear in Jellyfin's default log output.
/// </summary>
[SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "Verbose prefix is intentionally added at runtime based on configuration")]
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a message at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message.</param>
    public static void LogVerbose(this ILogger logger, string message)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] {Message}", message);
        }
        else
        {
            logger.LogDebug(message);
        }
    }

    /// <summary>
    /// Logs a message with one parameter at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <typeparam name="T0">The type of the first parameter.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="arg0">The first argument.</param>
    public static void LogVerbose<T0>(this ILogger logger, string message, T0 arg0)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] " + message, arg0);
        }
        else
        {
            logger.LogDebug(message, arg0);
        }
    }

    /// <summary>
    /// Logs a message with two parameters at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <typeparam name="T0">The type of the first parameter.</typeparam>
    /// <typeparam name="T1">The type of the second parameter.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    public static void LogVerbose<T0, T1>(this ILogger logger, string message, T0 arg0, T1 arg1)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] " + message, arg0, arg1);
        }
        else
        {
            logger.LogDebug(message, arg0, arg1);
        }
    }

    /// <summary>
    /// Logs a message with three parameters at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <typeparam name="T0">The type of the first parameter.</typeparam>
    /// <typeparam name="T1">The type of the second parameter.</typeparam>
    /// <typeparam name="T2">The type of the third parameter.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    /// <param name="arg2">The third argument.</param>
    public static void LogVerbose<T0, T1, T2>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] " + message, arg0, arg1, arg2);
        }
        else
        {
            logger.LogDebug(message, arg0, arg1, arg2);
        }
    }

    /// <summary>
    /// Logs a message with four parameters at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <typeparam name="T0">The type of the first parameter.</typeparam>
    /// <typeparam name="T1">The type of the second parameter.</typeparam>
    /// <typeparam name="T2">The type of the third parameter.</typeparam>
    /// <typeparam name="T3">The type of the fourth parameter.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    /// <param name="arg2">The third argument.</param>
    /// <param name="arg3">The fourth argument.</param>
    public static void LogVerbose<T0, T1, T2, T3>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] " + message, arg0, arg1, arg2, arg3);
        }
        else
        {
            logger.LogDebug(message, arg0, arg1, arg2, arg3);
        }
    }

    /// <summary>
    /// Logs a message with five parameters at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <typeparam name="T0">The type of the first parameter.</typeparam>
    /// <typeparam name="T1">The type of the second parameter.</typeparam>
    /// <typeparam name="T2">The type of the third parameter.</typeparam>
    /// <typeparam name="T3">The type of the fourth parameter.</typeparam>
    /// <typeparam name="T4">The type of the fifth parameter.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The log message template.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    /// <param name="arg2">The third argument.</param>
    /// <param name="arg3">The fourth argument.</param>
    /// <param name="arg4">The fifth argument.</param>
    public static void LogVerbose<T0, T1, T2, T3, T4>(this ILogger logger, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation("[Verbose] " + message, arg0, arg1, arg2, arg3, arg4);
        }
        else
        {
            logger.LogDebug(message, arg0, arg1, arg2, arg3, arg4);
        }
    }

    /// <summary>
    /// Logs an exception with a message at Debug level when verbose logging is disabled,
    /// or at Information level when verbose logging is enabled.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    public static void LogVerbose(this ILogger logger, Exception exception, string message)
    {
        if (IsVerboseLoggingEnabled())
        {
            logger.LogInformation(exception, "[Verbose] {Message}", message);
        }
        else
        {
            logger.LogDebug(exception, message);
        }
    }

    private static bool IsVerboseLoggingEnabled()
    {
        try
        {
            return Plugin.Instance?.Configuration?.GlobalSettings?.EnableVerboseLogging ?? false;
        }
        catch
        {
            // If we can't access configuration, default to not verbose
            return false;
        }
    }
}
