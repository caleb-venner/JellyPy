using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Notifications.Models;

/// <summary>
/// Represents an action button for ntfy notifications.
/// </summary>
public class NtfyAction
{
    /// <summary>
    /// Gets or sets the action type (view, broadcast, http).
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "view";

    /// <summary>
    /// Gets or sets the action label text.
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for view/http actions.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets whether to clear the notification after action.
    /// </summary>
    [JsonPropertyName("clear")]
    public bool? Clear { get; set; }
}
