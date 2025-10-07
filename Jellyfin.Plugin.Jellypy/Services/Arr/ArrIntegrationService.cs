using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Services.Arr;

/// <summary>
/// Service for integrating with Sonarr and Radarr APIs.
/// </summary>
public class ArrIntegrationService : IArrIntegrationService
{
    private readonly ILogger<ArrIntegrationService> _logger;
    private readonly ISonarrService _sonarrService;
    private readonly IRadarrService _radarrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrIntegrationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sonarrService">The Sonarr service.</param>
    /// <param name="radarrService">The Radarr service.</param>
    public ArrIntegrationService(
        ILogger<ArrIntegrationService> logger,
        ISonarrService sonarrService,
        IRadarrService radarrService)
    {
        _logger = logger;
        _sonarrService = sonarrService;
        _radarrService = radarrService;
    }

    /// <inheritdoc />
    public async Task ProcessPlaybackStartAsync(BaseItem item)
    {
        if (item == null)
        {
            return;
        }

        try
        {
            if (item is Episode episode)
            {
                await ProcessEpisodeAsync(episode);
            }
            else if (item is Movie movie)
            {
                await ProcessMovieAsync(movie);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing playback start for item: {ItemName}", item.Name);
        }
    }

    private async Task ProcessEpisodeAsync(Episode episode)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.EnableNativeSonarrIntegration)
        {
            _logger.LogInformation("Native Sonarr integration is disabled");
            return;
        }

        _logger.LogInformation(
            "Sonarr config check - URL: '{SonarrUrl}', ApiKey: '{ApiKeyPresent}'",
            config.SonarrUrl ?? "(null)",
            string.IsNullOrEmpty(config.SonarrApiKey) ? "empty" : "present");

        if (string.IsNullOrEmpty(config.SonarrApiKey) || string.IsNullOrEmpty(config.SonarrUrl))
        {
            _logger.LogWarning("Sonarr not configured. Skipping episode processing.");
            return;
        }

        var seriesName = episode.Series?.Name ?? episode.SeriesName;
        if (string.IsNullOrEmpty(seriesName))
        {
            _logger.LogWarning("Could not determine series name for episode");
            return;
        }

        _logger.LogInformation(
            "Processing episode: {SeriesName} S{Season}E{Episode}",
            seriesName,
            episode.ParentIndexNumber,
            episode.IndexNumber);

        // Get the series ID from Sonarr
        var seriesId = await _sonarrService.GetSeriesIdByNameAsync(seriesName);
        if (seriesId == null)
        {
            _logger.LogWarning("Could not find series in Sonarr: {SeriesName}", seriesName);
            return;
        }

        // Get all episodes for the series
        var allEpisodes = await _sonarrService.GetEpisodesAsync(seriesId.Value);
        if (allEpisodes.Count == 0)
        {
            _logger.LogWarning("No episodes found for series ID: {SeriesId}", seriesId);
            return;
        }

        // Find the current episode and get next episodes
        var currentSeasonNumber = episode.ParentIndexNumber ?? 0;
        var currentEpisodeNumber = episode.IndexNumber ?? 0;

        var orderedEpisodes = allEpisodes
            .OrderBy(e => e.SeasonNumber)
            .ThenBy(e => e.EpisodeNumber)
            .ToList();

        var currentEpisodeIndex = orderedEpisodes.FindIndex(e =>
            e.SeasonNumber == currentSeasonNumber && e.EpisodeNumber == currentEpisodeNumber);

        if (currentEpisodeIndex < 0)
        {
            _logger.LogWarning(
                "Could not find current episode in Sonarr: S{Season}E{Episode}",
                currentSeasonNumber,
                currentEpisodeNumber);
            return;
        }

        // Unmonitor the current episode (if enabled)
        var currentEp = orderedEpisodes[currentEpisodeIndex];
        if (config.UnmonitorWatchedEpisodes)
        {
            await _sonarrService.SetEpisodeMonitoredAsync(currentEp.Id, false);
            _logger.LogDebug(
                "Unmonitored watched episode: S{Season}E{Episode}",
                currentEp.SeasonNumber,
                currentEp.EpisodeNumber);
        }
        else
        {
            _logger.LogDebug(
                "Keeping episode monitored (UnmonitorWatchedEpisodes is disabled): S{Season}E{Episode}",
                currentEp.SeasonNumber,
                currentEp.EpisodeNumber);
        }

