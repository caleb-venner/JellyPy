#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.Jellypy.Events;

/// <summary>
/// Standardized event data structure for script execution.
/// </summary>
public class EventData
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public System.DateTime Timestamp { get; set; } = System.DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID associated with the event (if applicable).
    /// </summary>
    public System.Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name associated with the event (if applicable).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the session ID associated with the event (if applicable).
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the media item ID (if applicable).
    /// </summary>
    public System.Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the media item name (if applicable).
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Gets or sets the media item type (if applicable).
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the media item path (if applicable).
    /// </summary>
    public string? ItemPath { get; set; }

    /// <summary>
    /// Gets or sets the library ID (if applicable).
    /// </summary>
    public System.Guid? LibraryId { get; set; }

    /// <summary>
    /// Gets or sets the library name (if applicable).
    /// </summary>
    public string? LibraryName { get; set; }

    /// <summary>
    /// Gets additional event-specific properties.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; } = new();

    /// <summary>
    /// Gets or sets playback position in ticks (for playback events).
    /// </summary>
    public long? PlaybackPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets whether the playback is paused (for playback events).
    /// </summary>
    public bool? IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the client name/application (if applicable).
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the device name (if applicable).
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the device ID (if applicable).
    /// </summary>
    public string? DeviceId { get; set; }

    // Media-specific properties

    /// <summary>
    /// Gets or sets the series name (for TV shows).
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the season number (for TV episodes).
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number (for TV episodes).
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the movie year (for movies).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Gets the genres.
    /// </summary>
    public Collection<string> Genres { get; } = new();

    /// <summary>
    /// Gets or sets the content rating.
    /// </summary>
    public string? ContentRating { get; set; }
}
