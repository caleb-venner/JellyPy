using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Configuration;

/// <summary>
/// API controller for JellyPy plugin configuration endpoints.
/// </summary>
[ApiController]
[Route("Plugins/Jellypy")]
public class JellyPyApiController : ControllerBase
{
    private readonly ILogger<JellyPyApiController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyPyApiController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public JellyPyApiController(ILogger<JellyPyApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets available script files from the scripts directory.
    /// </summary>
    /// <returns>List of available script files.</returns>
    [HttpGet("scripts")]
    public ActionResult<IEnumerable<ScriptFile>> GetAvailableScripts()
    {
        try
        {
            var scriptDirectories = new[]
            {
                "/config/data/plugins/Jellypy/scripts",  // Docker/Unraid path
                "/jellypy/scripts",                       // Your preferred path
                Path.Combine(Environment.CurrentDirectory, "scripts"),
                Path.Combine(AppContext.BaseDirectory, "scripts")
            };

            var scripts = new List<ScriptFile>();

            foreach (var directory in scriptDirectories)
            {
                if (Directory.Exists(directory))
                {
                    _logger.LogDebug("Scanning directory for scripts: {Directory}", directory);

                    var scriptFiles = Directory.GetFiles(directory, "*.py", SearchOption.AllDirectories)
                        .Select(file => new ScriptFile
                        {
                            Name = Path.GetFileName(file),
                            Path = file,
                            RelativePath = Path.GetRelativePath(directory, file),
                            Directory = directory
                        })
                        .OrderBy(s => s.Name);

                    scripts.AddRange(scriptFiles);
                }
            }

            // Remove duplicates based on file name (prefer shorter paths)
            var uniqueScripts = scripts
                .GroupBy(s => s.Name)
                .Select(g => g.OrderBy(s => s.Path.Length).First())
                .ToList();

            _logger.LogInformation("Found {Count} script files", uniqueScripts.Count);
            return Ok(uniqueScripts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for script files");
            return StatusCode(500, "Error scanning for script files");
        }
    }
}

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
