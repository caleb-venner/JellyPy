using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.JellyPy.Configuration.Models;

/// <summary>
/// Represents directory browser results for path selection.
/// </summary>
public class DirectoryBrowser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryBrowser"/> class.
    /// </summary>
    public DirectoryBrowser()
    {
        Directories = new Collection<DirectoryInfoDto>();
    }

    /// <summary>
    /// Gets or sets the current directory path.
    /// </summary>
    public string CurrentPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent directory path, or null if at root.
    /// </summary>
    public string? ParentPath { get; set; }

    /// <summary>
    /// Gets the list of subdirectories in the current path.
    /// </summary>
    public Collection<DirectoryInfoDto> Directories { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this directory contains script files.
    /// </summary>
    public bool HasScripts { get; set; }
}

/// <summary>
/// Represents a directory in the browser results.
/// </summary>
public class DirectoryInfoDto
{
    /// <summary>
    /// Gets or sets the directory name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full directory path.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
