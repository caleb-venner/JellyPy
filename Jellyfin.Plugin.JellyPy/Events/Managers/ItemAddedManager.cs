using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using Jellyfin.Plugin.JellyPy.Events.Models;
using Jellyfin.Plugin.JellyPy.Services.Notifications;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Managers;

/// <summary>
/// Manager for processing items added to the library.
/// Queues items and processes them with intelligent grouping for episodes.
/// Episodes from the same Series+Season are grouped together, with the delay
/// refreshing each time a new episode is added to the group.
/// </summary>
public class ItemAddedManager : IItemAddedManager, IHostedService
{
    private readonly ILogger<ItemAddedManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Queue for non-episode items (movies, series, seasons, etc.).
    /// These are processed with a simple delay.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, QueuedItem> _nonEpisodeQueue = new();

    /// <summary>
    /// Queue for episode groups, keyed by Series+Season.
    /// Each group tracks its own activity timestamp for intelligent grouping.
    /// </summary>
    private readonly ConcurrentDictionary<SeriesSeasonKey, EpisodeGroupQueue> _episodeGroupQueues = new();

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAddedManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="scopeFactory">Service scope factory for resolving handlers.</param>
    /// <param name="libraryManager">Library manager for re-fetching items from database.</param>
    public ItemAddedManager(ILogger<ItemAddedManager> logger, IServiceScopeFactory scopeFactory, ILibraryManager libraryManager)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc/>
    public void QueueItemAdded(BaseItem item)
    {
        if (item.IsVirtualItem)
        {
            return; // Skip virtual items
        }

        var config = Plugin.Instance.Configuration;

        if (config.EnableItemGrouping && item is Episode episode)
        {
            QueueEpisode(episode);
        }
        else
        {
            // Non-episodes or grouping disabled - use simple queue
            _nonEpisodeQueue.TryAdd(item.Id, new QueuedItem(item.Id, DateTime.UtcNow));
            _logger.LogVerbose("Queued item for processing: {ItemName} ({ItemId})", item.Name, item.Id);
        }
    }

    private void QueueEpisode(Episode episode)
    {
        // Try to get series metadata - may not be available yet
        var seriesId = episode.SeriesId;
        var seasonNumber = episode.ParentIndexNumber;
        var seriesName = episode.Series?.Name ?? episode.SeriesName ?? "Unknown Series";

        // Create a key for this Series+Season combination
        var key = new SeriesSeasonKey(seriesId, seasonNumber);

        var groupQueue = _episodeGroupQueues.GetOrAdd(key, _ => new EpisodeGroupQueue(seriesId, seriesName, seasonNumber));

        // Add episode to the group and refresh activity timestamp
        var wasAdded = groupQueue.AddEpisode(episode.Id);

        if (wasAdded)
        {
            _logger.LogVerbose(
                "Queued episode for grouping: {EpisodeName} ({EpisodeId}) -> {SeriesName} Season {SeasonNumber} (Group count: {Count})",
                episode.Name,
                episode.Id,
                seriesName,
                seasonNumber ?? 0,
                groupQueue.EpisodeCount);

            // Check if we should send "queueing started" notification
            _ = CheckAndSendQueueingStartedAsync(groupQueue);
        }
    }

    private async Task CheckAndSendQueueingStartedAsync(EpisodeGroupQueue groupQueue)
    {
        // Send "queueing started" notification when we reach 2+ episodes for the first time
        if (groupQueue.EpisodeCount >= 2 && !groupQueue.QueueingNotificationSent)
        {
            groupQueue.QueueingNotificationSent = true;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ntfyService = scope.ServiceProvider.GetRequiredService<INtfyService>();

                // Try to get valid SeriesId for image
                Guid? imageSeriesId = groupQueue.SeriesId != Guid.Empty ? groupQueue.SeriesId : null;

                // If we don't have a SeriesId yet, try to get it from one of the episodes
                if (!imageSeriesId.HasValue)
                {
                    foreach (var episodeId in groupQueue.EpisodeIds.ToList())
                    {
                        var ep = _libraryManager.GetItemById(episodeId) as Episode;
                        if (ep?.SeriesId != Guid.Empty)
                        {
                            imageSeriesId = ep.SeriesId;
                            groupQueue.SeriesId = ep.SeriesId; // Update the cached value
                            break;
                        }
                    }
                }

                await ntfyService.SendQueueingStartedNotificationAsync(
                    groupQueue.SeriesName,
                    groupQueue.SeasonNumber,
                    groupQueue.EpisodeCount,
                    imageSeriesId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queueing started notification for {SeriesName}", groupQueue.SeriesName);
            }
        }
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = ProcessQueuedItemsAsync(_cancellationTokenSource.Token);
        _logger.LogInformation("ItemAddedManager started");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource?.CancelAsync()!;
        if (_processingTask != null)
        {
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
        }

        _cancellationTokenSource?.Dispose();
        _logger.LogInformation("ItemAddedManager stopped");
    }

