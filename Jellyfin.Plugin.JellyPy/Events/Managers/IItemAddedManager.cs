using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.JellyPy.Events.Managers;

/// <summary>
/// Manager interface for processing items added to the library.
/// </summary>
public interface IItemAddedManager
{
    /// <summary>
    /// Queues an item for processing when it's added to the library.
    /// </summary>
    /// <param name="item">The item that was added.</param>
    void QueueItemAdded(BaseItem item);
}
