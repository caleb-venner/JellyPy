using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using Jellyfin.Plugin.JellyPy.Events.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Managers;

/// <summary>
/// Manager for processing items added to the library.
/// Queues items and processes them periodically to batch handle rapid additions.
/// </summary>
public class ItemAddedManager : IItemAddedManager, IHostedService
{
    private readonly ILogger<ItemAddedManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, QueuedItem> _itemQueue = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAddedManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="scopeFactory">Service scope factory for resolving handlers.</param>
    public ItemAddedManager(ILogger<ItemAddedManager> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc/>
    public void QueueItemAdded(BaseItem item)
    {
        if (item.IsVirtualItem)
        {
            return; // Skip virtual items
        }

        _itemQueue.TryAdd(item.Id, new QueuedItem(item, DateTime.UtcNow));
        _logger.LogDebug("Queued item for processing: {ItemName} ({ItemId})", item.Name, item.Id);
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
        var config = Plugin.Instance.Configuration;
        var processingDelayMs = config.ItemGroupingDelaySeconds * 1000;
        const int maxBatchSize = 50; // Process max 50 items per cycle

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (config.EnableItemGrouping)
                {
                    // Batch processing logic (grouping enabled)
                    await Task.Delay(processingDelayMs, cancellationToken).ConfigureAwait(false);

                    if (_itemQueue.IsEmpty)
                    {
                        continue;
                    }

                    var itemsToProcess = new List<QueuedItem>();
                    while (itemsToProcess.Count < maxBatchSize && _itemQueue.TryRemove(_itemQueue.Keys.First(), out var queuedItem))
                    {
                        itemsToProcess.Add(queuedItem);
                    }

                    if (itemsToProcess.Count > 0)
                    {
                        await ProcessBatchAsync(itemsToProcess, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Immediate processing logic (grouping disabled)
                    if (_itemQueue.TryRemove(_itemQueue.Keys.First(), out var queuedItem))
                    {
                        await ProcessIndividualItemAsync(queuedItem.Item, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // If queue is empty, wait a bit before checking again
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
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

    private async Task ProcessBatchAsync(List<QueuedItem> items, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing batch of {ItemCount} queued items", items.Count);

        // Group episodes by series and process as SeriesEpisodeGroup
        var episodesBySeriesId = new Dictionary<Guid, List<Episode>>();
        var seriesCache = new Dictionary<Guid, Series>();
        var nonEpisodeItems = new List<QueuedItem>();

        // Separate episodes from other items
        foreach (var queuedItem in items)
        {
            if (queuedItem.Item is Episode episode)
            {
                var seriesId = episode.SeriesId;
                if (seriesId == Guid.Empty)
                {
                    _logger.LogWarning("Episode {EpisodeName} has no series ID, processing as individual item", episode.Name);
                    nonEpisodeItems.Add(queuedItem);
                    continue;
                }

                if (!episodesBySeriesId.TryGetValue(seriesId, out var existingEpisodes))
                {
                    existingEpisodes = new List<Episode>();
                    episodesBySeriesId[seriesId] = existingEpisodes;
                    seriesCache[seriesId] = episode.Series;
                }

                existingEpisodes.Add(episode);
            }
            else
            {
                nonEpisodeItems.Add(queuedItem);
            }
        }

        // Process episode groups (with 2+ episodes from same series)
        foreach (var (seriesId, episodes) in episodesBySeriesId)
        {
            if (episodes.Count >= 2)
            {
                // Sort episodes by season then episode number
                var sortedEpisodes = episodes
                    .OrderBy(e => e.ParentIndexNumber ?? 0)
                    .ThenBy(e => e.IndexNumber ?? 0)
                    .ToList();

                var series = seriesCache.TryGetValue(seriesId, out var s) ? s : null;

                // Convert list to Collection for SeriesEpisodeGroup
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

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<SeriesEpisodesAddedHandler>();
                    await handler.HandleAsync(group).ConfigureAwait(false);
                }

                // Generic catch intentional: GetRequiredService can throw InvalidOperationException,
                // and handler execution may throw any exception
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing series episode group: {SeriesName} ({SeriesId})",
                        group.SeriesName,
                        seriesId);
                }
            }
            else
            {
                // Single episode from series - process as individual item
                foreach (var episode in episodes)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<ItemAddedHandler>();
                        await handler.HandleAsync(episode).ConfigureAwait(false);
                    }

                    // Generic catch intentional: GetRequiredService can throw InvalidOperationException,
                    // and handler execution may throw any exception
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error processing queued item: {ItemName} ({ItemId})",
                            episode.Name,
                            episode.Id);
                    }
                }
            }
        }

        // Process non-episode items (movies, seasons, series, etc.)
        foreach (var queuedItem in nonEpisodeItems)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ItemAddedHandler>();
                await handler.HandleAsync(queuedItem.Item).ConfigureAwait(false);
            }

            // Generic catch intentional: GetRequiredService can throw InvalidOperationException,
            // and handler execution may throw any exception
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing queued item: {ItemName} ({ItemId})",
                    queuedItem.Item.Name,
                    queuedItem.Item.Id);
            }
        }
    }

    /// <summary>
    /// Container for a queued item with timestamp.
    /// </summary>
    private sealed class QueuedItem(BaseItem item, DateTime enqueuedAt)
    {
        public BaseItem Item { get; } = item;

        public DateTime EnqueuedAt { get; } = enqueuedAt;
    }
}
