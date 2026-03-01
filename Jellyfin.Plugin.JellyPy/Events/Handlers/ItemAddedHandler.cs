using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Models;
using Jellyfin.Plugin.JellyPy.Services.Notifications;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Handlers;

/// <summary>
/// Handler for item added to library events.
/// </summary>
public class ItemAddedHandler : IEventProcessor<BaseItem>
{
    private readonly ILogger<ItemAddedHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;
    private readonly INtfyService _ntfyService;

    // Deduplication cache to prevent duplicate notifications when items are deleted and re-added
    // (e.g., when upgrading to better quality). Uses 30-minute window.
    private static readonly EventDeduplicationCache DeduplicationCache = new(TimeSpan.FromMinutes(30));

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAddedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    /// <param name="ntfyService">The ntfy notification service.</param>
    public ItemAddedHandler(
        ILogger<ItemAddedHandler> logger,
        IScriptExecutionService scriptExecutionService,
        INtfyService ntfyService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
        _ntfyService = ntfyService;
    }

    /// <inheritdoc />
    public EventType EventType => EventType.ItemAdded;

    /// <inheritdoc />
    public bool CanHandle(BaseItem eventArgs)
    {
        if (eventArgs == null)
        {
            return false;
        }

        // Filter out non-media items (Person, Studio, Genre, etc.)
        // Only process actual media content
        return eventArgs is Episode
            || eventArgs is Movie
            || eventArgs is Season
            || eventArgs is Series;
    }

    /// <inheritdoc />
    public EventData ExtractEventData(BaseItem item)
    {
        var eventData = new EventData
        {
            EventType = EventType.ItemAdded,
            Timestamp = DateTime.UtcNow,
            ItemId = item.Id,
            ItemName = item.Name,
            ItemType = item.GetType().Name,
            ItemPath = item.Path
        };

        // Add media-specific information
        if (item is Episode episode)
        {
            eventData.SeriesName = episode.Series?.Name ?? episode.SeriesName;
            eventData.SeasonNumber = episode.ParentIndexNumber;
            eventData.EpisodeNumber = episode.IndexNumber;
            eventData.AdditionalData["EpisodeName"] = episode.Name;

            // Only store SeriesId if it's valid (not empty)
            if (episode.SeriesId != Guid.Empty)
            {
                eventData.AdditionalData["SeriesId"] = episode.SeriesId;
            }
        }
        else if (item is Movie movie)
        {
            eventData.Year = movie.ProductionYear;
            eventData.AdditionalData["MovieId"] = movie.Id;
        }
        else if (item is Season season)
        {
            eventData.SeriesName = season.Series?.Name;
            eventData.SeasonNumber = season.IndexNumber;
        }
        else if (item is Series series)
        {
            eventData.SeriesName = series.Name;
        }

        // Add common media information
        var genres = item.Genres?.ToList() ?? new List<string>();
        foreach (var genre in genres)
        {
            eventData.Genres.Add(genre);
        }

        eventData.ContentRating = item.OfficialRating;
        eventData.AdditionalData["CommunityRating"] = item.CommunityRating;
        eventData.AdditionalData["Runtime"] = item.RunTimeTicks;
        eventData.AdditionalData["DateCreated"] = item.DateCreated;

        return eventData;
    }

    /// <inheritdoc />
    public async Task HandleAsync(BaseItem item)
    {
        try
        {
            if (!CanHandle(item))
            {
                _logger.LogVerbose("ItemAdded event cannot be handled - no item provided");
                return;
            }

            var eventData = ExtractEventData(item);

            _logger.LogVerbose("Processing ItemAdded event for item {ItemName} ({ItemId})", item.Name, item.Id);

            // Create deduplication key based on item type and identifier
            string? deduplicationKey = null;
            if (item is Episode episode)
            {
                // For episodes, use series name + season + episode number
                deduplicationKey = $"{eventData.SeriesName}:S{episode.ParentIndexNumber?.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) ?? "XX"}E{episode.IndexNumber?.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) ?? "XX"}";
            }
            else if (item is Movie movie)
            {
                // For movies, use title + year
                deduplicationKey = $"{movie.Name}:{movie.ProductionYear ?? 0}";
            }
            else if (item is Season season)
            {
                // For seasons, use series name + season number
                deduplicationKey = $"{eventData.SeriesName}:Season{season.IndexNumber ?? 0}";
            }
            else if (item is Series series)
            {
                // For series, use series name
                deduplicationKey = $"{series.Name}:Series";
            }

            // Check for duplicates if we have a deduplication key
            if (deduplicationKey != null && !DeduplicationCache.ShouldProcessEvent(deduplicationKey))
            {
                _logger.LogInformation(
                    "Skipping duplicate notification for {ItemName} (recently processed)",
                    item.Name);
                return;
            }

            // Execute custom scripts (if configured)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);

            // Send ntfy notification (if configured)
            await _ntfyService.SendItemAddedNotificationAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ItemAdded event");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }
}
