using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Notifications.Models;

/// <summary>
/// Represents a notification payload for ntfy.
/// </summary>
public class NtfyNotification
{
    /// <summary>
    /// Gets or sets the topic to publish to.
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the notification message body.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification priority (1=min, 2=low, 3=default, 4=high, 5=max).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Gets the tags/emojis for the notification.
    /// </summary>
    [JsonPropertyName("tags")]
    public Collection<string>? Tags { get; } = new();

    /// <summary>
    /// Gets or sets the click URL when notification is tapped.
    /// </summary>
    [JsonPropertyName("click")]
    public string? Click { get; set; }

    /// <summary>
    /// Gets or sets the URL to attach (image/file).
    /// </summary>
    [JsonPropertyName("attach")]
    public string? Attach { get; set; }

    /// <summary>
    /// Gets or sets the filename for the attachment.
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>
    /// Gets or sets the icon URL for the notification.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets action buttons for the notification.
    /// </summary>
    [JsonPropertyName("actions")]
    public Collection<NtfyAction>? Actions { get; } = new();
}
