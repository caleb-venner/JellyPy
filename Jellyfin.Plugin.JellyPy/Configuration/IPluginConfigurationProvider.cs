namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Provides access to plugin configuration.
/// </summary>
public interface IPluginConfigurationProvider
{
    /// <summary>
    /// Gets the current plugin configuration.
    /// </summary>
    /// <returns>The plugin configuration instance.</returns>
    PluginConfiguration GetConfiguration();
}
