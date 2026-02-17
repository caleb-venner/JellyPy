using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

/// <summary>
/// Represents a Sonarr series.
/// </summary>
public class SonarrSeries
{
    /// <summary>
    /// Gets or sets the Sonarr series ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the series is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets the monitoring option for new items.
    /// </summary>
    [JsonPropertyName("monitorNewItems")]
    public string MonitorNewItems { get; set; } = string.Empty;
}
