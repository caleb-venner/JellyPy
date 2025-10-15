using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
                Path.Join(Environment.CurrentDirectory, "scripts"),
                Path.Join(AppContext.BaseDirectory, "scripts")
            };

            var scripts = new List<ScriptFile>();

            foreach (var directory in scriptDirectories.Where(Directory.Exists))
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

            // Remove duplicates based on file name (prefer shorter paths)
            var uniqueScripts = scripts
                .GroupBy(s => s.Name)
                .Select(g => g.OrderBy(s => s.Path.Length).First())
                .ToList();

            _logger.LogInformation("Found {Count} script files", uniqueScripts.Count);
            return Ok(uniqueScripts);
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
            return StatusCode(500, "Error scanning for script files");
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

            using var httpClient = new HttpClient();
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
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
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

            using var httpClient = new HttpClient();
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
            return Ok(new ConnectionTestResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
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
            return Ok(new ApiKeysResponse());
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

/// <summary>
/// Represents a connection test request.
/// </summary>
public class ConnectionTestRequest
{
    /// <summary>
    /// Gets or sets the base URL for the service.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a connection test.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection test was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents the decrypted API keys for display in the web interface.
/// </summary>
public class ApiKeysResponse
{
    /// <summary>
    /// Gets or sets the decrypted Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decrypted Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;
}
