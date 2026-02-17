using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Services.Arr.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Service for interacting with Radarr API.
/// </summary>
public class RadarrService : ArrBaseService<RadarrService>, IRadarrService
{
    private readonly IPluginConfigurationProvider _configProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configProvider">The configuration provider.</param>
    public RadarrService(ILogger<RadarrService> logger, IHttpClientFactory httpClientFactory, IPluginConfigurationProvider configProvider)
        : base(logger, httpClientFactory)
    {
        _configProvider = configProvider;
    }

    /// <inheritdoc />
    protected override string ServiceName => "Radarr";

    /// <inheritdoc />
    public Task<int?> GetMovieIdByNameAsync(string movieName)
    {
        return ExecuteApiCallAsync<int?>(
            async () =>
        {
            var config = _configProvider.GetConfiguration();

            if (string.IsNullOrEmpty(config.RadarrApiKey))
            {
                Logger.LogWarning("Radarr API key not configured");
                return null;
            }

            var client = CreateHttpClient(config.RadarrUrl, config.RadarrApiKey);
            var response = await client.GetAsync($"/api/v3/movie/lookup?term={Uri.EscapeDataString(movieName)}");

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Radarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var movies = await response.Content.ReadFromJsonAsync<List<RadarrMovie>>();
            var firstMovie = movies?.FirstOrDefault();

            if (firstMovie != null)
            {
                Logger.LogInformation("Found Radarr movie: {Title} (ID: {Id})", firstMovie.Title, firstMovie.Id);
                return firstMovie.Id;
            }

            Logger.LogWarning("No movie found in Radarr for: {MovieName}", movieName);
            return null;
        },
            $"getting movie ID for: {movieName}")!;
    }

    /// <inheritdoc />
    public Task<bool> SetMovieMonitoredAsync(int movieId, bool monitored)
    {
        return ExecuteApiCallAsync(
            async () =>
        {
            var config = _configProvider.GetConfiguration();

            if (string.IsNullOrEmpty(config.RadarrApiKey))
            {
                return false;
            }

            var client = CreateHttpClient(config.RadarrUrl, config.RadarrApiKey);
            var request = new UpdateMoviesRequest
            {
                MovieIds = new[] { movieId },
                Monitored = monitored
            };

            var response = await client.PutAsJsonAsync("/api/v3/movie/editor", request);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Set movie {MovieId} monitored status to {Monitored}", movieId, monitored);
                return true;
            }

            Logger.LogError("Failed to set movie monitored status. Status code: {StatusCode}", response.StatusCode);
            return false;
        },
            $"setting movie monitored status: {movieId.ToString(CultureInfo.InvariantCulture)}");
    }

    /// <inheritdoc />
    public Task<RadarrMovieDetails?> GetMovieDetailsAsync(int movieId)
    {
        return ExecuteApiCallAsync<RadarrMovieDetails?>(
            async () =>
        {
            var config = _configProvider.GetConfiguration();

            if (string.IsNullOrEmpty(config.RadarrApiKey))
            {
                Logger.LogWarning("Radarr API key not configured");
                return null;
            }

            var client = CreateHttpClient(config.RadarrUrl, config.RadarrApiKey);
            var response = await client.GetAsync($"/api/v3/movie/{movieId.ToString(CultureInfo.InvariantCulture)}");

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Radarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var movieDetails = await response.Content.ReadFromJsonAsync<RadarrMovieDetails>();

            if (movieDetails != null)
            {
                Logger.LogInformation(
                    "Retrieved movie details: {Title} (ID: {Id}), HasFile: {HasFile}, QualityCutoffMet: {CutoffMet}",
                    movieDetails.Title,
                    movieDetails.Id,
                    movieDetails.HasFile,
                    movieDetails.HasReachedQualityCutoff);
                return movieDetails;
            }

            Logger.LogWarning("No movie details found in Radarr for ID: {MovieId}", movieId);
            return null;
        },
            $"getting movie details for ID: {movieId.ToString(CultureInfo.InvariantCulture)}")!;
    }
}
