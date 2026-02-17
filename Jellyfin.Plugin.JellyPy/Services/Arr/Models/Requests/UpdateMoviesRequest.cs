using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Request to update movies.
/// </summary>
public class UpdateMoviesRequest
{
    /// <summary>
    /// Gets or sets the movie IDs to update.
    /// </summary>
    [JsonPropertyName("movieIds")]
    public int[] MovieIds { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Gets or sets a value indicating whether movies should be monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}
