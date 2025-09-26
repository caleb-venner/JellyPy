using System.Threading.Tasks;

namespace Jellyfin.Plugin.Jellypy.Events;

/// <summary>
/// Generic event handler interface for processing various Jellyfin events.
/// </summary>
/// <typeparam name="T">The event arguments type.</typeparam>
public interface IGenericEventHandler<in T>
{
    /// <summary>
    /// Gets the event type this handler processes.
    /// </summary>
    EventType EventType { get; }

    /// <summary>
    /// Determines if this handler should process the given event.
    /// </summary>
    /// <param name="eventArgs">The event arguments.</param>
    /// <returns>True if the handler should process the event, false otherwise.</returns>
    bool CanHandle(T eventArgs);

    /// <summary>
    /// Processes the event and extracts relevant data.
    /// </summary>
    /// <param name="eventArgs">The event arguments.</param>
    /// <returns>The extracted event data.</returns>
    EventData ExtractEventData(T eventArgs);

    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="eventArgs">The event arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(T eventArgs);
}
