using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// API controller for JellyPy plugin configuration endpoints.
/// </summary>
[ApiController]
[Route("Plugins/Jellypy")]
public class JellyPyApiController : ControllerBase
{
    private readonly ILogger<JellyPyApiController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyPyApiController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public JellyPyApiController(ILogger<JellyPyApiController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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
            var config = Plugin.Instance?.Configuration;
            if (config?.GlobalSettings == null)
            {
                return BadRequest("Configuration not available");
            }

            // Use custom directory if specified, otherwise use default
            string scriptDirectory = string.IsNullOrWhiteSpace(config.GlobalSettings.CustomScriptsDirectory)
                ? PluginConfiguration.ScriptsDirectory
                : config.GlobalSettings.CustomScriptsDirectory;

            var scripts = new List<ScriptFile>();

            if (!Directory.Exists(scriptDirectory))
            {
                _logger.LogInformation("Scripts directory does not exist: {Directory}", scriptDirectory);
                return Ok(scripts);
            }

            _logger.LogDebug("Scanning directory for scripts: {Directory}", scriptDirectory);

            var scriptFiles = Directory.GetFiles(scriptDirectory, "*", SearchOption.AllDirectories)
                .Where(file => IsScriptFile(file))
                .Select(file => new ScriptFile
                {
                    Name = Path.GetFileName(file),
                    Path = file,
                    RelativePath = Path.GetRelativePath(scriptDirectory, file),
                    Directory = scriptDirectory
                })
                .OrderBy(s => s.Name)
                .ToList();

            scripts.AddRange(scriptFiles);

            _logger.LogInformation("Found {Count} script files in {Directory}", scripts.Count, scriptDirectory);
            return Ok(scripts);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when scanning for script files");
            return StatusCode(500, "Access denied when scanning for script files");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error when scanning for script files");
            return StatusCode(500, "I/O error when scanning for script files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error scanning for script files");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <summary>
    /// Determines if a file is a valid script file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a valid script file, false otherwise.</returns>
    private static bool IsScriptFile(string filePath)
    {
        var supportedExtensions = new[] { ".py", ".sh" };
        var extension = Path.GetExtension(filePath);
        return supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Browses directories for the directory selector.
    /// </summary>
    /// <param name="path">The directory path to browse. If empty, uses the home directory.</param>
    /// <returns>Directory contents.</returns>
    [HttpGet("browse-directory")]
    public ActionResult<DirectoryBrowser> BrowseDirectory([FromQuery] string? path)
    {
        try
        {
            // If path is empty, start from home directory or root
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = "/"; // Fallback to root on Unix systems
                }
            }

            // Normalize the path to prevent directory traversal
            path = Path.GetFullPath(path);

            // Ensure the path is a valid directory and exists
            var dirInfo = new System.IO.DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                return BadRequest(new { error = "Directory does not exist" });
            }

            var result = new DirectoryBrowser
            {
                CurrentPath = path,
                ParentPath = dirInfo.Parent?.FullName,
                HasScripts = false
            };

            try
            {
                // Get subdirectories
                var directories = dirInfo.GetDirectories()
                    .OrderBy(d => d.Name)
                    .Take(ApiConstants.MaxDirectoryListingResults)
                    .Select(d => new DirectoryInfoDto { Name = d.Name, Path = d.FullName })
                    .ToList();

                foreach (var dir in directories)
                {
                    result.Directories.Add(dir);
                }

                // Check if this directory has any script files
                result.HasScripts = dirInfo.GetFiles("*.py", SearchOption.TopDirectoryOnly).Length > 0
                    || dirInfo.GetFiles("*.sh", SearchOption.TopDirectoryOnly).Length > 0;
            }
            catch (UnauthorizedAccessException)
            {
                var sanitizedPath = path?.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
                _logger.LogWarning("Access denied when browsing directory: {Path}", sanitizedPath);
                // Return what we can
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid path provided");
            return BadRequest(new { error = "Invalid path" });
        }

        // Generic catch as fallback for unexpected errors during directory browsing
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error browsing directory");
            return StatusCode(500, new { error = "Unexpected error browsing directory" });
        }
    }

    /// <summary>
    /// Tests if a scripts directory exists and contains script files.
    /// </summary>
    /// <param name="request">The test request containing the directory path.</param>
    /// <returns>Test result with script count.</returns>
    [HttpPost("test-scripts-directory")]
    public ActionResult<ScriptsDirectoryTestResult> TestScriptsDirectory([FromBody] DirectoryPathRequest request)
    {
        try
        {
            string path = string.IsNullOrWhiteSpace(request.Path)
                ? PluginConfiguration.ScriptsDirectory
                : request.Path;

            // Normalize and validate path
            path = Path.GetFullPath(path);
            var dirInfo = new System.IO.DirectoryInfo(path);

            if (!dirInfo.Exists)
            {
                return Ok(new ScriptsDirectoryTestResult
                {
                    Success = false,
                    Message = $"Directory does not exist: {path}"
                });
            }

            try
            {
                // Count Python and Bash script files
                var pyFiles = dirInfo.GetFiles("*.py", SearchOption.TopDirectoryOnly);
                var shFiles = dirInfo.GetFiles("*.sh", SearchOption.TopDirectoryOnly);
                int totalScripts = pyFiles.Length + shFiles.Length;

                var sanitizedPath = path?.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
                _logger.LogInformation("Scripts directory test successful: {Path} ({Count} scripts found)", sanitizedPath, totalScripts);

                return Ok(new ScriptsDirectoryTestResult
                {
                    Success = true,
                    Message = $"Directory is valid. Found {totalScripts} script file(s): {pyFiles.Length} Python, {shFiles.Length} Bash"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Ok(new ScriptsDirectoryTestResult
                {
                    Success = false,
                    Message = $"Access denied reading directory: {path}"
                });
            }
        }
        catch (ArgumentException)
        {
            return Ok(new ScriptsDirectoryTestResult
            {
                Success = false,
                Message = "Invalid directory path"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing scripts directory");
            throw;
        }
    }

    /// <summary>
    /// Validates and normalizes an executable path.
    /// </summary>
    /// <param name="executablePath">The executable path to validate.</param>
    /// <returns>Validated path or empty string if invalid.</returns>
    private string ValidateExecutablePath(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return string.Empty;
        }

        string trimmedPath = executablePath.Trim();
        try
        {
            string fullPath = Path.GetFullPath(trimmedPath);
            if (fullPath.Contains("..", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return fullPath;
        }
        catch (ArgumentException ex)
        {
            _logger.LogDebug(ex, "Failed to get full path");
            return string.Empty;
        }
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable format.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>Formatted file size string.</returns>
    private string FormatFileSize(long bytes)
    {
        double len = bytes;
        int order = 0;

        while (len >= FileSizeConstants.BytesPerKilobyte && order < FileSizeConstants.SizeUnits.Length - 1)
        {
            order++;
            len = len / FileSizeConstants.BytesPerKilobyte;
        }

        return $"{len:0.##} {FileSizeConstants.SizeUnits[order]}";
    }

    /// <summary>
    /// Tests an executable path to verify it exists and is readable.
    /// </summary>
    /// <param name="request">The request containing the executable path to test.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    [HttpPost("test-executable")]
    public ActionResult<ExecutableTestResult> TestExecutable([FromBody] ExecutablePathRequest request)
    {
        try
        {
            string execPath = string.IsNullOrWhiteSpace(request.ExecutablePath)
                ? string.Empty
                : request.ExecutablePath.Trim();

            if (string.IsNullOrEmpty(execPath))
            {
                return Ok(new ExecutableTestResult
                {
                    Success = false,
                    Message = "Executable path cannot be empty"
                });
            }

            // Normalize and validate the path
            string validatedPath;
            try
            {
                validatedPath = Path.GetFullPath(execPath);
                // Validate that the path doesn't contain suspicious patterns
                if (string.IsNullOrEmpty(validatedPath) || validatedPath.Contains("..", StringComparison.Ordinal))
                {
                    return Ok(new ExecutableTestResult
                    {
                        Success = false,
                        Message = "Invalid path"
                    });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Error validating executable path");
                return Ok(new ExecutableTestResult
                {
                    Success = false,
                    Message = "Invalid path format"
                });
            }

            // Check if file exists
            if (!System.IO.File.Exists(validatedPath))
            {
                return Ok(new ExecutableTestResult
                {
                    Success = false,
                    Message = $"Executable not found: {validatedPath}"
                });
            }

            // Try to get file information and verify it's executable
            try
            {
#pragma warning disable CA2050 // File path injection
                var fileInfo = new System.IO.FileInfo(validatedPath);
#pragma warning restore CA2050

                // Verify file is readable
                if (!fileInfo.Exists)
                {
                    return Ok(new ExecutableTestResult
                    {
                        Success = false,
                        Message = "File is not accessible"
                    });
                }

                // Check if it's a regular file (not a directory)
                if (fileInfo.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    return Ok(new ExecutableTestResult
                    {
                        Success = false,
                        Message = "Path is a directory, not an executable"
                    });
                }

                // Try to verify it's actually executable by checking if we can read it
                try
                {
                    using (System.IO.File.OpenRead(validatedPath))
                    {
                        // Just opening and closing is enough to verify access
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return Ok(new ExecutableTestResult
                    {
                        Success = false,
                        Message = "Cannot read executable (permission denied)"
                    });
                }
                catch (System.IO.IOException ex)
                {
                    _logger.LogWarning(ex, "IO error accessing executable");
                    return Ok(new ExecutableTestResult
                    {
                        Success = false,
                        Message = "Cannot access executable file"
                    });
                }

                var sanitizedExecutablePath = validatedPath?.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
                _logger.LogInformation("Executable test successful: {Path} (Size: {Size} bytes)", sanitizedExecutablePath, fileInfo.Length);

                return Ok(new ExecutableTestResult
                {
                    Success = true,
                    Message = $"Executable verified: {Path.GetFileName(validatedPath)} ({FormatFileSize(fileInfo.Length)})"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Ok(new ExecutableTestResult
                {
                    Success = false,
                    Message = $"Access denied reading executable: {execPath}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing executable");
            throw;
        }
    }

    /// <summary>
    /// Tests connection to Sonarr API.
    /// </summary>
    /// <param name="request">The connection test request.</param>
    /// <returns>Test result.</returns>
    [HttpPost("test-sonarr")]
    public async Task<ActionResult<ConnectionTestResult>> TestSonarrConnection([FromBody] ConnectionTestRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Url) || string.IsNullOrEmpty(request.ApiKey))
            {
                return BadRequest(new ConnectionTestResult
                {
                    Success = false,
                    Message = "URL and API Key are required"
                });
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ApiConstants.HttpRequestTimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", request.ApiKey);

            // Test Sonarr /system/status endpoint
            string testUrl = $"{request.Url.TrimEnd('/')}/api/v3/system/status";
            var response = await httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var systemInfo = JsonSerializer.Deserialize<JsonElement>(content);

                string version = systemInfo.TryGetProperty("version", out var versionProp) ?
                    versionProp.GetString() ?? "Unknown" : "Unknown";

                return Ok(new ConnectionTestResult
                {
                    Success = true,
                    Message = $"Connected successfully to Sonarr v{version}"
                });
            }
            else
            {
                return Ok(new ConnectionTestResult
                {
                    Success = false,
                    Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Sonarr connection test failed: {Message}", ex.Message);
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Sonarr API response");
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = "Failed to parse Sonarr API response"
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Sonarr connection test timed out");
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = "Connection timed out"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing Sonarr connection");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <summary>
    /// Tests connection to Radarr API.
    /// </summary>
    /// <param name="request">The connection test request.</param>
    /// <returns>Test result.</returns>
    [HttpPost("test-radarr")]
    public async Task<ActionResult<ConnectionTestResult>> TestRadarrConnection([FromBody] ConnectionTestRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Url) || string.IsNullOrEmpty(request.ApiKey))
            {
                return BadRequest(new ConnectionTestResult
                {
                    Success = false,
                    Message = "URL and API Key are required"
                });
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(ApiConstants.HttpRequestTimeoutSeconds);
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", request.ApiKey);

            // Test Radarr /system/status endpoint
            string testUrl = $"{request.Url.TrimEnd('/')}/api/v3/system/status";
            var response = await httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var systemInfo = JsonSerializer.Deserialize<JsonElement>(content);

                string version = systemInfo.TryGetProperty("version", out var versionProp) ?
                    versionProp.GetString() ?? "Unknown" : "Unknown";

                return Ok(new ConnectionTestResult
                {
                    Success = true,
                    Message = $"Connected successfully to Radarr v{version}"
                });
            }
            else
            {
                return Ok(new ConnectionTestResult
                {
                    Success = false,
                    Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
                });
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Radarr connection test failed: {Message}", ex.Message);
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Radarr API response");
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = "Failed to parse Radarr API response"
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Radarr connection test timed out");
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = "Connection timed out"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing Radarr connection");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <summary>
    /// Gets the decrypted API keys for display in the web interface.
    /// </summary>
    /// <returns>The decrypted API keys.</returns>
    [HttpGet("api-keys")]
    public ActionResult<ApiKeysResponse> GetApiKeys()
    {
        try
        {
            if (Plugin.Instance?.Configuration is not PluginConfiguration config)
            {
                return Ok(new ApiKeysResponse());
            }

            return Ok(new ApiKeysResponse
            {
                SonarrApiKey = config.SonarrApiKey, // Uses the compatibility property that auto-decrypts
                RadarrApiKey = config.RadarrApiKey // Uses the compatibility property that auto-decrypts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting decrypted API keys");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }
}
