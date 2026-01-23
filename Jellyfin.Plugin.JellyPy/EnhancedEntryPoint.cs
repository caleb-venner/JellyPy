using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using Jellyfin.Plugin.JellyPy.Events.Managers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy;

/// <summary>
/// Enhanced entry point that handles multiple Jellyfin event types.
/// </summary>
public class EnhancedEntryPoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IItemAddedManager _itemAddedManager;
    private readonly IItemDeletedManager _itemDeletedManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnhancedEntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedEntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager used to subscribe to playback events.</param>
    /// <param name="libraryManager">Library manager used to subscribe to library item events.</param>
    /// <param name="itemAddedManager">Manager for processing items added to the library.</param>
    /// <param name="itemDeletedManager">Manager for processing items deleted from the library.</param>
    /// <param name="scopeFactory">Scope factory used to resolve scoped services.</param>
    /// <param name="logger">Logger instance.</param>
    public EnhancedEntryPoint(
        ISessionManager sessionManager,
        ILibraryManager libraryManager,
        IItemAddedManager itemAddedManager,
        IItemDeletedManager itemDeletedManager,
        IServiceScopeFactory scopeFactory,
        ILogger<EnhancedEntryPoint> logger)
    {
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
        _itemAddedManager = itemAddedManager;
        _itemDeletedManager = itemDeletedManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Ensure scripts directory exists
        try
        {
            var scriptsDirectory = PluginConfiguration.ScriptsDirectory;
            if (!Directory.Exists(scriptsDirectory))
            {
                Directory.CreateDirectory(scriptsDirectory);
                _logger.LogInformation("Created scripts directory: {Directory}", scriptsDirectory);
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to create scripts directory");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Failed to create scripts directory");
        }

        // Wire playback events
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;

        // Wire library events
        _libraryManager.ItemAdded += OnLibraryItemAdded;
        _libraryManager.ItemRemoved += OnLibraryItemRemoved;

        _logger.LogInformation("Enhanced EntryPoint started - listening for Jellyfin events");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;

        _libraryManager.ItemAdded -= OnLibraryItemAdded;
        _libraryManager.ItemRemoved -= OnLibraryItemRemoved;

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

    private async Task HandlePlaybackStartEventAsync(PlaybackProgressEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackProgressEventArgs, PlaybackStartHandler>(eventArgs).ConfigureAwait(false);
    }

    private async Task HandlePlaybackStopEventAsync(PlaybackStopEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackStopEventArgs, PlaybackStopHandler>(eventArgs).ConfigureAwait(false);
    }

    private void OnPlaybackProgress(object sender, PlaybackProgressEventArgs eventArgs)
    {
        // Route to pause or resume handler based on IsPaused state
        _ = eventArgs.IsPaused
            ? HandlePlaybackPauseEventAsync(eventArgs)
            : HandlePlaybackResumeEventAsync(eventArgs);
    }

    private async Task HandlePlaybackPauseEventAsync(PlaybackProgressEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackProgressEventArgs, PlaybackPauseHandler>(eventArgs).ConfigureAwait(false);
    }

    private async Task HandlePlaybackResumeEventAsync(PlaybackProgressEventArgs eventArgs)
    {
        await HandleEventAsync<PlaybackProgressEventArgs, PlaybackResumeHandler>(eventArgs).ConfigureAwait(false);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to resolve handler for {EventType} event", typeof(TEventArgs).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling {EventType} event", typeof(TEventArgs).Name);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private void OnLibraryItemAdded(object sender, ItemChangeEventArgs eventArgs)
    {
        _itemAddedManager.QueueItemAdded(eventArgs.Item);
    }

    private void OnLibraryItemRemoved(object sender, ItemChangeEventArgs eventArgs)
    {
        _itemDeletedManager.QueueItemDeleted(eventArgs.Item);
    }
}
