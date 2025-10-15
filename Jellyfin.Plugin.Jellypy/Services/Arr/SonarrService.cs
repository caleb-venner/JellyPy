using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Service for interacting with Sonarr API.
/// </summary>
public class SonarrService : ISonarrService
{
    private readonly ILogger<SonarrService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public SonarrService(ILogger<SonarrService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<int?> GetSeriesIdByNameAsync(string seriesName)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                _logger.LogWarning("Sonarr API key not configured");
                return null;
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/series/lookup?term={Uri.EscapeDataString(seriesName)}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Sonarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var series = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>();
            var firstSeries = series?.FirstOrDefault();

            if (firstSeries != null)
            {
                _logger.LogInformation("Found Sonarr series: {Title} (ID: {Id})", firstSeries.Title, firstSeries.Id);
                return firstSeries.Id;
            }

            _logger.LogWarning("No series found in Sonarr for: {SeriesName}", seriesName);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting Sonarr series ID for: {SeriesName}", seriesName);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Sonarr API response for series: {SeriesName}", seriesName);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting Sonarr series ID for: {SeriesName}", seriesName);
            return null;
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid Sonarr URL format when getting series ID for: {SeriesName}", seriesName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting Sonarr series ID for: {SeriesName}", seriesName);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetSeriesIdByTvdbIdAsync(int tvdbId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return null;
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/series?tvdbId={tvdbId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Sonarr API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var series = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>();
            return series?.FirstOrDefault()?.Id;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting Sonarr series ID by TVDB ID: {TvdbId}", tvdbId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Sonarr API response for TVDB ID: {TvdbId}", tvdbId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting Sonarr series ID by TVDB ID: {TvdbId}", tvdbId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting Sonarr series ID by TVDB ID: {TvdbId}", tvdbId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<List<SonarrEpisode>> GetEpisodesAsync(int seriesId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return new List<SonarrEpisode>();
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/episode?seriesId={seriesId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get episodes for series {SeriesId}", seriesId);
                return new List<SonarrEpisode>();
            }

            var episodes = await response.Content.ReadFromJsonAsync<List<SonarrEpisode>>();
            return episodes ?? new List<SonarrEpisode>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting episodes for series: {SeriesId}", seriesId);
            return new List<SonarrEpisode>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Sonarr API response for series episodes: {SeriesId}", seriesId);
            return new List<SonarrEpisode>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting episodes for series: {SeriesId}", seriesId);
            return new List<SonarrEpisode>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting episodes for series: {SeriesId}", seriesId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetEpisodeMonitoredAsync(int episodeId, bool monitored)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return false;
            }

            var client = CreateHttpClient(config);
            var request = new MonitorEpisodesRequest
            {
                EpisodeIds = new[] { episodeId },
                Monitored = monitored
            };

            var response = await client.PutAsJsonAsync("/api/v3/episode/monitor", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Set episode {EpisodeId} monitored status to {Monitored}", episodeId, monitored);
                return true;
            }

            _logger.LogError("Failed to set episode monitored status. Status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when setting episode monitored status: {EpisodeId}", episodeId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize Sonarr API request for episode: {EpisodeId}", episodeId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when setting episode monitored status: {EpisodeId}", episodeId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting episode monitored status: {EpisodeId}", episodeId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<bool> SearchEpisodeAsync(int episodeId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return false;
            }

            var client = CreateHttpClient(config);
            var command = new EpisodeSearchCommand
            {
                EpisodeIds = new[] { episodeId }
            };

            var response = await client.PostAsJsonAsync("/api/v3/command", command);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Triggered search for episode {EpisodeId}", episodeId);
                return true;
            }

            _logger.LogError("Failed to trigger episode search. Status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when searching for episode: {EpisodeId}", episodeId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize Sonarr API request for episode search: {EpisodeId}", episodeId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when searching for episode: {EpisodeId}", episodeId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching for episode: {EpisodeId}", episodeId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<bool> MonitorFutureEpisodesAsync(int seriesId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return false;
            }

            var client = CreateHttpClient(config);
            var request = new MonitorFutureRequest
            {
                Series = new[] { new SeriesIdWrapper { Id = seriesId } },
                MonitoringOptions = new MonitoringOptions { Monitor = "future" }
            };

            var response = await client.PostAsJsonAsync("/api/v3/seasonPass", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Enabled future episode monitoring for series {SeriesId}", seriesId);
                return true;
            }

            _logger.LogError("Failed to enable future episode monitoring. Status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when monitoring future episodes for series: {SeriesId}", seriesId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize Sonarr API request for future monitoring: {SeriesId}", seriesId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when monitoring future episodes for series: {SeriesId}", seriesId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error monitoring future episodes for series: {SeriesId}", seriesId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<SonarrSeries?> GetSeriesAsync(int seriesId)
    {
        try
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return null;
            }

            var client = CreateHttpClient(config);
            var response = await client.GetAsync($"/api/v3/series/{seriesId}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SonarrSeries>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when getting series: {SeriesId}", seriesId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Sonarr API response for series: {SeriesId}", seriesId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when getting series: {SeriesId}", seriesId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting series: {SeriesId}", seriesId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <inheritdoc />
    public async Task<bool> MonitorNewSeasonsAsync(int seriesId)
    {
        try
        {
            var series = await GetSeriesAsync(seriesId);
            if (series == null)
            {
                return false;
            }

            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.SonarrApiKey))
            {
                return false;
            }

            series.Monitored = true;
            series.MonitorNewItems = "all";

            var client = CreateHttpClient(config);
            var response = await client.PutAsJsonAsync($"/api/v3/series/{seriesId}", series);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Enabled new season monitoring for series {SeriesId}", seriesId);
                return true;
            }

            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when monitoring new seasons for series: {SeriesId}", seriesId);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize Sonarr API request for new season monitoring: {SeriesId}", seriesId);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when monitoring new seasons for series: {SeriesId}", seriesId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error monitoring new seasons for series: {SeriesId}", seriesId);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private HttpClient CreateHttpClient(PluginConfiguration config)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(config.SonarrUrl.TrimEnd('/'));
        client.DefaultRequestHeaders.Add("X-Api-Key", config.SonarrApiKey);
        return client;
    }
}
