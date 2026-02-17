using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Services.Arr.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Service for interacting with Sonarr API.
/// </summary>
public class SonarrService : ArrBaseService<SonarrService>, ISonarrService
{
    private readonly IPluginConfigurationProvider _configProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="configProvider">The configuration provider.</param>
    public SonarrService(
        ILogger<SonarrService> logger,
        IHttpClientFactory httpClientFactory,
        IPluginConfigurationProvider configProvider)
        : base(logger, httpClientFactory)
    {
        _configProvider = configProvider;
    }

    /// <inheritdoc />
    protected override string ServiceName => "Sonarr";

    /// <inheritdoc />
    public async Task<int?> GetSeriesIdByNameAsync(string seriesName)
    {
        return await ExecuteApiCallAsync<int?>(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    Logger.LogWarning("Sonarr API key not configured");
                    return null;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var response = await client.GetAsync(
                    $"/api/v3/series/lookup?term={Uri.EscapeDataString(seriesName)}");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError(
                        "Sonarr API request failed with status code: {StatusCode}",
                        response.StatusCode);
                    return null;
                }

                var series = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>();
                var firstSeries = series?.FirstOrDefault();

                if (firstSeries != null)
                {
                    Logger.LogInformation(
                        "Found Sonarr series: {Title} (ID: {Id})",
                        firstSeries.Title,
                        firstSeries.Id);
                    return firstSeries.Id;
                }

                Logger.LogWarning("No series found in Sonarr for: {SeriesName}", seriesName);
                return null;
            },
            "getting Sonarr series ID",
            seriesName);
    }

    /// <inheritdoc />
    public async Task<int?> GetSeriesIdByTvdbIdAsync(int tvdbId)
    {
        return await ExecuteApiCallAsync<int?>(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return null;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var response = await client.GetAsync($"/api/v3/series?tvdbId={tvdbId}");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError(
                        "Sonarr API request failed with status code: {StatusCode}",
                        response.StatusCode);
                    return null;
                }

                var series = await response.Content.ReadFromJsonAsync<List<SonarrSeries>>();
                return series?.FirstOrDefault()?.Id;
            },
            "getting Sonarr series ID by TVDB ID",
            tvdbId.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<List<SonarrEpisode>> GetEpisodesAsync(int seriesId)
    {
        return await ExecuteApiCallAsync<List<SonarrEpisode>>(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return new List<SonarrEpisode>();
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var response = await client.GetAsync($"/api/v3/episode?seriesId={seriesId}");

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("Failed to get episodes for series {SeriesId}", seriesId);
                    return new List<SonarrEpisode>();
                }

                var episodes = await response.Content.ReadFromJsonAsync<List<SonarrEpisode>>();
                return episodes ?? new List<SonarrEpisode>();
            },
            "getting episodes",
            seriesId.ToString(CultureInfo.InvariantCulture)) ?? new List<SonarrEpisode>();
    }

    /// <inheritdoc />
    public async Task<bool> SetEpisodeMonitoredAsync(int episodeId, bool monitored)
    {
        return await ExecuteApiCallAsync(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return false;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var request = new MonitorEpisodesRequest
                {
                    EpisodeIds = new[] { episodeId },
                    Monitored = monitored,
                };

                var response = await client.PutAsJsonAsync("/api/v3/episode/monitor", request);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogDebug(
                        "Set episode {EpisodeId} monitored status to {Monitored}",
                        episodeId,
                        monitored);
                    return true;
                }

                Logger.LogError(
                    "Failed to set episode monitored status. Status code: {StatusCode}",
                    response.StatusCode);
                return false;
            },
            "setting episode monitored status",
            episodeId.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<bool> SearchEpisodeAsync(int episodeId)
    {
        return await ExecuteApiCallAsync(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return false;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var command = new EpisodeSearchCommand { EpisodeIds = new[] { episodeId } };

                var response = await client.PostAsJsonAsync("/api/v3/command", command);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation("Triggered search for episode {EpisodeId}", episodeId);
                    return true;
                }

                Logger.LogError(
                    "Failed to trigger episode search. Status code: {StatusCode}",
                    response.StatusCode);
                return false;
            },
            "searching for episode",
            episodeId.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<bool> MonitorFutureEpisodesAsync(int seriesId)
    {
        return await ExecuteApiCallAsync(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return false;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var request = new MonitorFutureRequest
                {
                    Series = [new SeriesIdWrapper { Id = seriesId }],
                    MonitoringOptions = new MonitoringOptions { Monitor = "future" },
                };

                var response = await client.PostAsJsonAsync("/api/v3/seasonPass", request);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation(
                        "Enabled future episode monitoring for series {SeriesId}",
                        seriesId);
                    return true;
                }

                Logger.LogError(
                    "Failed to enable future episode monitoring. Status code: {StatusCode}",
                    response.StatusCode);
                return false;
            },
            "monitoring future episodes",
            seriesId.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<SonarrSeries?> GetSeriesAsync(int seriesId)
    {
        return await ExecuteApiCallAsync<SonarrSeries?>(
            async () =>
            {
                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return null;
                }

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var response = await client.GetAsync($"/api/v3/series/{seriesId}");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<SonarrSeries>();
            },
            "getting series",
            seriesId.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<bool> MonitorNewSeasonsAsync(int seriesId)
    {
        return await ExecuteApiCallAsync(
            async () =>
            {
                var series = await GetSeriesAsync(seriesId);
                if (series == null)
                {
                    return false;
                }

                var config = _configProvider.GetConfiguration();

                if (string.IsNullOrEmpty(config.SonarrApiKey))
                {
                    return false;
                }

                series.Monitored = true;
                series.MonitorNewItems = "all";

                var client = CreateHttpClient(config.SonarrUrl, config.SonarrApiKey);
                var response = await client.PutAsJsonAsync($"/api/v3/series/{seriesId}", series);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation(
                        "Enabled new season monitoring for series {SeriesId}",
                        seriesId);
                    return true;
                }

                return false;
            },
            "monitoring new seasons",
            seriesId.ToString(CultureInfo.InvariantCulture));
    }
}
