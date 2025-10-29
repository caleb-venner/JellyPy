using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Services.Arr;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Handlers;

/// <summary>
/// Handler for playback resume events.
/// </summary>
public class PlaybackResumeHandler : IEventProcessor<PlaybackProgressEventArgs>
{
    private static readonly EventDeduplicationCache DeduplicationCache = new(TimeSpan.FromSeconds(5));

    private readonly ILogger<PlaybackResumeHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackResumeHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    public PlaybackResumeHandler(
        ILogger<PlaybackResumeHandler> logger,
        IScriptExecutionService scriptExecutionService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
    }

    /// <inheritdoc />
    public EventType EventType => EventType.PlaybackResume;

    /// <inheritdoc />
    public bool CanHandle(PlaybackProgressEventArgs eventArgs)
    {
        return eventArgs?.Item != null && eventArgs.Session != null && !eventArgs.IsPaused;
    }

    /// <inheritdoc />
    public EventData ExtractEventData(PlaybackProgressEventArgs eventArgs)
    {
        var eventData = new EventData
        {
            EventType = EventType.PlaybackResume,
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
                _logger.LogDebug("PlaybackResume event cannot be handled - missing required data or is paused");
                return;
            }

            // Deduplicate rapid resume events for the same session to avoid script thrashing
            var eventKey = $"resume-{eventArgs.Session?.Id}";
            if (!DeduplicationCache.ShouldProcessEvent(eventKey))
            {
                _logger.LogDebug("Skipping duplicate PlaybackResume event for session {SessionId}", eventArgs.Session?.Id);
                return;
            }

            var eventData = ExtractEventData(eventArgs);

            // Execute custom scripts (if configured)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlaybackResume event for item {ItemName}", eventArgs.Item?.Name);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }
}
