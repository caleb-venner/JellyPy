namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Global settings for script execution.
/// </summary>
public class GlobalScriptSettings
{
    /// <summary>
    /// Gets or sets the custom directory to scan for scripts.
    /// If empty or null, uses the default scripts directory within the Jellyfin installation.
    /// </summary>
    public string CustomScriptsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the Python interpreter executable.
    /// Examples: /usr/bin/python3, C:\Python311\python.exe.
    /// </summary>
    public string PythonExecutablePath { get; set; } = "/usr/bin/python3";

    /// <summary>
    /// Gets or sets the path to the Bash interpreter executable.
    /// Examples: /bin/bash, /usr/local/bin/bash.
    /// </summary>
    public string BashExecutablePath { get; set; } = "/bin/bash";

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
