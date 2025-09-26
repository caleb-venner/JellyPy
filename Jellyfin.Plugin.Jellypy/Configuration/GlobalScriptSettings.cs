namespace Jellyfin.Plugin.Jellypy.Configuration;

/// <summary>
/// Global settings for script execution.
/// </summary>
public class GlobalScriptSettings
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent script executions.
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the default timeout for script execution in seconds.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether to enable verbose logging.
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use legacy mode for backward compatibility.
    /// </summary>
    public bool UseLegacyMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the script execution queue size.
    /// </summary>
    public int QueueSize { get; set; } = 100;
}
