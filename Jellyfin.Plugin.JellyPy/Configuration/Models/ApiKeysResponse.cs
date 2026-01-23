namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Represents the decrypted API keys for display in the web interface.
/// </summary>
public class ApiKeysResponse
{
    /// <summary>
    /// Gets or sets the decrypted Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decrypted Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;
}
