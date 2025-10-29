using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.JellyPy.Events;

/// <summary>
/// Represents a group of episodes from the same TV series added in the same batch.
/// </summary>
public class SeriesEpisodeGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesEpisodeGroup"/> class.
    /// </summary>
    /// <param name="series">The series entity.</param>
    /// <param name="episodes">The list of episodes from the series.</param>
    /// <param name="enqueuedAt">When the first episode in the group was queued.</param>
    public SeriesEpisodeGroup(Series? series, Collection<Episode> episodes, DateTime enqueuedAt)
    {
        Series = series;
        Episodes = episodes ?? new Collection<Episode>();
        EnqueuedAt = enqueuedAt;
    }

    /// <summary>
    /// Gets the series entity (may be null if not available).
    /// </summary>
    public Series? Series { get; }

    /// <summary>
    /// Gets the list of episodes in this group.
    /// </summary>
    public Collection<Episode> Episodes { get; }

    /// <summary>
    /// Gets the series name.
    /// </summary>
    public string? SeriesName => Series?.Name;

    /// <summary>
    /// Gets the series ID.
    /// </summary>
    public Guid? SeriesId => Series?.Id;

    /// <summary>
    /// Gets the number of episodes in the group.
    /// </summary>
    public int EpisodeCount => Episodes.Count;

    /// <summary>
    /// Gets the first episode in the group (by season/episode number order).
    /// </summary>
    public Episode? FirstEpisode => Episodes.Count > 0 ? Episodes[0] : null;

    /// <summary>
    /// Gets the last episode in the group (by season/episode number order).
    /// </summary>
    public Episode? LastEpisode => Episodes.Count > 0 ? Episodes[^1] : null;

    /// <summary>
    /// Gets when the first episode in the group was queued.
    /// </summary>
    public DateTime EnqueuedAt { get; }

    /// <summary>
    /// Gets the range of seasons covered by this group (e.g., "1-2" or "1").
    /// </summary>
    /// <returns>A string representing the season range.</returns>
    public string GetSeasonRange()
    {
        if (Episodes.Count == 0)
        {
            return "Unknown";
        }

        var seasons = Episodes
            .Where(e => e.ParentIndexNumber.HasValue)
            .Select(e => e.ParentIndexNumber!.Value)
            .ToHashSet();

        if (seasons.Count == 0)
        {
            return "Unknown";
        }

        var sortedSeasons = new List<int>(seasons);
        sortedSeasons.Sort();

        if (seasons.Count == 1)
        {
            return sortedSeasons[0].ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return $"{sortedSeasons[0].ToString(System.Globalization.CultureInfo.InvariantCulture)}-{sortedSeasons[^1].ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Gets episode range display string (e.g., "S01E01-E05" or "S01E01 - S02E03").
    /// </summary>
    /// <returns>A formatted episode range string.</returns>
    public string GetEpisodeRange()
    {
        if (FirstEpisode == null || LastEpisode == null)
        {
            return "Unknown";
        }

        var firstSeason = FirstEpisode.ParentIndexNumber ?? 0;
        var firstEpisode = FirstEpisode.IndexNumber ?? 0;
        var lastSeason = LastEpisode.ParentIndexNumber ?? 0;
        var lastEpisode = LastEpisode.IndexNumber ?? 0;

        if (firstSeason == lastSeason)
        {
            // Same season: S01E01-E05
            return $"S{firstSeason:D2}E{firstEpisode:D2}-E{lastEpisode:D2}";
        }

        // Different seasons: S01E01 - S02E03
        return $"S{firstSeason:D2}E{firstEpisode:D2} - S{lastSeason:D2}E{lastEpisode:D2}";
    }
}
