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
        // Legacy properties - no defaults to prevent unwanted execution
        PythonExecutablePath = string.Empty;
        ScriptPath = string.Empty;
        ScriptWorkingDirectory = string.Empty;
        EnableEpisodeProcessing = false;
        EnableMovieProcessing = false;
        ScriptTimeoutSeconds = 300;
        SonarrUrl = string.Empty;
        RadarrUrl = string.Empty;

        // Modern configuration
        ScriptSettings = new Collection<ScriptSetting>();
        GlobalSettings = new GlobalScriptSettings();

        // Native integration settings - enabled by default
        EnableNativeSonarrIntegration = true;
        EnableNativeRadarrIntegration = true;
        EpisodeDownloadBuffer = 5;
        AutoSearchEpisodes = true;
        MonitorFutureEpisodes = true;
        UnmonitorWatchedMovies = true;
        SkipEpisodesWithFiles = true;
        UnmonitorWatchedEpisodes = true;
        MonitorOnlyCurrentSeason = false;
        MinimumEpisodeBuffer = 2;
        UnmonitorOnlyIfWatched = false;
        MinimumWatchPercentage = 90;
        UnmonitorAfterUpgrade = false;
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
    public string SonarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr API key used when invoking the script.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr base URL used when invoking the script.
    /// </summary>
    public string RadarrUrl { get; set; } = string.Empty;

    // Native Sonarr/Radarr Integration Settings

    /// <summary>
    /// Gets or sets a value indicating whether native Sonarr integration is enabled.
    /// When enabled, episodes will be automatically downloaded without requiring Python scripts.
    /// </summary>
    public bool EnableNativeSonarrIntegration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether native Radarr integration is enabled.
    /// When enabled, movies will be automatically unmonitored after watching without requiring Python scripts.
    /// </summary>
    public bool EnableNativeRadarrIntegration { get; set; }

    /// <summary>
    /// Gets or sets the number of next episodes to download when watching a TV show.
    /// Default is 5 episodes.
    /// </summary>
    public int EpisodeDownloadBuffer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically search for next episodes.
    /// When true, triggers Sonarr to search for the next episodes immediately.
    /// </summary>
    public bool AutoSearchEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to monitor future episodes automatically.
    /// When true and remaining episodes are less than the buffer, all future episodes will be monitored.
    /// </summary>
    public bool MonitorFutureEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to unmonitor movies after they are watched.
    /// When true, sets movies to unmonitored in Radarr when playback starts.
    /// </summary>
    public bool UnmonitorWatchedMovies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip monitoring episodes that already have files.
    /// When true, only monitors episodes that are missing files (default behavior).
    /// When false, monitors all next episodes regardless of file existence (useful for upgrades).
    /// </summary>
    public bool SkipEpisodesWithFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to unmonitor episodes after watching them.
    /// When false, episodes remain monitored after watching (useful for quality upgrades).
    /// Default is true.
    /// </summary>
    public bool UnmonitorWatchedEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only monitor episodes in the current season being watched.
    /// When true, only monitors episodes in the same season as the episode you just watched.
    /// Episodes in future seasons will not be monitored until you start watching that season.
    /// Default is false (monitors episodes across all future seasons).
    /// </summary>
    public bool MonitorOnlyCurrentSeason { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of unwatched episodes to maintain in the queue.
    /// If unwatched count drops below this value, additional episodes will be monitored.
    /// Default is 2 episodes.
    /// </summary>
    public int MinimumEpisodeBuffer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only unmonitor movies after they've been watched.
    /// When true, checks playback progress and only unmonitors if watch percentage exceeds threshold.
    /// When false, unmonitors immediately on playback start.
    /// Default is false.
    /// </summary>
    public bool UnmonitorOnlyIfWatched { get; set; }

    /// <summary>
    /// Gets or sets the minimum watch percentage required to unmonitor a movie.
    /// Only used when UnmonitorOnlyIfWatched is true.
    /// Default is 90 percent.
    /// </summary>
    public int MinimumWatchPercentage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only unmonitor movies after they reach quality cutoff.
    /// When true, keeps movies monitored until they meet the quality profile cutoff.
    /// Default is false.
    /// </summary>
    public bool UnmonitorAfterUpgrade { get; set; }

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
