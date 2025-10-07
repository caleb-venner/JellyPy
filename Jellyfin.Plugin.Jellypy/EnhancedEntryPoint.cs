using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellypy.Events;
using Jellyfin.Plugin.Jellypy.Events.Handlers;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy;

/// <summary>
/// Enhanced entry point that handles multiple Jellyfin event types.
/// </summary>
public class EnhancedEntryPoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnhancedEntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedEntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager used to subscribe to playback events.</param>
    /// <param name="scopeFactory">Scope factory used to resolve scoped services.</param>
    /// <param name="logger">Logger instance.</param>
    public EnhancedEntryPoint(ISessionManager sessionManager, IServiceScopeFactory scopeFactory, ILogger<EnhancedEntryPoint> logger)
    {
        _sessionManager = sessionManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _logger.LogInformation("Enhanced EntryPoint started - listening for Jellyfin events");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _logger.LogInformation("Enhanced EntryPoint stopped");
        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object sender, PlaybackProgressEventArgs eventArgs)
    {
        _ = HandlePlaybackStartEventAsync(eventArgs);
    }

    private void OnPlaybackStopped(object sender, PlaybackStopEventArgs eventArgs)
    {
        _ = HandlePlaybackStopEventAsync(eventArgs);
    }

    private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs eventArgs)
    {
        // We can use this for pause/resume detection by checking IsPaused
        HandlePlaybackProgressEventAsync(eventArgs);
    }

    private async Task HandlePlaybackStartEventAsync(PlaybackProgressEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackProgressEventArgs, PlaybackStartHandler>(eventArgs).ConfigureAwait(false);
    }

    private async Task HandlePlaybackStopEventAsync(PlaybackStopEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackStopEventArgs, PlaybackStopHandler>(eventArgs).ConfigureAwait(false);
    }

    private void HandlePlaybackProgressEventAsync(PlaybackProgressEventArgs eventArgs)
    {
        // Determine if this is a pause or resume based on IsPaused
        // For now, we'll just log it and potentially handle it later
        _logger.LogDebug(
            "Playback progress event: Paused={IsPaused}, Position={Position}",
            eventArgs.IsPaused,
            eventArgs.PlaybackPositionTicks);
    }

    private async Task HandleEventAsync<TEventArgs, THandler>(TEventArgs eventArgs)
        where THandler : class, IEventProcessor<TEventArgs>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync(eventArgs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle {EventType} event", typeof(TEventArgs).Name);
        }
    }
}
