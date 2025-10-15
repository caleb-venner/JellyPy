using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellypy.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Services.Arr;

/// <summary>
/// Service for interacting with Radarr API.
/// </summary>
public class RadarrService : IRadarrService
{
    private readonly ILogger<RadarrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public RadarrService(ILogger<RadarrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<int?> GetMovieIdByNameAsync(string movieName)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.RadarrApiKey))
            {
                _logger.LogWarning("Radarr API key not configured");
                return null;
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/movie/lookup?term={Uri.EscapeDataString(movieName)}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Radarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var movies = await response.Content.ReadFromJsonAsync<List<RadarrMovie>>();
            var firstMovie = movies?.FirstOrDefault();

            if (firstMovie != null)
            {
                _logger.LogInformation("Found Radarr movie: {Title} (ID: {Id})", firstMovie.Title, firstMovie.Id);
                return firstMovie.Id;
            }

            _logger.LogWarning("No movie found in Radarr for: {MovieName}", movieName);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting Radarr movie ID for: {MovieName}", movieName);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Radarr API response for movie: {MovieName}", movieName);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting Radarr movie ID for: {MovieName}", movieName);
            return null;
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid Radarr URL format when getting movie ID for: {MovieName}", movieName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting Radarr movie ID for: {MovieName}", movieName);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetMovieMonitoredAsync(int movieId, bool monitored)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.RadarrApiKey))
            {
                return false;
            }

            var client = CreateHttpClient(config);
            var request = new UpdateMoviesRequest
            {
                MovieIds = new[] { movieId },
                Monitored = monitored
            };

            var response = await client.PutAsJsonAsync("/api/v3/movie/editor", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Set movie {MovieId} monitored status to {Monitored}", movieId, monitored);
                return true;
            }

            _logger.LogError("Failed to set movie monitored status. Status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when setting movie monitored status: {MovieId}", movieId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize Radarr API request for movie: {MovieId}", movieId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when setting movie monitored status: {MovieId}", movieId);
            return false;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument when setting movie monitored status: {MovieId}", movieId);
            return false;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when setting movie monitored status: {MovieId}", movieId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting movie monitored status: {MovieId}", movieId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<RadarrMovieDetails?> GetMovieDetailsAsync(int movieId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.RadarrApiKey))
            {
                _logger.LogWarning("Radarr API key not configured");
                return null;
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/movie/{movieId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Radarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var movieDetails = await response.Content.ReadFromJsonAsync<RadarrMovieDetails>();

            if (movieDetails != null)
            {
                _logger.LogInformation(
                    "Retrieved movie details: {Title} (ID: {Id}), HasFile: {HasFile}, QualityCutoffMet: {CutoffMet}",
                    movieDetails.Title,
                    movieDetails.Id,
                    movieDetails.HasFile,
                    movieDetails.HasReachedQualityCutoff);
                return movieDetails;
            }

            _logger.LogWarning("No movie details found in Radarr for ID: {MovieId}", movieId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting Radarr movie details for ID: {MovieId}", movieId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Radarr API response for movie ID: {MovieId}", movieId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting Radarr movie details for ID: {MovieId}", movieId);
            return null;
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid Radarr URL format when getting movie details for ID: {MovieId}", movieId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting Radarr movie details for ID: {MovieId}", movieId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private HttpClient CreateHttpClient(PluginConfiguration config)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(config.RadarrUrl.TrimEnd('/'));
        client.DefaultRequestHeaders.Add("X-Api-Key", config.RadarrApiKey);
        // Content-Type is set automatically by HttpClient for JSON content
        return client;
    }
}