        // Calculate episode buffer with minimum enforcement
        var episodeBuffer = config.EpisodeDownloadBuffer;

        // Count unwatched episodes (episodes without files)
        var unwatchedCount = orderedEpisodes
            .Skip(currentEpisodeIndex + 1)
            .Count(e => !e.HasFile);

        // If we have fewer unwatched episodes than the minimum, increase the buffer
        if (unwatchedCount < config.MinimumEpisodeBuffer)
        {
            var additionalBuffer = config.MinimumEpisodeBuffer - unwatchedCount;
            episodeBuffer = Math.Max(episodeBuffer, episodeBuffer + additionalBuffer);
            _logger.LogInformation(
                "Adjusting episode buffer from {Original} to {Adjusted} (unwatched: {Unwatched}, minimum: {Minimum})",
                config.EpisodeDownloadBuffer,
                episodeBuffer,
                unwatchedCount,
                config.MinimumEpisodeBuffer);
        }

        // Get the next episodes based on configuration
        var nextEpisodesQuery = orderedEpisodes.Skip(currentEpisodeIndex + 1);

        // Filter by current season only if enabled
        if (config.MonitorOnlyCurrentSeason)
        {
            nextEpisodesQuery = nextEpisodesQuery.Where(e => e.SeasonNumber == currentSeasonNumber);
            _logger.LogInformation(
                "MonitorOnlyCurrentSeason enabled - limiting to season {Season}",
                currentSeasonNumber);
        }

        var nextEpisodes = nextEpisodesQuery
            .Take(episodeBuffer)
            .ToList();

        _logger.LogInformation(
            "Found {Count} next episodes to process for {SeriesName}",
            nextEpisodes.Count,
            seriesName);

        // Process each next episode
        foreach (var nextEpisode in nextEpisodes)
        {
            // Check if we should skip episodes with existing files
            if (config.SkipEpisodesWithFiles && nextEpisode.HasFile)
            {
                _logger.LogDebug(
                    "Episode already has file, skipping: S{Season}E{Episode}",
                    nextEpisode.SeasonNumber,
                    nextEpisode.EpisodeNumber);
                continue;
            }

            if (!nextEpisode.HasFile)
            {
                _logger.LogInformation(
                    "Processing next episode: S{Season}E{Episode} - {Title}",
                    nextEpisode.SeasonNumber,
                    nextEpisode.EpisodeNumber,
                    nextEpisode.Title);
            }
            else
            {
                _logger.LogInformation(
                    "Re-monitoring episode with existing file: S{Season}E{Episode} - {Title}",
                    nextEpisode.SeasonNumber,
                    nextEpisode.EpisodeNumber,
                    nextEpisode.Title);
            }

            // Monitor the episode
            await _sonarrService.SetEpisodeMonitoredAsync(nextEpisode.Id, true);

            // Trigger automatic search if enabled
            if (config.AutoSearchEpisodes)
            {
                await _sonarrService.SearchEpisodeAsync(nextEpisode.Id);
            }
        }