    private async Task ProcessQueuedItemsAsync(CancellationToken cancellationToken)
    {
        const int checkIntervalMs = 500; // Check queues every 500ms

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(checkIntervalMs, cancellationToken).ConfigureAwait(false);

                var config = Plugin.Instance.Configuration;
                var groupingDelaySeconds = config.EnableItemGrouping ? config.ItemGroupingDelaySeconds : 2;

                // Process expired episode groups
                await ProcessExpiredEpisodeGroupsAsync(groupingDelaySeconds, cancellationToken).ConfigureAwait(false);

                // Process non-episode items
                await ProcessNonEpisodeItemsAsync(groupingDelaySeconds, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued items");
            }
        }
    }

    private async Task ProcessExpiredEpisodeGroupsAsync(int groupingDelaySeconds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiredGroups = _episodeGroupQueues
            .Where(kvp => (now - kvp.Value.LastActivity).TotalSeconds >= groupingDelaySeconds)
            .ToList();

        foreach (var (key, groupQueue) in expiredGroups)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Remove from queue first to prevent duplicate processing
            if (!_episodeGroupQueues.TryRemove(key, out _))
            {
                continue;
            }

            _logger.LogVerbose(
                "Processing expired episode group: {SeriesName} Season {SeasonNumber} ({Count} episodes, inactive for {Seconds:F1}s)",
                groupQueue.SeriesName,
                groupQueue.SeasonNumber ?? 0,
                groupQueue.EpisodeCount,
                (now - groupQueue.LastActivity).TotalSeconds);

            await ProcessEpisodeGroupAsync(groupQueue, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessEpisodeGroupAsync(EpisodeGroupQueue groupQueue, CancellationToken cancellationToken)
    {
        var episodes = new List<Episode>();
        var validSeriesId = Guid.Empty;
        Series? series = null;
        const int MaxRetries = 5;

        foreach (var episodeId in groupQueue.EpisodeIds)
        {
            // Re-fetch episode from database to get updated metadata
            var episode = _libraryManager.GetItemById(episodeId) as Episode;
            if (episode == null)
            {
                _logger.LogWarning("Episode {EpisodeId} no longer exists in library. Skipping.", episodeId);
                continue;
            }

            // Check if episode has valid series metadata
            if (episode.SeriesId == Guid.Empty)
            {
                // Check retry count
                var retryCount = groupQueue.GetRetryCount(episodeId);
                if (retryCount < MaxRetries)
                {
                    groupQueue.IncrementRetryCount(episodeId);
                    _logger.LogVerbose(
                        "Episode {EpisodeName} ({EpisodeId}) has no series ID yet (attempt {RetryCount}/{MaxRetries}). Re-queueing.",
                        episode.Name,
                        episodeId,
                        retryCount + 1,
                        MaxRetries);

                    // Re-add to queue for retry
                    var newKey = new SeriesSeasonKey(Guid.Empty, episode.ParentIndexNumber);
                    var newQueue = _episodeGroupQueues.GetOrAdd(newKey, _ => new EpisodeGroupQueue(
                        Guid.Empty,
                        episode.Series?.Name ?? episode.SeriesName ?? "Unknown Series",
                        episode.ParentIndexNumber));
                    newQueue.AddEpisode(episodeId, retryCount + 1);
                    continue;
                }
                else
                {
                    _logger.LogWarning(
                        "Episode {EpisodeName} ({EpisodeId}) still has no series ID after {RetryCount} retries. Processing individually.",
                        episode.Name,
                        episodeId,
                        retryCount);

                    // Process as individual item
                    await ProcessIndividualItemAsync(episode, cancellationToken).ConfigureAwait(false);
                    continue;
                }
            }

            // Track valid series ID and cache series
            if (validSeriesId == Guid.Empty)
            {
                validSeriesId = episode.SeriesId;
                series = episode.Series ?? _libraryManager.GetItemById(episode.SeriesId) as Series;
            }

            episodes.Add(episode);
        }

        if (episodes.Count == 0)
        {
            return;
        }

        if (episodes.Count == 1)
        {
            // Single episode - process individually
            await ProcessIndividualItemAsync(episodes[0], cancellationToken).ConfigureAwait(false);
            return;
        }

        // Sort episodes by season then episode number
        var sortedEpisodes = episodes
            .OrderBy(e => e.ParentIndexNumber ?? 0)
            .ThenBy(e => e.IndexNumber ?? 0)
            .ToList();

        // Create episode collection
        var episodeCollection = new Collection<Episode>();
        foreach (var episode in sortedEpisodes)
        {
            episodeCollection.Add(episode);
        }

        var group = new SeriesEpisodeGroup(series, episodeCollection, DateTime.UtcNow);

        _logger.LogInformation(
            "Processing series group: {SeriesName} with {EpisodeCount} episodes ({EpisodeRange})",
            group.SeriesName,
            group.EpisodeCount,
            group.GetEpisodeRange());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<SeriesEpisodesAddedHandler>();
            await handler.HandleAsync(group).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing series episode group: {SeriesName}",
                group.SeriesName);
        }
    }

    private async Task ProcessNonEpisodeItemsAsync(int groupingDelaySeconds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var (itemId, queuedItem) in _nonEpisodeQueue.ToList())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Check if item has been in queue long enough
            if ((now - queuedItem.EnqueuedAt).TotalSeconds < groupingDelaySeconds)
            {
                continue;
            }

            // Remove from queue
            if (!_nonEpisodeQueue.TryRemove(itemId, out _))
            {
                continue;
            }

            // Re-fetch item from database
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                _logger.LogWarning("Item {ItemId} no longer exists in library. Skipping.", itemId);
                continue;
            }

            await ProcessIndividualItemAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessIndividualItemAsync(BaseItem item, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ItemAddedHandler>();
            await handler.HandleAsync(item).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing individual item: {ItemName} ({ItemId})",
                item.Name,
                item.Id);
        }
    }

    /// <summary>
    /// Composite key for grouping episodes by Series and Season.
    /// </summary>
    private readonly struct SeriesSeasonKey : IEquatable<SeriesSeasonKey>
    {
        public SeriesSeasonKey(Guid seriesId, int? seasonNumber)
        {
            SeriesId = seriesId;
            SeasonNumber = seasonNumber ?? -1; // Use -1 for unknown season
        }

        public Guid SeriesId { get; }

        public int SeasonNumber { get; }

        public override bool Equals(object? obj) => obj is SeriesSeasonKey other && Equals(other);

        public bool Equals(SeriesSeasonKey other) =>
            SeriesId.Equals(other.SeriesId) && SeasonNumber == other.SeasonNumber;

        public override int GetHashCode() => HashCode.Combine(SeriesId, SeasonNumber);
    }

    /// <summary>
    /// Container for a queued non-episode item.
    /// </summary>
    private sealed class QueuedItem
    {
        public QueuedItem(Guid itemId, DateTime enqueuedAt)
        {
            ItemId = itemId;
            EnqueuedAt = enqueuedAt;
        }

        public Guid ItemId { get; }

        public DateTime EnqueuedAt { get; }
    }

    /// <summary>
    /// Queue for episodes belonging to the same Series+Season.
    /// Tracks activity timestamp for intelligent grouping delay.
    /// </summary>
    private sealed class EpisodeGroupQueue
    {
        private readonly object _lock = new();
        private readonly HashSet<Guid> _episodeIds = new();
        private readonly Dictionary<Guid, int> _retryCounts = new();

        public EpisodeGroupQueue(Guid seriesId, string seriesName, int? seasonNumber)
        {
            SeriesId = seriesId;
            SeriesName = seriesName;
            SeasonNumber = seasonNumber;
            LastActivity = DateTime.UtcNow;
        }

        public Guid SeriesId { get; set; }

        public string SeriesName { get; }

        public int? SeasonNumber { get; }

        public DateTime LastActivity { get; private set; }

        public bool QueueingNotificationSent { get; set; }

        public int EpisodeCount
        {
            get
            {
                lock (_lock)
                {
                    return _episodeIds.Count;
                }
            }
        }

        public IReadOnlyCollection<Guid> EpisodeIds
        {
            get
            {
                lock (_lock)
                {
                    return _episodeIds.ToList();
                }
            }
        }

        public bool AddEpisode(Guid episodeId, int retryCount = 0)
        {
            lock (_lock)
            {
                if (_episodeIds.Add(episodeId))
                {
                    LastActivity = DateTime.UtcNow;
                    if (retryCount > 0)
                    {
                        _retryCounts[episodeId] = retryCount;
                    }

                    return true;
                }

                // Episode already in queue, but still refresh activity
                LastActivity = DateTime.UtcNow;
                return false;
            }
        }

        public int GetRetryCount(Guid episodeId)
        {
            lock (_lock)
            {
                return _retryCounts.TryGetValue(episodeId, out var count) ? count : 0;
            }
        }

        public void IncrementRetryCount(Guid episodeId)
        {
            lock (_lock)
            {
                _retryCounts[episodeId] = GetRetryCount(episodeId) + 1;
            }
        }
    }
}
