using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Command request for episode search.
/// </summary>
public class EpisodeSearchCommand
{
    /// <summary>
    /// Gets or sets the command name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "EpisodeSearch";

    /// <summary>
    /// Gets or sets the episode IDs to search.
    /// </summary>
    [JsonPropertyName("episodeIds")]
    public int[] EpisodeIds { get; set; } = System.Array.Empty<int>();
}
