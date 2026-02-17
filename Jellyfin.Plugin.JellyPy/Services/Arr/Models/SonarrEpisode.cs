using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Represents a Sonarr episode.
/// </summary>
public class SonarrEpisode
{
    /// <summary>
    /// Gets or sets the episode ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the episode has a file.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the episode is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}
