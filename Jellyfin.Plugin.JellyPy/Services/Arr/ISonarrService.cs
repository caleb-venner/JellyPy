using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Service for interacting with Sonarr API.
/// </summary>
public interface ISonarrService
{
    /// <summary>
    /// Gets the Sonarr series ID from a series name.
    /// </summary>
    /// <param name="seriesName">The series name to search for.</param>
    /// <returns>The Sonarr series ID, or null if not found.</returns>
    Task<int?> GetSeriesIdByNameAsync(string seriesName);

    /// <summary>
    /// Gets the Sonarr series ID from a TVDB ID.
    /// </summary>
    /// <param name="tvdbId">The TVDB ID.</param>
    /// <returns>The Sonarr series ID, or null if not found.</returns>
    Task<int?> GetSeriesIdByTvdbIdAsync(int tvdbId);

    /// <summary>
    /// Gets all episodes for a series.
    /// </summary>
    /// <param name="seriesId">The Sonarr series ID.</param>
    /// <returns>A list of episodes.</returns>
    Task<List<SonarrEpisode>> GetEpisodesAsync(int seriesId);

    /// <summary>
    /// Sets the monitored status of an episode.
    /// </summary>
    /// <param name="episodeId">The episode ID.</param>
    /// <param name="monitored">Whether the episode should be monitored.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetEpisodeMonitoredAsync(int episodeId, bool monitored);

    /// <summary>
    /// Triggers an automatic search for an episode.
    /// </summary>
    /// <param name="episodeId">The episode ID to search for.</param>
    /// <returns>True if the search was triggered successfully.</returns>
    Task<bool> SearchEpisodeAsync(int episodeId);

    /// <summary>
    /// Enables monitoring for all future episodes of a series.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> MonitorFutureEpisodesAsync(int seriesId);

    /// <summary>
    /// Gets series information.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <returns>The series information.</returns>
    Task<SonarrSeries?> GetSeriesAsync(int seriesId);

    /// <summary>
    /// Updates series to monitor new items.
    /// </summary>
    /// <param name="seriesId">The series ID.</param>
    /// <returns>True if successful.</returns>
    Task<bool> MonitorNewSeasonsAsync(int seriesId);
}
