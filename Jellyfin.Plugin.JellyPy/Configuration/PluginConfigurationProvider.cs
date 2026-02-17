namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Provides access to plugin configuration through dependency injection.
/// </summary>
public class PluginConfigurationProvider : IPluginConfigurationProvider
{
    /// <inheritdoc />
    public PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }
}
