using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellypy.Services.Arr;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Events.Handlers;

/// <summary>
/// Handler for playback start events.
/// </summary>
public class PlaybackStartHandler : IEventProcessor<PlaybackProgressEventArgs>
{
    private readonly ILogger<PlaybackStartHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;
    private readonly IArrIntegrationService _arrIntegrationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    /// <param name="arrIntegrationService">The Arr integration service.</param>
    public PlaybackStartHandler(
        ILogger<PlaybackStartHandler> logger,
        IScriptExecutionService scriptExecutionService,
        IArrIntegrationService arrIntegrationService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
        _arrIntegrationService = arrIntegrationService;
    }

    /// <inheritdoc />
    public EventType EventType => EventType.PlaybackStart;

    /// <inheritdoc />
    public bool CanHandle(PlaybackProgressEventArgs eventArgs)
    {
        return eventArgs?.Item != null && eventArgs.Session != null;
    }

    /// <inheritdoc />
    public EventData ExtractEventData(PlaybackProgressEventArgs eventArgs)
    {
        var eventData = new EventData
        {
            EventType = EventType.PlaybackStart,
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
            IsPaused = eventArgs.IsPaused
        };

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
            var genres = item.Genres?.ToList() ?? new List<string>();
            foreach (var genre in genres)
            {
                eventData.Genres.Add(genre);
            }

            eventData.ContentRating = item.OfficialRating;
            eventData.AdditionalData["CommunityRating"] = item.CommunityRating;
            eventData.AdditionalData["Runtime"] = item.RunTimeTicks;
        }

        return eventData;
    }

    /// <inheritdoc />
    public async Task HandleAsync(PlaybackProgressEventArgs eventArgs)
    {
        try
        {
            if (!CanHandle(eventArgs))
            {
                _logger.LogDebug("PlaybackStart event cannot be handled - missing required data");
                return;
            }

            var eventData = ExtractEventData(eventArgs);

            // Execute native Sonarr/Radarr integration
            await _arrIntegrationService.ProcessPlaybackStartAsync(eventArgs.Item).ConfigureAwait(false);

            // Execute custom scripts (if configured)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // CA1031: We catch Exception here as a safety net for truly unexpected errors.
            // This is a high-level event handler that coordinates multiple services and should not swallow exceptions.
            _logger.LogError(ex, "Error handling PlaybackStart event for item {ItemName}", eventArgs.Item?.Name);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }
}
