using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy;

/// <summary>
/// Playback start notifier.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlaybackStartNotifier"/> class.
/// </remarks>
public class PlaybackStartNotifier : IEventConsumer<PlaybackStartEventArgs>
{
    private readonly ExecuteScript _script;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartNotifier"/> class.
    /// </summary>
    /// <param name="script">.</param>
    public PlaybackStartNotifier(ExecuteScript script)
    {
        _script = script;
    }

    /// <inheritdoc/>
    public async Task<string> OnEvent()
    {
        return await _script.RunScript().ConfigureAwait(false);
    }
}
