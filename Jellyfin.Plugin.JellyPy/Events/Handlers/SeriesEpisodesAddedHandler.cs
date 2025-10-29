using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Events.Handlers;

/// <summary>
/// Handler for multiple episodes from the same series being added to the library.
/// Creates a single aggregated event for batch operations.
/// </summary>
public class SeriesEpisodesAddedHandler
{
    private readonly ILogger<SeriesEpisodesAddedHandler> _logger;
    private readonly IScriptExecutionService _scriptExecutionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesEpisodesAddedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scriptExecutionService">The script execution service.</param>
    public SeriesEpisodesAddedHandler(
        ILogger<SeriesEpisodesAddedHandler> logger,
        IScriptExecutionService scriptExecutionService)
    {
        _logger = logger;
        _scriptExecutionService = scriptExecutionService;
    }

    /// <summary>
    /// Handles a group of episodes from the same series.
    /// </summary>
    /// <param name="group">The series episode group.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(SeriesEpisodeGroup group)
    {
        try
        {
            if (group == null || group.Episodes.Count == 0)
            {
                _logger.LogDebug("SeriesEpisodesAdded event cannot be handled - no episodes provided");
                return;
            }

            var eventData = ExtractEventData(group);

            _logger.LogInformation(
                "Processing SeriesEpisodesAdded event for series {SeriesName} with {EpisodeCount} episodes ({EpisodeRange})",
                group.SeriesName,
                group.EpisodeCount,
                eventData.EpisodeRange);

            // Execute custom scripts (if configured for SeriesEpisodesAdded event)
            await _scriptExecutionService.ExecuteScriptsAsync(eventData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SeriesEpisodesAdded event");
            throw; // Rethrow to maintain CA1031 compliance while still logging the error
        }
    }

    /// <summary>
    /// Extracts event data from a series episode group.
    /// </summary>
    /// <param name="group">The series episode group.</param>
    /// <returns>The event data containing series and episode information.</returns>
    private static EventData ExtractEventData(SeriesEpisodeGroup group)
    {
        var firstEpisode = group.FirstEpisode;
        var lastEpisode = group.LastEpisode;

        var eventData = new EventData
        {
            EventType = EventType.SeriesEpisodesAdded,
            Timestamp = DateTime.UtcNow,
            SeriesName = group.SeriesName,
            ItemName = group.SeriesName, // For display purposes
            ItemType = "SeriesEpisodes",
            EpisodeGroupCount = group.EpisodeCount,
            EpisodeRange = group.GetEpisodeRange(),
            SeasonRange = group.GetSeasonRange()
        };

        // Add series information if available
        if (group.Series != null)
        {
            eventData.ItemId = group.Series.Id;
            eventData.ItemPath = group.Series.Path;

            // Add common media information
            var genres = group.Series.Genres?.ToList() ?? new List<string>();
            foreach (var genre in genres)
            {
                eventData.Genres.Add(genre);
            }

            eventData.ContentRating = group.Series.OfficialRating;
            eventData.AdditionalData["CommunityRating"] = group.Series.CommunityRating;
            eventData.AdditionalData["Runtime"] = group.Series.RunTimeTicks;
            eventData.AdditionalData["DateCreated"] = group.Series.DateCreated;
            eventData.AdditionalData["SeriesId"] = group.Series.Id;
        }

        // Add first and last episode details for context
        if (firstEpisode != null)
        {
            eventData.AdditionalData["FirstEpisodeName"] = firstEpisode.Name;
            eventData.AdditionalData["FirstEpisodeId"] = firstEpisode.Id;
            if (firstEpisode.ParentIndexNumber.HasValue)
            {
                eventData.AdditionalData["FirstEpisodeSeason"] = firstEpisode.ParentIndexNumber.Value;
            }

            if (firstEpisode.IndexNumber.HasValue)
            {
                eventData.AdditionalData["FirstEpisodeNumber"] = firstEpisode.IndexNumber.Value;
            }
        }

        if (lastEpisode != null && lastEpisode.Id != firstEpisode?.Id)
        {
            eventData.AdditionalData["LastEpisodeName"] = lastEpisode.Name;
            eventData.AdditionalData["LastEpisodeId"] = lastEpisode.Id;
            if (lastEpisode.ParentIndexNumber.HasValue)
            {
                eventData.AdditionalData["LastEpisodeSeason"] = lastEpisode.ParentIndexNumber.Value;
            }

            if (lastEpisode.IndexNumber.HasValue)
            {
                eventData.AdditionalData["LastEpisodeNumber"] = lastEpisode.IndexNumber.Value;
            }
        }

        // Add all episode details to the collection
        foreach (var episode in group.Episodes)
        {
            eventData.Episodes.Add(episode);
        }

        // Add summary information
        eventData.AdditionalData["EpisodeGroupTotalCount"] = group.Episodes.Count;
        eventData.AdditionalData["ProcessedAt"] = DateTime.UtcNow;

        return eventData;
    }
}
