namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Represents a request to test an executable path.
/// </summary>
public class ExecutablePathRequest
{
    /// <summary>
    /// Gets or sets the executable path to test.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of an executable test.
/// </summary>
public class ExecutableTestResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the executable test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
