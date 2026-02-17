using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Represents a Radarr movie.
/// </summary>
public class RadarrMovie
{
    /// <summary>
    /// Gets or sets the Radarr movie ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the movie is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the movie has a file.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }
}
