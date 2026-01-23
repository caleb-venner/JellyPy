namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Represents a request to test a directory path.
/// </summary>
public class DirectoryPathRequest
{
    /// <summary>
    /// Gets or sets the directory path to test.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a scripts directory test.
/// </summary>
public class ScriptsDirectoryTestResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the directory test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