        // If we found fewer episodes than the buffer size, enable future episode monitoring
        if (config.MonitorFutureEpisodes && nextEpisodes.Count < episodeBuffer)
        {
            _logger.LogInformation(
                "Found only {Count} remaining episodes (buffer: {Buffer}). Enabling future episode monitoring.",
                nextEpisodes.Count,
                episodeBuffer);
            await _sonarrService.MonitorFutureEpisodesAsync(seriesId.Value);
        }
    }

    private async Task ProcessMovieAsync(Movie movie)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.EnableNativeRadarrIntegration)
        {
            _logger.LogDebug("Native Radarr integration is disabled");
            return;
        }

        if (!config.UnmonitorWatchedMovies)
        {
            _logger.LogDebug("Unmonitoring watched movies is disabled");
            return;
        }

        _logger.LogInformation(
            "Radarr config check - URL: '{RadarrUrl}', ApiKey: '{ApiKeyPresent}'",
            config.RadarrUrl ?? "(null)",
            string.IsNullOrEmpty(config.RadarrApiKey) ? "empty" : "present");

        if (string.IsNullOrEmpty(config.RadarrApiKey) || string.IsNullOrEmpty(config.RadarrUrl))
        {
            _logger.LogWarning("Radarr not configured. Skipping movie processing.");
            return;
        }

        var movieName = movie.Name;
        if (string.IsNullOrEmpty(movieName))
        {
            _logger.LogWarning("Could not determine movie name");
            return;
        }

        _logger.LogInformation("Processing movie: {MovieName}", movieName);

        // Get the movie ID from Radarr
        var movieId = await _radarrService.GetMovieIdByNameAsync(movieName);
        if (movieId == null)
        {
            _logger.LogWarning("Could not find movie in Radarr: {MovieName}", movieName);
            return;
        }

        // Set the movie to unmonitored
        var success = await _radarrService.SetMovieMonitoredAsync(movieId.Value, false);
        if (success)
        {
            _logger.LogInformation(
                "Successfully set movie to unmonitored in Radarr: {MovieName} (ID: {MovieId})",
                movieName,
                movieId);
        }
        else
        {
            _logger.LogError("Failed to set movie to unmonitored: {MovieName}", movieName);
        }
    }

    /// <inheritdoc />
    public async Task ProcessPlaybackStopAsync(Movie movie, double watchPercentage)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.EnableNativeRadarrIntegration)
        {
            _logger.LogDebug("Native Radarr integration is disabled");
            return;
        }

        if (!config.UnmonitorWatchedMovies)
        {
            _logger.LogDebug("Unmonitoring watched movies is disabled");
            return;
        }

        // Check if we should only unmonitor if actually watched
        if (config.UnmonitorOnlyIfWatched)
        {
            if (watchPercentage < config.MinimumWatchPercentage)
            {
                _logger.LogInformation(
                    "Movie {MovieName} not watched enough ({Percentage}% < {Minimum}%). Keeping monitored.",
                    movie.Name,
                    watchPercentage,
                    config.MinimumWatchPercentage);
                return;
            }

            _logger.LogInformation(
                "Movie {MovieName} watched {Percentage}% (>= {Minimum}%). Proceeding with unmonitoring.",
                movie.Name,
                watchPercentage,
                config.MinimumWatchPercentage);
        }

        _logger.LogInformation(
            "Radarr config check - URL: '{RadarrUrl}', ApiKey: '{ApiKeyPresent}'",
            config.RadarrUrl ?? "(null)",
            string.IsNullOrEmpty(config.RadarrApiKey) ? "empty" : "present");

        if (string.IsNullOrEmpty(config.RadarrApiKey) || string.IsNullOrEmpty(config.RadarrUrl))
        {
            _logger.LogWarning("Radarr not configured. Skipping movie processing.");
            return;
        }

        var movieName = movie.Name;
        if (string.IsNullOrEmpty(movieName))
        {
            _logger.LogWarning("Could not determine movie name");
            return;
        }

        _logger.LogInformation("Processing movie on playback stop: {MovieName}", movieName);

        // Get the movie from Radarr
        var movieId = await _radarrService.GetMovieIdByNameAsync(movieName);
        if (movieId == null)
        {
            _logger.LogWarning("Could not find movie in Radarr: {MovieName}", movieName);
            return;
        }

        // Check if we should only unmonitor after quality cutoff is met
        if (config.UnmonitorAfterUpgrade)
        {
            var movieDetails = await _radarrService.GetMovieDetailsAsync(movieId.Value);
            if (movieDetails == null)
            {
                _logger.LogWarning("Could not get movie details from Radarr: {MovieName}", movieName);
                return;
            }

            // Check if the movie has reached its quality cutoff
            if (!movieDetails.HasReachedQualityCutoff)
            {
                _logger.LogInformation(
                    "Movie {MovieName} has not reached quality cutoff. Keeping monitored for upgrades.",
                    movieName);
                return;
            }

            _logger.LogInformation(
                "Movie {MovieName} has reached quality cutoff. Proceeding with unmonitoring.",
                movieName);
        }

        // Set the movie to unmonitored
        var success = await _radarrService.SetMovieMonitoredAsync(movieId.Value, false);
        if (success)
        {
            _logger.LogInformation(
                "Successfully set movie to unmonitored in Radarr: {MovieName} (ID: {MovieId})",
                movieName,
                movieId);
        }
        else
        {
            _logger.LogError("Failed to set movie to unmonitored: {MovieName}", movieName);
        }
    }
}
