using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.JellyPy.Events;

/// <summary>
/// Manager interface for processing items deleted from the library.
/// </summary>
public interface IItemDeletedManager
{
    /// <summary>
    /// Queues an item for processing when it's deleted from the library.
    /// </summary>
    /// <param name="item">The item that was deleted.</param>
    void QueueItemDeleted(BaseItem item);
}
