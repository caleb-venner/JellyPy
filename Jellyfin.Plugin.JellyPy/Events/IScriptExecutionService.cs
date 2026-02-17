using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Events.Models;

namespace Jellyfin.Plugin.JellyPy.Events;

/// <summary>
/// Service for executing scripts based on events.
/// </summary>
public interface IScriptExecutionService
{
    /// <summary>
    /// Executes all configured scripts for the given event data.
    /// </summary>
    /// <param name="eventData">The event data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteScriptsAsync(EventData eventData);
}
