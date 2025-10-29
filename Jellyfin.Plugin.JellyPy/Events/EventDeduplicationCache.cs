using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Jellyfin.Plugin.JellyPy.Events;

/// <summary>
/// Utility for tracking recent events to prevent duplicate rapid-fire executions.
/// </summary>
public class EventDeduplicationCache
{
    private readonly TimeSpan _threshold;
    private readonly ConcurrentDictionary<string, DateTime> _recentEvents = new();
    private readonly TimeSpan _cleanupThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDeduplicationCache"/> class.
    /// </summary>
    /// <param name="threshold">Time threshold for considering events duplicates (e.g., 5 seconds).</param>
    public EventDeduplicationCache(TimeSpan threshold)
    {
        _threshold = threshold;
        _cleanupThreshold = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Checks if an event should be processed based on recent history.
    /// </summary>
    /// <param name="eventKey">Unique identifier for the event (e.g., session ID).</param>
    /// <returns>True if the event should be processed, false if it's a duplicate within threshold.</returns>
    public bool ShouldProcessEvent(string eventKey)
    {
        CleanupOldEntries();

        if (_recentEvents.TryGetValue(eventKey, out var lastProcessedTime))
        {
            if (DateTime.UtcNow - lastProcessedTime < _threshold)
            {
                return false; // Event is a duplicate, skip it
            }
        }

        // Update or add the event timestamp
        _recentEvents[eventKey] = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Removes old entries from the cache to prevent memory bloat.
    /// </summary>
    private void CleanupOldEntries()
    {
        var cutoffTime = DateTime.UtcNow - _cleanupThreshold;
        var oldKeys = _recentEvents
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldKeys)
        {
            _recentEvents.TryRemove(key, out _);
        }
    }
}
