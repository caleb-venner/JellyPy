using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Models;

namespace Jellyfin.Plugin.JellyPy.Services.Notifications;

/// <summary>
/// Interface for ntfy notification service.
/// </summary>
public interface INtfyService
{
    /// <summary>
    /// Sends a notification for an item added event.
    /// </summary>
    /// <param name="eventData">The event data containing item information.</param>
    /// <returns>True if the notification was sent successfully, false otherwise.</returns>
    Task<bool> SendItemAddedNotificationAsync(EventData eventData);

    /// <summary>
    /// Sends a notification that episode queueing has started for a series/season.
    /// </summary>
    /// <param name="seriesName">Name of the series.</param>
    /// <param name="seasonNumber">Season number (nullable for unknown season).</param>
    /// <param name="episodeCount">Current number of episodes in queue.</param>
    /// <param name="seriesId">Series ID for poster image (nullable).</param>
    /// <returns>True if the notification was sent successfully, false otherwise.</returns>
    Task<bool> SendQueueingStartedNotificationAsync(string seriesName, int? seasonNumber, int episodeCount, Guid? seriesId);

    /// <summary>
    /// Sends a test notification to verify the configuration.
    /// </summary>
    /// <returns>True if the test notification was sent successfully, false otherwise.</returns>
    Task<bool> SendTestNotificationAsync();

    /// <summary>
    /// Gets whether ntfy notifications are enabled and properly configured.
    /// </summary>
    /// <returns>True if enabled and configured, false otherwise.</returns>
    bool IsEnabled();
}
