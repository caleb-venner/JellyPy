using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Events.Handlers;

/// <summary>
/// Handler for playback stop events.
/// </summary>
public class PlaybackStopHandler : IGenericEventHandler<PlaybackStopEventArgs>
{
    private readonly ILogger<PlaybackStopHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStopHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    public PlaybackStopHandler(ILogger<PlaybackStopHandler> logger, IScriptExecutionService scriptExecutionService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
    }

    /// <inheritdoc />
    public EventType EventType => EventType.PlaybackStop;

    /// <inheritdoc />
    public bool CanHandle(PlaybackStopEventArgs eventArgs)
    {
        return eventArgs?.Item != null && eventArgs.Session != null;
    }

    /// <inheritdoc />
    public EventData ExtractEventData(PlaybackStopEventArgs eventArgs)
    {
        var eventData = new EventData
        {
            EventType = EventType.PlaybackStop,
            Timestamp = DateTime.UtcNow,
            UserId = eventArgs.Session?.UserId,
            UserName = eventArgs.Session?.UserName,
            SessionId = eventArgs.Session?.Id,
            ItemId = eventArgs.Item?.Id,
            ItemName = eventArgs.Item?.Name,
            ItemType = eventArgs.Item?.GetType().Name,
            ItemPath = eventArgs.Item?.Path,
            ClientName = eventArgs.Session?.Client,
            DeviceName = eventArgs.Session?.DeviceName,
            DeviceId = eventArgs.Session?.DeviceId,
            PlaybackPositionTicks = eventArgs.PlaybackPositionTicks,
            IsPaused = false
        };

        // Add playback completion information
        eventData.AdditionalData["PlayedToCompletion"] = eventArgs.PlayedToCompletion;

        // Add media-specific information
        if (eventArgs.Item is Episode episode)
        {
            eventData.SeriesName = episode.Series?.Name ?? episode.SeriesName;
            eventData.SeasonNumber = episode.ParentIndexNumber;
            eventData.EpisodeNumber = episode.IndexNumber;
            eventData.AdditionalData["EpisodeName"] = episode.Name;
            eventData.AdditionalData["SeriesId"] = episode.SeriesId;
        }
        else if (eventArgs.Item is Movie movie)
        {
            eventData.Year = movie.ProductionYear;
            eventData.AdditionalData["MovieId"] = movie.Id;
        }

        // Add common media information
        if (eventArgs.Item is BaseItem item)
        {
            eventData.Genres = item.Genres?.ToList() ?? new List<string>();
            eventData.ContentRating = item.OfficialRating;
            eventData.AdditionalData["CommunityRating"] = item.CommunityRating;
            eventData.AdditionalData["Runtime"] = item.RunTimeTicks;
        }

        return eventData;
    }

    /// <inheritdoc />
    public async Task HandleAsync(PlaybackStopEventArgs eventArgs)
    {
        try
        {
            if (!CanHandle(eventArgs))
            {
                _logger.LogDebug("PlaybackStop event cannot be handled - missing required data");
                return;
            }

            var eventData = ExtractEventData(eventArgs);
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlaybackStop event for item {ItemName}", eventArgs.Item?.Name);
        }
    }
}
