namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Represents a script file.
/// </summary>
public class ScriptFile
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path from the scripts directory.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory containing the script.
    /// </summary>
    public string Directory { get; set; } = string.Empty;
}
