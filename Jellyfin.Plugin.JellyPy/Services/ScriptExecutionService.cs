#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Events.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services;

/// <summary>
/// Service responsible for executing scripts based on events.
/// </summary>
public class ScriptExecutionService : IScriptExecutionService, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };
    private readonly ILogger<ScriptExecutionService> _logger;
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly DataAttributeProcessor _dataAttributeProcessor;
    private readonly IPluginConfigurationProvider _configProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptExecutionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="conditionEvaluator">The condition evaluator service.</param>
    /// <param name="dataAttributeProcessor">The data attribute processor service.</param>
    /// <param name="configProvider">The configuration provider.</param>
    public ScriptExecutionService(
        ILogger<ScriptExecutionService> logger,
        ConditionEvaluator conditionEvaluator,
        DataAttributeProcessor dataAttributeProcessor,
        IPluginConfigurationProvider configProvider)
    {
        _logger = logger;
        _conditionEvaluator = conditionEvaluator;
        _dataAttributeProcessor = dataAttributeProcessor;
        _configProvider = configProvider;
    }

    /// <inheritdoc />
    public async Task ExecuteScriptsAsync(EventData eventData)
    {
        var config = _configProvider.GetConfiguration();

        // Execute enhanced script settings if available
        if (config.ScriptSettings?.Count > 0)
        {
            await ExecuteEnhancedScriptsAsync(config, eventData).ConfigureAwait(false);
        }
    }

    private async Task ExecuteEnhancedScriptsAsync(PluginConfiguration config, EventData eventData)
    {
        var applicableSettings = config.ScriptSettings
            .Where(setting => setting.Enabled)
            .Where(setting => setting.Triggers.Contains(eventData.EventType))
            .OrderBy(setting => setting.Priority)
            .ToList();

        if (applicableSettings.Count == 0)
        {
            _logger.LogDebug("No script settings configured for event type {EventType}", eventData.EventType);
            return;
        }

        var globalSettings = config.GlobalSettings ?? new GlobalScriptSettings();
        using var throttler = new SemaphoreSlim(globalSettings.MaxConcurrentExecutions, globalSettings.MaxConcurrentExecutions);

        var tasks = applicableSettings.Select(setting => ExecuteScriptSettingWithThrottleAsync(setting, eventData, globalSettings, throttler));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteScriptSettingWithThrottleAsync(ScriptSetting setting, EventData eventData, GlobalScriptSettings globalSettings, SemaphoreSlim throttler)
    {
        await throttler.WaitAsync().ConfigureAwait(false);
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
            throttler.Release();
        }
    }

    private async Task ExecuteScriptSettingAsync(ScriptSetting setting, EventData eventData, GlobalScriptSettings globalSettings)
    {
        try
        {
            var arguments = new List<string>();
            var environmentVariables = new Dictionary<string, string>();

            var useJsonPayload = setting.ExecutionMode == ExecutionMode.JsonPayload || setting.DataAttributes.Count == 0;

            if (!useJsonPayload)
            {
                (arguments, environmentVariables) = _dataAttributeProcessor.ProcessDataAttributes(setting.DataAttributes, eventData);
            }

            var scriptPath = ResolveScriptPath(setting.Execution.ScriptName, globalSettings.CustomScriptsDirectory);
            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                _logger.LogError("Script path invalid or missing for setting {SettingId}: {ScriptPath}", setting.Id, scriptPath);
                return;
            }

            var executorPath = GetExecutorPath(setting.Execution.ExecutorType, setting.Execution.ExecutablePath, globalSettings);
            if (string.IsNullOrEmpty(executorPath))
            {
                _logger.LogError("Executor path not configured for script setting {SettingId}", setting.Id);
                return;
            }

            var timeoutSeconds = setting.Execution.TimeoutSeconds > 0
                ? setting.Execution.TimeoutSeconds
                : globalSettings.DefaultTimeoutSeconds;

            var startInfo = new ProcessStartInfo
            {
                FileName = executorPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? PluginConfiguration.ScriptsDirectory
            };

            // Add script path as first argument
            startInfo.ArgumentList.Add(scriptPath);

            if (useJsonPayload)
            {
                // Add EventData as JSON argument
                var eventDataJson = JsonSerializer.Serialize(eventData, _jsonOptions);
                startInfo.ArgumentList.Add(eventDataJson);
            }
            else
            {
                // Add processed arguments
                foreach (var argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
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

            await ExecuteProcessAsync(startInfo, timeoutSeconds, eventData.EventType).ConfigureAwait(false);
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
            _logger.LogError(ex, "Unexpected error executing script setting {SettingId} for event {EventType}", setting.Id, eventData.EventType);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private string GetExecutorPath(ScriptExecutorType executorType, string customExecutorPath, GlobalScriptSettings globalSettings)
    {
        // Global overrides take precedence when provided
        if (!string.IsNullOrWhiteSpace(globalSettings.PythonExecutablePath) && executorType == ScriptExecutorType.Python)
        {
            return globalSettings.PythonExecutablePath;
        }

        if (!string.IsNullOrWhiteSpace(globalSettings.BashExecutablePath) && executorType == ScriptExecutorType.Bash)
        {
            return globalSettings.BashExecutablePath;
        }

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
            _ => customExecutorPath
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

    private string ResolveScriptPath(string scriptName, string customScriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(scriptName))
        {
            return string.Empty;
        }

        var baseDirectory = string.IsNullOrWhiteSpace(customScriptsDirectory)
            ? PluginConfiguration.ScriptsDirectory
            : customScriptsDirectory;

        var resolvedPath = Path.IsPathRooted(scriptName)
            ? scriptName
            : Path.Join(baseDirectory, scriptName);

        var fullPath = Path.GetFullPath(resolvedPath);
        var basePath = Path.GetFullPath(baseDirectory);

        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Script path traversal detected: {ScriptName} resolves outside {BaseDirectory}", scriptName, baseDirectory);
            return string.Empty;
        }

        return fullPath;
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
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = path,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
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
                    process.Kill(true);
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
            using var whichProcess = new Process();
            whichProcess.StartInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = executableName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
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
                    whichProcess.Kill(true);
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

        // Fallback: try direct execution
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executableName,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
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
                    process.Kill(true);
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

            _logger.LogInformation("Operating System: {OS}", RuntimeInformation.OSDescription);
            _logger.LogInformation("Architecture: {Arch}", RuntimeInformation.OSArchitecture);

            var directories = new[] { "/usr/bin", "/usr/local/bin", "/bin" };
            foreach (var dir in directories)
            {
                if (Directory.Exists(dir))
                {
                    var pythonFiles = Directory.GetFiles(dir, "python*")
                        .Where(f => !Path.GetFileName(f).Contains("config", StringComparison.OrdinalIgnoreCase))
                        .Take(LoggingConstants.MaxErrorLinesToLog)
                        .ToArray();

                    if (pythonFiles.Length > 0)
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

            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                var paths = pathVar.Split(':', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("PATH contains {Count} directories", paths.Length);
                _logger.LogDebug("PATH directories: {Paths}", string.Join(", ", paths.Take(LoggingConstants.MaxPathDirectoriesToLog)));
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
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to kill timed-out process.");
                    throw; // Rethrow to maintain CA1031 compliance while still logging the error
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
            _logger.LogError(ex, "Unexpected error executing script process for event {EventType}", eventType);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
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
            _disposed = true;
        }
    }

    private static CancellationTokenSource? CreateTimeoutToken(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            return null;
        }

        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
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
}
