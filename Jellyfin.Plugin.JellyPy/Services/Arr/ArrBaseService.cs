using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Base class for Sonarr and Radarr API services providing common functionality.
/// </summary>
/// <typeparam name="TLogger">The logger type for the derived service.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ArrBaseService{TLogger}"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="httpClientFactory">The HTTP client factory.</param>
public abstract class ArrBaseService<TLogger>(ILogger<TLogger> logger, IHttpClientFactory httpClientFactory)
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger<TLogger> Logger { get; } = logger;

    /// <summary>
    /// Gets the HTTP client factory.
    /// </summary>
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;

    /// <summary>
    /// Gets the service name for logging (e.g., "Sonarr", "Radarr").
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Executes an API call with standardized error handling.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging.</param>
    /// <param name="contextInfo">Additional context information for logging.</param>
    /// <returns>The result of the operation, or default(T) on error.</returns>
    protected async Task<T?> ExecuteApiCallAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        string? contextInfo = null)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException ex)
        {
            LogError(ex, $"HTTP request failed when {operationName}", contextInfo);
            return default;
        }
        catch (JsonException ex)
        {
            LogError(ex, $"Failed to deserialize {ServiceName} API response for {operationName}", contextInfo);
            return default;
        }
        catch (TaskCanceledException ex)
        {
            LogError(ex, $"Request timed out when {operationName}", contextInfo);
            return default;
        }
        catch (UriFormatException ex)
        {
            LogError(ex, $"Invalid {ServiceName} URL format when {operationName}", contextInfo);
            return default;
        }
        catch (ArgumentException ex)
        {
            LogError(ex, $"Invalid argument when {operationName}", contextInfo);
            return default;
        }
        catch (InvalidOperationException ex)
        {
            LogError(ex, $"Invalid operation when {operationName}", contextInfo);
            return default;
        }
        catch (Exception ex)
        {
            LogError(ex, $"Unexpected error {operationName}", contextInfo);
            throw;
        }
    }

    /// <summary>
    /// Executes an API call that returns a boolean result with standardized error handling.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging.</param>
    /// <param name="contextInfo">Additional context information for logging.</param>
    /// <returns>The result of the operation, or false on error.</returns>
    protected async Task<bool> ExecuteApiCallAsync(
        Func<Task<bool>> operation,
        string operationName,
        string? contextInfo = null)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException ex)
        {
            LogError(ex, $"HTTP request failed when {operationName}", contextInfo);
            return false;
        }
        catch (JsonException ex)
        {
            LogError(ex, $"Failed to deserialize {ServiceName} API response for {operationName}", contextInfo);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            LogError(ex, $"Request timed out when {operationName}", contextInfo);
            return false;
        }
        catch (UriFormatException ex)
        {
            LogError(ex, $"Invalid {ServiceName} URL format when {operationName}", contextInfo);
            return false;
        }
        catch (ArgumentException ex)
        {
            LogError(ex, $"Invalid argument when {operationName}", contextInfo);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            LogError(ex, $"Invalid operation when {operationName}", contextInfo);
            return false;
        }
        catch (Exception ex)
        {
            LogError(ex, $"Unexpected error {operationName}", contextInfo);
            throw;
        }
    }

    /// <summary>
    /// Creates an HTTP client configured for the service.
    /// </summary>
    /// <param name="baseUrl">The base URL for the service.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <returns>A configured HTTP client.</returns>
    protected HttpClient CreateHttpClient(string baseUrl, string apiKey)
    {
        var client = HttpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    /// <summary>
    /// Logs an error with optional context information.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="contextInfo">Optional context information.</param>
    private void LogError(Exception exception, string message, string? contextInfo)
    {
        if (string.IsNullOrEmpty(contextInfo))
        {
            Logger.LogError(exception, "{Message}", message);
        }
        else
        {
            Logger.LogError(exception, "{Message}: {Context}", message, contextInfo);
        }
    }
}
