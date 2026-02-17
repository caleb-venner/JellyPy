using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Request to monitor episodes.
/// </summary>
public class MonitorEpisodesRequest
{
    /// <summary>
    /// Gets or sets the episode IDs to monitor.
    /// </summary>
    [JsonPropertyName("episodeIds")]
    public int[] EpisodeIds { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Gets or sets a value indicating whether episodes should be monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}
