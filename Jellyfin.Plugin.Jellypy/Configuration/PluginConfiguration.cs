using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellypy.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        PythonExecutablePath = "/usr/bin/python3";
        ScriptPath = "/config/scripts/download.py";
        ScriptWorkingDirectory = "/config/scripts";
        EnableEpisodeProcessing = true;
        EnableMovieProcessing = true;
        ScriptTimeoutSeconds = 300;
        SonarrUrl = "http://sonarr:8989";
        RadarrUrl = "http://radarr:7878";
        ScriptSettings = new Collection<ScriptSetting>();
        GlobalSettings = new GlobalScriptSettings();
    }

    // Legacy configuration properties (maintained for backward compatibility)

    /// <summary>
    /// Gets or sets the absolute path to the Python interpreter used to run scripts.
    /// </summary>
    public string PythonExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the script that should be executed.
    /// </summary>
    public string ScriptPath { get; set; }

    /// <summary>
    /// Gets or sets the optional working directory for the script process.
    /// </summary>
    public string ScriptWorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets an optional argument string appended to every invocation.
    /// </summary>
    public string AdditionalArguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to invoke the script for episodes.
    /// </summary>
    public bool EnableEpisodeProcessing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to invoke the script for movies.
    /// </summary>
    public bool EnableMovieProcessing { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of time to wait for the script process to exit.
    /// </summary>
    public int ScriptTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the Sonarr API key used when invoking the script.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr base URL used when invoking the script.
    /// </summary>
    public string SonarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Radarr API key used when invoking the script.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr base URL used when invoking the script.
    /// </summary>
    public string RadarrUrl { get; set; }

    // New enhanced configuration properties

    /// <summary>
    /// Gets the list of script settings for enhanced event handling.
    /// </summary>
    public Collection<ScriptSetting> ScriptSettings { get; }

    /// <summary>
    /// Gets or sets global settings for script execution.
    /// </summary>
    public GlobalScriptSettings GlobalSettings { get; set; }
}
