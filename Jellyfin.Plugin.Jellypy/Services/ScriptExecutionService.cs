using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        if (config.ScriptSettings?.Any() == true)
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

        if (!applicableSettings.Any())
        {
            _logger.LogDebug("No script settings configured for event type {EventType}", eventData.EventType);
            return;
        }

        var globalSettings = config.GlobalSettings ?? new GlobalScriptSettings();
        var semaphore = new SemaphoreSlim(globalSettings.MaxConcurrentExecutions, globalSettings.MaxConcurrentExecutions);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script setting {SettingId} for event {EventType}", setting.Id, eventData.EventType);
        }
    }

    private static string GetExecutorPath(ScriptExecutorType executorType, string customExecutorPath)
    {
        if (!string.IsNullOrEmpty(customExecutorPath))
        {
            return customExecutorPath;
        }

        return executorType switch
        {
            ScriptExecutorType.Python => "/usr/bin/python3", // Default paths - should be configurable
            ScriptExecutorType.PowerShell => "/usr/local/bin/pwsh",
            ScriptExecutorType.Bash => "/bin/bash",
            ScriptExecutorType.NodeJs => "/usr/bin/node",
            ScriptExecutorType.Binary => string.Empty, // Binary doesn't need an executor
            _ => string.Empty
        };
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script process for event {EventType}", eventType);
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
                _logger.LogError("Script execution timed out after {TimeoutSeconds} seconds for event {EventType}",
                    config.ScriptTimeoutSeconds, eventData.EventType);
                return;
            }

            var output = await stdOutTask.ConfigureAwait(false);
            var error = await stdErrTask.ConfigureAwait(false);

            LogScriptResults(eventData, process.ExitCode, output, error);
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
        arguments.Add(JsonSerializer.Serialize(eventData, new JsonSerializerOptions { WriteIndented = false }));

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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to terminate process after timeout.");
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
