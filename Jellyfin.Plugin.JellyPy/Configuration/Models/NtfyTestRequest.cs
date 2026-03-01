namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Request model for testing ntfy connection.
/// </summary>
public class NtfyTestRequest
{
    /// <summary>
    /// Gets or sets the ntfy server URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ntfy topic.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ntfy access token (optional).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the ntfy username (optional).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the ntfy password (optional).
    /// </summary>
    public string? Password { get; set; }
}
