using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

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

/// <summary>
/// Request for monitoring future episodes.
/// </summary>
public class MonitorFutureRequest
{
    /// <summary>
    /// Gets or sets the series IDs.
    /// </summary>
    [JsonPropertyName("series")]
    public SeriesIdWrapper[] Series { get; set; } = System.Array.Empty<SeriesIdWrapper>();

    /// <summary>
    /// Gets or sets the monitoring options.
    /// </summary>
    [JsonPropertyName("monitoringOptions")]
    public MonitoringOptions MonitoringOptions { get; set; } = new MonitoringOptions();
}

/// <summary>
/// Wrapper for series ID.
/// </summary>
public class SeriesIdWrapper
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }
}

/// <summary>
/// Monitoring options.
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Gets or sets the monitor type.
    /// </summary>
    [JsonPropertyName("monitor")]
    public string Monitor { get; set; } = "future";
}
