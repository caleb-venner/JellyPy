using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr.Models;

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
