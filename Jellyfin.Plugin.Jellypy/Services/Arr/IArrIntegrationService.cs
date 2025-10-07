using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.Jellypy.Services.Arr;

/// <summary>
/// Service for integrating with Sonarr and Radarr APIs.
/// </summary>
public interface IArrIntegrationService
{
    /// <summary>
    /// Processes a media item when playback starts.
    /// For TV shows: monitors upcoming episodes in Sonarr.
    /// For movies: updates monitoring status in Radarr.
    /// </summary>
    /// <param name="item">The media item being played.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessPlaybackStartAsync(BaseItem item);

    /// <summary>
    /// Processes a movie when playback stops.
    /// For movies: conditionally unmonitors based on watch percentage and quality cutoff.
    /// </summary>
    /// <param name="movie">The movie that finished playing.</param>
    /// <param name="watchPercentage">The percentage of the movie that was watched.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessPlaybackStopAsync(Movie movie, double watchPercentage);
}
