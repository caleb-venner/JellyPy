using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Models;
using MediaBrowser.Controller.Entities;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAddedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    public ItemAddedHandler(
        ILogger<ItemAddedHandler> logger,
        IScriptExecutionService scriptExecutionService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
    }

    /// <inheritdoc />
    public EventType EventType => EventType.ItemAdded;

    /// <inheritdoc />
    public bool CanHandle(BaseItem eventArgs)
    {
        return eventArgs != null;
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
            eventData.AdditionalData["SeriesId"] = episode.SeriesId;
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
                _logger.LogDebug("ItemAdded event cannot be handled - no item provided");
                return;
            }

            var eventData = ExtractEventData(item);

            _logger.LogDebug("Processing ItemAdded event for item {ItemName} ({ItemId})", item.Name, item.Id);

            // Execute custom scripts (if configured)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ItemAdded event");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }
}
