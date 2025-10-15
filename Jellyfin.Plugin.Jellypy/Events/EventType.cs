using System;

namespace Jellyfin.Plugin.JellyPy.Events;

/// <summary>
/// Enumeration of supported event types for script execution.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Playback has started.
    /// </summary>
    PlaybackStart,

    /// <summary>
    /// Playback has stopped.
    /// </summary>
    PlaybackStop,

    /// <summary>
    /// Playback has been paused.
    /// </summary>
    PlaybackPause,

    /// <summary>
    /// Playback has resumed from pause.
    /// </summary>
    PlaybackResume,

    /// <summary>
    /// An item has been added to the library.
    /// </summary>
    ItemAdded,

    /// <summary>
    /// An existing library item has been updated.
    /// </summary>
    ItemUpdated,

    /// <summary>
    /// An item has been removed from the library.
    /// </summary>
    ItemRemoved,

    /// <summary>
    /// A new user has been created.
    /// </summary>
    UserCreated,

    /// <summary>
    /// An existing user has been updated.
    /// </summary>
    UserUpdated,

    /// <summary>
    /// A user has been deleted.
    /// </summary>
    UserDeleted,

    /// <summary>
    /// A user session has started.
    /// </summary>
    SessionStart,

    /// <summary>
    /// A user session has ended.
    /// </summary>
    SessionEnd,

    /// <summary>
    /// Server has started up.
    /// </summary>
    ServerStartup,

    /// <summary>
    /// Server is shutting down.
    /// </summary>
    ServerShutdown
}
