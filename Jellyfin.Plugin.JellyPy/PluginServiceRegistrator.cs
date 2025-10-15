using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using Jellyfin.Plugin.JellyPy.Services;
using Jellyfin.Plugin.JellyPy.Services.Arr;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.JellyPy;

/// <summary>
/// Register plugin services for enhanced event handling.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Initialize encryption helper with server information for server-based key generation
        EncryptionHelper.Initialize(applicationHost);

        // Register the entry point
        serviceCollection.AddHostedService<EnhancedEntryPoint>();

        // Register the script execution service and supporting services
        serviceCollection.AddSingleton<IScriptExecutionService, ScriptExecutionService>();
        serviceCollection.AddSingleton<ConditionEvaluator>();
        serviceCollection.AddSingleton<DataAttributeProcessor>();

        // Register Sonarr/Radarr integration services
        serviceCollection.AddHttpClient(); // Required for HTTP API calls
        serviceCollection.AddSingleton<ISonarrService, SonarrService>();
        serviceCollection.AddSingleton<IRadarrService, RadarrService>();
        serviceCollection.AddSingleton<IArrIntegrationService, ArrIntegrationService>();

        // Register event handlers
        serviceCollection.AddTransient<PlaybackStartHandler>();
        serviceCollection.AddTransient<PlaybackStopHandler>();
    }
}
