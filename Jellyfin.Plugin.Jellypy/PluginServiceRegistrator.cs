using Jellyfin.Plugin.Jellypy.Events;
using Jellyfin.Plugin.Jellypy.Events.Handlers;
using Jellyfin.Plugin.Jellypy.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Jellypy;

/// <summary>
/// Register plugin services for enhanced event handling.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register the enhanced entry point and legacy entry point
        serviceCollection.AddHostedService<EnhancedEntryPoint>();
        serviceCollection.AddHostedService<EntryPoint>(); // Keep for backward compatibility

        // Register the script execution service and supporting services
        serviceCollection.AddSingleton<IScriptExecutionService, ScriptExecutionService>();
        serviceCollection.AddSingleton<ConditionEvaluator>();
        serviceCollection.AddSingleton<DataAttributeProcessor>();

        // Register event handlers
        serviceCollection.AddTransient<PlaybackStartHandler>();
        serviceCollection.AddTransient<PlaybackStopHandler>();

        // Register event consumers for the event system
        serviceCollection.AddTransient<IEventConsumer<PlaybackProgressEventArgs>, EnhancedEntryPoint>();

        // Keep legacy registration for backward compatibility
        serviceCollection.AddTransient<IEventConsumer<PlaybackProgressEventArgs>, ExecuteScript>();
    }
}
