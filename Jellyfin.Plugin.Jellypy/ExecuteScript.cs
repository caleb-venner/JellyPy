using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellypy.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Jellyfin.Plugin.Jellypy;

/// <summary>
/// Service responsible for executing the configured external script on playback events.
/// </summary>
public class ExecuteScript : IEventConsumer<PlaybackProgressEventArgs>
{
    private readonly ILogger<ExecuteScript> _logger;
    private static readonly SemaphoreSlim ScriptSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteScript"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ExecuteScript(ILogger<ExecuteScript> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        await RunScript(eventArgs).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the configured script for the supplied playback event.
    /// </summary>
    /// <param name="eventArgs">Playback instance arguments.</param>
    /// <returns>The captured standard output from the script, if any.</returns>
    public async Task<string?> RunScript(PlaybackProgressEventArgs eventArgs)
    {
        if (eventArgs?.Item is null)
        {
            _logger.LogWarning("Playback event did not include a media item; skipping script execution.");
            return null;
        }

        var config = Plugin.Instance?.Configuration;
        if (config is null)
        {
            _logger.LogWarning("Plugin configuration is unavailable; skipping script execution.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.PythonExecutablePath) || !File.Exists(config.PythonExecutablePath))
        {
            _logger.LogWarning("Python executable path '{PythonExecutablePath}' is invalid; skipping script execution.", config.PythonExecutablePath);
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.ScriptPath) || !File.Exists(config.ScriptPath))
        {
            _logger.LogWarning("Script path '{ScriptPath}' is invalid; skipping script execution.", config.ScriptPath);
            return null;
        }

        var arguments = new List<string> { config.ScriptPath };
        string? mediaLabel = null;

        if (eventArgs.Item is Episode episode)
        {
            if (!config.EnableEpisodeProcessing)
            {
                _logger.LogDebug("Episode processing disabled; skipping script execution for {ItemName}.", episode.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(config.SonarrApiKey) || string.IsNullOrWhiteSpace(config.SonarrUrl))
            {
                _logger.LogWarning("Sonarr credentials are not configured; skipping episode processing for {ItemName}.", episode.Name);
                return null;
            }

            var seriesName = episode.Series?.Name ?? episode.SeriesName ?? episode.Name;
            var seasonNumber = episode.ParentIndexNumber;
            var episodeNumber = episode.IndexNumber;

            if (string.IsNullOrWhiteSpace(seriesName) || seasonNumber is null || episodeNumber is null)
            {
                _logger.LogWarning("Episode metadata incomplete for {ItemId}; cannot supply script arguments.", episode.Id);
                return null;
            }

            mediaLabel = seriesName;
            arguments.Add("-mt");
            arguments.Add(seriesName);
            arguments.Add("-sn");
            arguments.Add(seasonNumber.Value.ToString(CultureInfo.InvariantCulture));
            arguments.Add("-en");
            arguments.Add(episodeNumber.Value.ToString(CultureInfo.InvariantCulture));
        }
        else if (eventArgs.Item is Movie movie)
        {
            if (!config.EnableMovieProcessing)
            {
                _logger.LogDebug("Movie processing disabled; skipping script execution for {ItemName}.", movie.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(config.RadarrApiKey) || string.IsNullOrWhiteSpace(config.RadarrUrl))
            {
                _logger.LogWarning("Radarr credentials are not configured; skipping movie processing for {ItemName}.", movie.Name);
                return null;
            }

            mediaLabel = movie.Name;
            if (string.IsNullOrWhiteSpace(mediaLabel))
            {
                _logger.LogWarning("Movie metadata incomplete for {ItemId}; cannot supply script arguments.", movie.Id);
                return null;
            }

            arguments.Add("-mt");
            arguments.Add(mediaLabel);
        }
        else
        {
            _logger.LogDebug("Unsupported media type {ItemType}; script not invoked.", eventArgs.Item.GetType().Name);
            return null;
        }

        foreach (var extraArgument in TokenizeAdditionalArguments(config.AdditionalArguments))
        {
            arguments.Add(extraArgument);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = config.PythonExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = ResolveWorkingDirectory(config)
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(config.SonarrApiKey))
        {
            startInfo.Environment["SONARR_APIKEY"] = config.SonarrApiKey;
        }

        if (!string.IsNullOrWhiteSpace(config.SonarrUrl))
        {
            startInfo.Environment["SONARR_URL"] = config.SonarrUrl;
        }

        if (!string.IsNullOrWhiteSpace(config.RadarrApiKey))
        {
            startInfo.Environment["RADARR_APIKEY"] = config.RadarrApiKey;
        }

        if (!string.IsNullOrWhiteSpace(config.RadarrUrl))
        {
            startInfo.Environment["RADARR_URL"] = config.RadarrUrl;
        }

        await ScriptSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start external script process for media '{MediaLabel}'.", mediaLabel ?? eventArgs.Item.Name);
                return null;
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
                _logger.LogError("Script execution timed out after {TimeoutSeconds} seconds for media '{MediaLabel}'.", config.ScriptTimeoutSeconds, mediaLabel ?? eventArgs.Item.Name);
                return null;
            }

            var output = await stdOutTask.ConfigureAwait(false);
            var error = await stdErrTask.ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("Script stderr for '{MediaLabel}': {Error}", mediaLabel ?? eventArgs.Item.Name, error.Trim());
            }

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Script exited with code {ExitCode} for '{MediaLabel}'.", process.ExitCode, mediaLabel ?? eventArgs.Item.Name);
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.LogInformation("Script output for '{MediaLabel}': {Output}", mediaLabel ?? eventArgs.Item.Name, output.Trim());
            }

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while executing script for '{MediaLabel}'.", mediaLabel ?? eventArgs.Item.Name);
            return null;
        }
        finally
        {
            ScriptSemaphore.Release();
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
}
