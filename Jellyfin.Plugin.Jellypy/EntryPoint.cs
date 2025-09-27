using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy;

/// <summary>
/// Hosted service responsible for wiring playback events to the script runner.
/// </summary>
public class EntryPoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager used to subscribe to playback events.</param>
    /// <param name="scopeFactory">Scope factory used to resolve scoped services.</param>
    /// <param name="logger">Logger instance.</param>
    public EntryPoint(ISessionManager sessionManager, IServiceScopeFactory scopeFactory, ILogger<EntryPoint> logger)
    {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object sender, PlaybackProgressEventArgs eventArgs)
    {
        _ = HandlePlaybackStartAsync(eventArgs);
    }

    private async Task HandlePlaybackStartAsync(PlaybackProgressEventArgs eventArgs)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var script = scope.ServiceProvider.GetRequiredService<ExecuteScript>();
            await script.RunScript(eventArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            string itemName;
            if (eventArgs.Item is Episode episode)
            {
                itemName = episode.SeriesName ?? episode.Name ?? "<unknown>";
            }
            else
            {
                itemName = eventArgs.Item?.Name ?? "<unknown>";
            }
            _logger.LogError(ex, "Failed to execute script for playback start of {ItemName}", itemName);
        }
    }
}
