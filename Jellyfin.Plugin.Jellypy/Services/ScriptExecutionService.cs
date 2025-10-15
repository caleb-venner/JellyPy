#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellypy.Configuration;
using Jellyfin.Plugin.Jellypy.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Services;

/// <summary>
/// Service responsible for executing scripts based on events.
/// </summary>
public class ScriptExecutionService : IScriptExecutionService, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private readonly ILogger<ScriptExecutionService> _logger;
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly DataAttributeProcessor _dataAttributeProcessor;
    private readonly SemaphoreSlim _scriptSemaphore;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="conditionEvaluator">The condition evaluator service.</param>
    /// <param name="dataAttributeProcessor">The data attribute processor service.</param>
    public ScriptExecutionService(
        ILogger<ScriptExecutionService> logger,
        ConditionEvaluator conditionEvaluator,
        DataAttributeProcessor dataAttributeProcessor)
    {
        _logger = logger;
        _conditionEvaluator = conditionEvaluator;
        _dataAttributeProcessor = dataAttributeProcessor;
        _scriptSemaphore = new SemaphoreSlim(5, 5); // Allow up to 5 concurrent scripts
    }

    /// <inheritdoc />
    public async Task ExecuteScriptsAsync(EventData eventData)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null)
        {
            _logger.LogWarning("Plugin configuration is unavailable; skipping script execution for event {EventType}", eventData.EventType);
            return;
        }

        // Execute enhanced script settings if available
        if (config.ScriptSettings?.Count > 0)
        {
            await ExecuteEnhancedScriptsAsync(config, eventData).ConfigureAwait(false);
        }

        // Execute legacy scripts for backward compatibility
        if (ShouldExecuteLegacyScript(eventData.EventType, config) && IsValidLegacyConfiguration(config))
        {
            await ExecuteLegacyScriptAsync(config, eventData).ConfigureAwait(false);
        }
    }

    private async Task ExecuteEnhancedScriptsAsync(PluginConfiguration config, EventData eventData)
    {
        var applicableSettings = config.ScriptSettings
            .Where(setting => setting.Triggers.Contains(eventData.EventType))
            .ToList();

        if (applicableSettings.Count == 0)
        {
            _logger.LogDebug("No script settings configured for event type {EventType}", eventData.EventType);
            return;
        }

        var globalSettings = config.GlobalSettings ?? new GlobalScriptSettings();
        using var semaphore = new SemaphoreSlim(globalSettings.MaxConcurrentExecutions, globalSettings.MaxConcurrentExecutions);

        var tasks = applicableSettings.Select(async setting =>
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_conditionEvaluator.EvaluateConditions(setting.Conditions, eventData))
                {
                    await ExecuteScriptSettingAsync(setting, eventData, globalSettings).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogDebug("Conditions not met for script setting {SettingId}", setting.Id);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteScriptSettingAsync(ScriptSetting setting, EventData eventData, GlobalScriptSettings globalSettings)
    {
        try
        {
            var (arguments, environmentVariables) = _dataAttributeProcessor.ProcessDataAttributes(setting.DataAttributes, eventData);

            var executorPath = GetExecutorPath(setting.Execution.ExecutorType, setting.Execution.ExecutablePath);
            if (string.IsNullOrEmpty(executorPath))
            {
                _logger.LogError("Executor path not configured for script setting {SettingId}", setting.Id);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executorPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(setting.Execution.ScriptPath) ?? Environment.CurrentDirectory
            };

            // Add script path as first argument
            startInfo.ArgumentList.Add(setting.Execution.ScriptPath);

            // Add processed arguments
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // Add custom arguments
            if (!string.IsNullOrEmpty(setting.Execution.AdditionalArguments))
            {
                foreach (var arg in TokenizeAdditionalArguments(setting.Execution.AdditionalArguments))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            // Add environment variables
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }

            await ExecuteProcessAsync(startInfo, setting.Execution.TimeoutSeconds, eventData.EventType).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when executing script setting {SettingId} for event {EventType}", setting.Id, eventData.EventType);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start process for script setting {SettingId} for event {EventType}", setting.Id, eventData.EventType);
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This script execution method should not swallow exceptions.
            _logger.LogError(ex, "Unexpected error executing script setting {SettingId} for event {EventType}", setting.Id, eventData.EventType);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private string GetExecutorPath(ScriptExecutorType executorType, string customExecutorPath)
    {
        // If user explicitly set a path (and it's not default), use it
        if (!string.IsNullOrWhiteSpace(customExecutorPath) &&
            customExecutorPath != "auto" &&
            !IsDefaultPath(executorType, customExecutorPath))
        {
            return customExecutorPath;
        }

        // Auto-detect based on executor type
        return executorType switch
        {
            ScriptExecutorType.Python => ResolvePythonPath(),
            ScriptExecutorType.PowerShell => ResolvePowerShellPath(),
            ScriptExecutorType.Bash => ResolveBashPath(),
            ScriptExecutorType.NodeJs => ResolveNodePath(),
            ScriptExecutorType.Binary => string.Empty, // Binary is the script itself
            _ => customExecutorPath ?? string.Empty
        };
    }

    private static bool IsDefaultPath(ScriptExecutorType executorType, string path)
    {
        return executorType switch
        {
            ScriptExecutorType.Python => path.Equals("/usr/bin/python3", StringComparison.OrdinalIgnoreCase),
            ScriptExecutorType.PowerShell => path.Equals("/usr/local/bin/pwsh", StringComparison.OrdinalIgnoreCase),
            ScriptExecutorType.Bash => path.Equals("/bin/bash", StringComparison.OrdinalIgnoreCase),
            ScriptExecutorType.NodeJs => path.Equals("/usr/bin/node", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// Resolves Python executable path with multiple fallback strategies.
    /// </summary>
    private string ResolvePythonPath()
    {
        // Get plugin directory for potential bundled runtime
        var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        var candidates = new[]
        {
            // 1. Bundled Python in plugin directory (future enhancement)
            Path.Join(pluginDirectory, "runtime", "bin", "python3"),
            Path.Join(pluginDirectory, "runtime", "bin", "python"),

            // 2. Common Docker container locations
            "/usr/bin/python3",
            "/usr/bin/python",
            "/usr/local/bin/python3",
            "/usr/local/bin/python",

            // 3. Alpine Linux locations (common in Docker)
            "/usr/bin/python3.12",
            "/usr/bin/python3.11",
            "/usr/bin/python3.10",
            "/usr/bin/python3.9",

            // 4. System PATH locations
            "python3",
            "python"
        };

        var pythonPath = candidates.FirstOrDefault(IsPythonExecutable);
        if (pythonPath != null)
        {
            _logger.LogInformation("Found Python executable: {Path}", pythonPath);
            return pythonPath;
        }

        // Log detailed diagnostic info for troubleshooting
        LogPythonDiagnostics();

        // Fallback to default - will likely fail but provides clear error
        var fallback = "/usr/bin/python3";
        _logger.LogWarning("No Python executable found. Using fallback: {Path}", fallback);
        return fallback;
    }

    /// <summary>
    /// Resolves PowerShell executable path.
    /// </summary>
    private string ResolvePowerShellPath()
    {
        var candidates = new[] { "pwsh", "powershell", "/usr/bin/pwsh", "/usr/local/bin/pwsh" };

        var pwshPath = candidates.FirstOrDefault(IsExecutableAvailable);
        if (pwshPath != null)
        {
            _logger.LogInformation("Found PowerShell executable: {Path}", pwshPath);
            return pwshPath;
        }

        _logger.LogWarning("No PowerShell executable found. Using fallback: pwsh");
        return "pwsh"; // Fallback
    }

    /// <summary>
    /// Resolves Bash executable path.
    /// </summary>
    private string ResolveBashPath()
    {
        var candidates = new[] { "/bin/bash", "/usr/bin/bash", "bash", "/bin/sh", "sh" };

        var bashPath = candidates.FirstOrDefault(IsExecutableAvailable);
        if (bashPath != null)
        {
            _logger.LogInformation("Found Bash executable: {Path}", bashPath);
            return bashPath;
        }

        _logger.LogWarning("No Bash executable found. Using fallback: /bin/bash");
        return "/bin/bash"; // Fallback
    }

    /// <summary>
    /// Resolves Node.js executable path.
    /// </summary>
    private string ResolveNodePath()
    {
        var candidates = new[] { "node", "/usr/bin/node", "/usr/local/bin/node", "nodejs" };

        var nodePath = candidates.FirstOrDefault(IsExecutableAvailable);
        if (nodePath != null)
        {
            _logger.LogInformation("Found Node.js executable: {Path}", nodePath);
            return nodePath;
        }

        _logger.LogWarning("No Node.js executable found. Using fallback: node");
        return "node"; // Fallback
    }

    /// <summary>
    /// Checks if a path points to a valid Python executable.
    /// </summary>
    private bool IsPythonExecutable(string path)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            if (process.Start())
            {
                var completed = process.WaitForExit(5000);
                if (completed && process.ExitCode == 0)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    _logger.LogDebug("Python version check for {Path}: {Output}", path, output.Trim());
                    return output.Contains("Python", StringComparison.OrdinalIgnoreCase);
                }

                if (!completed)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (Exception)
                    {
                        // CA1031: Ignore exceptions when killing a process that may already be dead or in invalid state.
                        // This is cleanup code where failure is acceptable.
                    }
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug("Failed to check Python executable {Path}: {Error}", path, ex.Message);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogDebug("Python executable not found at {Path}: {Error}", path, ex.Message);
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This validation method should not swallow exceptions.
            _logger.LogDebug("Unexpected error checking Python executable {Path}: {Error}", path, ex.Message);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }

        return false;
    }

    /// <summary>
    /// Checks if an executable is available on the system.
    /// </summary>
    private bool IsExecutableAvailable(string executableName)
    {
        try
        {
            // Try using 'which' command first (Unix-like systems)
            using var whichProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = executableName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            if (whichProcess.Start())
            {
                var completed = whichProcess.WaitForExit(3000);
                if (completed && whichProcess.ExitCode == 0)
                {
                    return true;
                }

                if (!completed)
                {
                    try
                    {
                        whichProcess.Kill(true);
                    }
                    catch (Exception)
                    {
                        // CA1031: Ignore exceptions when killing a process that may already be dead or in invalid state.
                        // This is cleanup code where failure is acceptable.
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // If 'which' command fails, try direct execution
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // If 'which' command fails, try direct execution
        }
        catch (Exception)
        {
            // If 'which' command fails, try direct execution
        }

        // Fallback: try direct execution
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            if (process.Start())
            {
                var completed = process.WaitForExit(3000);
                if (completed && process.ExitCode == 0)
                {
                    return true;
                }

                if (!completed)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch (Exception)
                    {
                        // CA1031: Ignore exceptions when killing a process that may already be dead or in invalid state.
                        // This is cleanup code where failure is acceptable.
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Ignore
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Ignore
        }
        catch (Exception)
        {
            // CA1031: Ignore exceptions from 'which' command - we fall back to direct execution.
            // This is intentional fallback logic where failure is acceptable.
        }

        return false;
    }

    /// <summary>
    /// Logs diagnostic information to help with troubleshooting Python detection.
    /// </summary>
    private void LogPythonDiagnostics()
    {
        try
        {
            _logger.LogWarning("Python auto-detection failed. Diagnostic information:");

            // Log environment info
            _logger.LogInformation("Operating System: {OS}", RuntimeInformation.OSDescription);
            _logger.LogInformation("Architecture: {Arch}", RuntimeInformation.OSArchitecture);

            // Log common directories
            var directories = new[] { "/usr/bin", "/usr/local/bin", "/bin" };
            foreach (var dir in directories)
            {
                if (Directory.Exists(dir))
                {
                    var pythonFiles = Directory.GetFiles(dir, "python*")
                        .Where(f => !Path.GetFileName(f).Contains("config", StringComparison.OrdinalIgnoreCase))
                        .Take(5);

                    if (pythonFiles.Any())
                    {
                        _logger.LogInformation(
                            "Python-like files in {Directory}: {Files}",
                            dir,
                            string.Join(", ", pythonFiles.Select(Path.GetFileName)));
                    }
                }
                else
                {
                    _logger.LogDebug("Directory does not exist: {Directory}", dir);
                }
            }

            // Log PATH environment variable
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                var paths = pathVar.Split(':', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("PATH contains {Count} directories", paths.Length);
                _logger.LogDebug("PATH directories: {Paths}", string.Join(", ", paths.Take(10)));
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug("Failed to collect Python diagnostics (invalid operation): {Error}", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogDebug("Failed to collect Python diagnostics (access denied): {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This diagnostics method should not swallow exceptions.
            _logger.LogDebug("Failed to collect Python diagnostics: {Error}", ex.Message);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private async Task ExecuteLegacyScriptAsync(PluginConfiguration config, EventData eventData)
    {
        await _scriptSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await ExecuteScriptInternal(config, eventData).ConfigureAwait(false);
        }
        finally
        {
            _scriptSemaphore.Release();
        }
    }

    private static bool ShouldExecuteLegacyScript(EventType eventType, PluginConfiguration config)
    {
        // For now, map event types to existing configuration flags
        return eventType switch
        {
            EventType.PlaybackStart => true, // Always enabled for now
            EventType.PlaybackStop => true,  // Always enabled for now
            EventType.PlaybackPause => true, // Always enabled for now
            EventType.PlaybackResume => true, // Always enabled for now
            EventType.ItemAdded => config.EnableEpisodeProcessing || config.EnableMovieProcessing,
            EventType.ItemUpdated => config.EnableEpisodeProcessing || config.EnableMovieProcessing,
            EventType.UserCreated => true, // Always enabled for now
            _ => false
        };
    }

    private static bool IsValidLegacyConfiguration(PluginConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.PythonExecutablePath) || !File.Exists(config.PythonExecutablePath))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.ScriptPath) || !File.Exists(config.ScriptPath))
        {
            return false;
        }

        return true;
    }

    private async Task ExecuteProcessAsync(ProcessStartInfo startInfo, int timeoutSeconds, EventType eventType)
    {
        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start external script process for event {EventType}", eventType);
                return;
            }

            using var cts = CreateTimeoutToken(timeoutSeconds);
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            try
            {
                if (cts is not null)
                {
                    await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError(
                    "Script execution timed out after {TimeoutSeconds} seconds for event {EventType}",
                    timeoutSeconds,
                    eventType);

                try
                {
                    process.Kill(true);
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }
                catch (Exception)
                {
                    // CA1031: Ignore exceptions when killing a process that may already be dead or in invalid state.
                    // This is cleanup code where failure is acceptable.
                }

                return;
            }

            var stdOut = await stdOutTask.ConfigureAwait(false);
            var stdErr = await stdErrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "Script execution failed with exit code {ExitCode} for event {EventType}. StdErr: {StdErr}",
                    process.ExitCode,
                    eventType,
                    stdErr);
            }
            else
            {
                _logger.LogDebug(
                    "Script executed successfully for event {EventType}. StdOut: {StdOut}",
                    eventType,
                    stdOut);
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Script execution was cancelled for event {EventType}", eventType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when executing script process for event {EventType}", eventType);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start script process for event {EventType}", eventType);
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This script execution method should not swallow exceptions.
            _logger.LogError(ex, "Unexpected error executing script process for event {EventType}", eventType);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private static bool ShouldExecuteForEvent(EventType eventType, PluginConfiguration config)
    {
        // For now, map event types to existing configuration flags
        return eventType switch
        {
            EventType.PlaybackStart => true, // Always enabled for now
            EventType.PlaybackStop => true,  // Always enabled for now
            EventType.PlaybackPause => true, // Always enabled for now
            EventType.PlaybackResume => true, // Always enabled for now
            EventType.ItemAdded => config.EnableEpisodeProcessing || config.EnableMovieProcessing,
            EventType.ItemUpdated => config.EnableEpisodeProcessing || config.EnableMovieProcessing,
            EventType.UserCreated => true, // Always enabled for now
            _ => false
        };
    }

    private static bool IsValidConfiguration(PluginConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.PythonExecutablePath) || !File.Exists(config.PythonExecutablePath))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.ScriptPath) || !File.Exists(config.ScriptPath))
        {
            return false;
        }

        return true;
    }

    private async Task ExecuteScriptInternal(PluginConfiguration config, EventData eventData)
    {
        var arguments = BuildScriptArguments(config, eventData);
        var environmentVars = BuildEnvironmentVariables(config, eventData);

        var startInfo = new ProcessStartInfo
        {
            FileName = config.PythonExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = ResolveWorkingDirectory(config)
        };

        // Add script arguments
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Add environment variables
        foreach (var (key, value) in environmentVars)
        {
            startInfo.Environment[key] = value;
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start external script process for event {EventType}", eventData.EventType);
                return;
            }

            using var cts = CreateTimeoutToken(config.ScriptTimeoutSeconds);
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            try
            {
                if (cts is not null)
                {
                    await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                TryTerminateProcess(process);
                await process.WaitForExitAsync().ConfigureAwait(false);
                await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);
                _logger.LogError(
                    "Script execution timed out after {TimeoutSeconds} seconds for event {EventType}",
                    config.ScriptTimeoutSeconds,
                    eventData.EventType);
                return;
            }

            var output = await stdOutTask.ConfigureAwait(false);
            var error = await stdErrTask.ConfigureAwait(false);

            LogScriptResults(eventData, process.ExitCode, output, error);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Script execution was cancelled for event {EventType}", eventData.EventType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while executing script for event {EventType}", eventData.EventType);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start script process for event {EventType}", eventData.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while executing script for event {EventType}", eventData.EventType);
        }
    }

    private List<string> BuildScriptArguments(PluginConfiguration config, EventData eventData)
    {
        var arguments = new List<string> { config.ScriptPath };

        // Add event type
        arguments.Add("--event-type");
        arguments.Add(eventData.EventType.ToString());

        // Add event data as JSON
        arguments.Add("--event-data");
        arguments.Add(JsonSerializer.Serialize(eventData, _jsonOptions));

        // Legacy arguments for backward compatibility with existing scripts
        if (eventData.EventType == EventType.PlaybackStart || eventData.EventType == EventType.PlaybackStop)
        {
            if (!string.IsNullOrEmpty(eventData.SeriesName))
            {
                arguments.Add("-mt");
                arguments.Add(eventData.SeriesName);
                if (eventData.SeasonNumber.HasValue)
                {
                    arguments.Add("-sn");
                    arguments.Add(eventData.SeasonNumber.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (eventData.EpisodeNumber.HasValue)
                {
                    arguments.Add("-en");
                    arguments.Add(eventData.EpisodeNumber.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (!string.IsNullOrEmpty(eventData.ItemName) && eventData.ItemType == "Movie")
            {
                arguments.Add("-mt");
                arguments.Add(eventData.ItemName);
            }
        }

        // Add additional arguments from configuration
        foreach (var extraArgument in TokenizeAdditionalArguments(config.AdditionalArguments))
        {
            arguments.Add(extraArgument);
        }

        return arguments;
    }

    private Dictionary<string, string> BuildEnvironmentVariables(PluginConfiguration config, EventData eventData)
    {
        var environmentVars = new Dictionary<string, string>();

        // Add legacy environment variables for backward compatibility
        if (!string.IsNullOrWhiteSpace(config.SonarrApiKey))
        {
            environmentVars["SONARR_APIKEY"] = config.SonarrApiKey;
        }

        if (!string.IsNullOrWhiteSpace(config.SonarrUrl))
        {
            environmentVars["SONARR_URL"] = config.SonarrUrl;
        }

        if (!string.IsNullOrWhiteSpace(config.RadarrApiKey))
        {
            environmentVars["RADARR_APIKEY"] = config.RadarrApiKey;
        }

        if (!string.IsNullOrWhiteSpace(config.RadarrUrl))
        {
            environmentVars["RADARR_URL"] = config.RadarrUrl;
        }

        // Add new event-specific environment variables
        environmentVars["JELLYPY_EVENT_TYPE"] = eventData.EventType.ToString();
        environmentVars["JELLYPY_TIMESTAMP"] = eventData.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

        if (eventData.ItemId.HasValue)
        {
            environmentVars["JELLYPY_ITEM_ID"] = eventData.ItemId.Value.ToString();
        }

        if (!string.IsNullOrEmpty(eventData.ItemName))
        {
            environmentVars["JELLYPY_ITEM_NAME"] = eventData.ItemName;
        }

        if (!string.IsNullOrEmpty(eventData.ItemType))
        {
            environmentVars["JELLYPY_ITEM_TYPE"] = eventData.ItemType;
        }

        if (eventData.UserId.HasValue)
        {
            environmentVars["JELLYPY_USER_ID"] = eventData.UserId.Value.ToString();
        }

        if (!string.IsNullOrEmpty(eventData.UserName))
        {
            environmentVars["JELLYPY_USER_NAME"] = eventData.UserName;
        }

        return environmentVars;
    }

    private void LogScriptResults(EventData eventData, int exitCode, string output, string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("Script stderr for event {EventType}: {Error}", eventData.EventType, error.Trim());
        }

        if (exitCode != 0)
        {
            _logger.LogWarning("Script exited with code {ExitCode} for event {EventType}", exitCode, eventData.EventType);
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            _logger.LogInformation("Script output for event {EventType}: {Output}", eventData.EventType, output.Trim());
        }
    }

    private static string ResolveWorkingDirectory(PluginConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.ScriptWorkingDirectory))
        {
            return config.ScriptWorkingDirectory;
        }

        var scriptDirectory = Path.GetDirectoryName(config.ScriptPath);
        return string.IsNullOrWhiteSpace(scriptDirectory) ? Directory.GetCurrentDirectory() : scriptDirectory;
    }

    private static CancellationTokenSource? CreateTimeoutToken(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            return null;
        }

        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    }

    private void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "Failed to terminate process after timeout (invalid operation).");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogDebug(ex, "Failed to terminate process after timeout (Win32 error).");
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This cleanup method should not swallow exceptions.
            _logger.LogDebug(ex, "Failed to terminate process after timeout.");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private static IEnumerable<string> TokenizeAdditionalArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            yield break;
        }

        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var character in arguments)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
            {
                current.Append(character);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing from user code, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _scriptSemaphore?.Dispose();
            _disposed = true;
        }
    }
}
