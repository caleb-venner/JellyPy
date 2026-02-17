using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Services.Arr.Models;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Service for interacting with Radarr API.
/// </summary>
public interface IRadarrService
{
    /// <summary>
    /// Gets the Radarr movie ID from a movie name.
    /// </summary>
    /// <param name="movieName">The movie name to search for.</param>
    /// <returns>The Radarr movie ID, or null if not found.</returns>
    Task<int?> GetMovieIdByNameAsync(string movieName);

    /// <summary>
    /// Sets the monitored status of a movie.
    /// </summary>
    /// <param name="movieId">The movie ID.</param>
    /// <param name="monitored">Whether the movie should be monitored.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetMovieMonitoredAsync(int movieId, bool monitored);

    /// <summary>
    /// Gets detailed information about a movie including quality profile and file details.
    /// </summary>
    /// <param name="movieId">The movie ID.</param>
    /// <returns>The movie details, or null if not found.</returns>
    Task<RadarrMovieDetails?> GetMovieDetailsAsync(int movieId);
}
