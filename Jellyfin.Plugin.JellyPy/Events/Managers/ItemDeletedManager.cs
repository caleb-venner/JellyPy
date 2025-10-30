using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Managers;

/// <summary>
/// Manager for processing items deleted from the library.
/// Queues items and processes them periodically to batch handle rapid deletions.
/// </summary>
public class ItemDeletedManager : IItemDeletedManager, IHostedService
{
    private readonly ILogger<ItemDeletedManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, QueuedItem> _itemQueue = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemDeletedManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="scopeFactory">Service scope factory for resolving handlers.</param>
    public ItemDeletedManager(ILogger<ItemDeletedManager> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc/>
    public void QueueItemDeleted(BaseItem item)
    {
        if (item.IsVirtualItem)
        {
            return; // Skip virtual items
        }

        _itemQueue.TryAdd(item.Id, new QueuedItem(item, DateTime.UtcNow));
        _logger.LogDebug("Queued item for deletion processing: {ItemName} ({ItemId})", item.Name, item.Id);
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = ProcessQueuedItemsAsync(_cancellationTokenSource.Token);
        _logger.LogInformation("ItemDeletedManager started");
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
        _logger.LogInformation("ItemDeletedManager stopped");
    }

    private async Task ProcessQueuedItemsAsync(CancellationToken cancellationToken)
    {
        const int processingDelayMs = 2000; // Wait 2 seconds for bulk deletions to complete
        const int maxBatchSize = 50; // Process max 50 items per cycle

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
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
            catch (OperationCanceledException)
            {
                break;
            }

            // Generic catch intentional: ProcessBatchAsync can throw InvalidOperationException
            // from GetRequiredService, plus any exception from handler implementation
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued items");
            }
        }
    }

    private async Task ProcessBatchAsync(List<QueuedItem> items, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing batch of {ItemCount} deleted items", items.Count);

        foreach (var queuedItem in items)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ItemDeletedHandler>();
                await handler.HandleAsync(queuedItem.Item).ConfigureAwait(false);
            }

            // Generic catch intentional: GetRequiredService can throw InvalidOperationException,
            // and handler execution may throw any exception
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing deleted item: {ItemName} ({ItemId})", queuedItem.Item.Name, queuedItem.Item.Id);
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
