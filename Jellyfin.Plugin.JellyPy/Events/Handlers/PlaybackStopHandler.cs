using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Models;
using Jellyfin.Plugin.JellyPy.Services.Arr;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Handlers;

/// <summary>
/// Handler for playback stop events.
/// </summary>
public class PlaybackStopHandler : IEventProcessor<PlaybackStopEventArgs>
{
    private readonly ILogger<PlaybackStopHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;
    private readonly IArrIntegrationService _arrIntegrationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStopHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    /// <param name="arrIntegrationService">The Arr integration service.</param>
    public PlaybackStopHandler(
        ILogger<PlaybackStopHandler> logger,
        IScriptExecutionService scriptExecutionService,
        IArrIntegrationService arrIntegrationService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
        _arrIntegrationService = arrIntegrationService;
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
    public async Task HandleAsync(PlaybackStopEventArgs eventArgs)
    {
        try
        {
            if (!CanHandle(eventArgs))
            {
                _logger.LogVerbose("PlaybackStop event cannot be handled - missing required data");
                return;
            }

            var eventData = ExtractEventData(eventArgs);

            // Calculate watch percentage for movies
            if (eventArgs.Item is Movie movie)
            {
                var watchPercentage = CalculateWatchPercentage(eventArgs);
                _logger.LogVerbose(
                    "Movie playback stopped: {MovieName}, Watch Percentage: {Percentage}%",
                    movie.Name,
                    watchPercentage);

                // Process movie with watch percentage for conditional unmonitoring
                await _arrIntegrationService.ProcessPlaybackStopAsync(movie, watchPercentage).ConfigureAwait(false);
            }

            // Execute custom scripts (if configured)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PlaybackStop event for item {ItemName}", eventArgs.Item?.Name);
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    private double CalculateWatchPercentage(PlaybackStopEventArgs eventArgs)
    {
        if (eventArgs.Item?.RunTimeTicks == null || eventArgs.Item.RunTimeTicks == 0)
        {
            return 0;
        }

        var runtime = eventArgs.Item.RunTimeTicks.Value;
        var position = eventArgs.PlaybackPositionTicks ?? 0;

        return Math.Round((double)position / runtime * 100, 2);
    }
}
