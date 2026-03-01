using System;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Events.Handlers;
using Jellyfin.Plugin.JellyPy.Events.Managers;
using Jellyfin.Plugin.JellyPy.Services;
using Jellyfin.Plugin.JellyPy.Services.Arr;
using Jellyfin.Plugin.JellyPy.Services.Notifications;
using MediaBrowser.Controller;
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

        // Register configuration provider
        serviceCollection.AddSingleton<IPluginConfigurationProvider, PluginConfigurationProvider>();

        // Register the entry point
        serviceCollection.AddHostedService<EnhancedEntryPoint>();

        // Register library event managers (hosted services that process queued items)
        serviceCollection.AddSingleton<IItemAddedManager, ItemAddedManager>();
        serviceCollection.AddSingleton<IItemDeletedManager, ItemDeletedManager>();
        serviceCollection.AddHostedService(provider => provider.GetRequiredService<IItemAddedManager>() as ItemAddedManager ?? throw new InvalidOperationException("Failed to resolve ItemAddedManager"));
        serviceCollection.AddHostedService(provider => provider.GetRequiredService<IItemDeletedManager>() as ItemDeletedManager ?? throw new InvalidOperationException("Failed to resolve ItemDeletedManager"));

        // Register the script execution service and supporting services
        serviceCollection.AddSingleton<IScriptExecutionService, ScriptExecutionService>();
        serviceCollection.AddSingleton<ConditionEvaluator>();
        serviceCollection.AddSingleton<DataAttributeProcessor>();

        // Register Sonarr/Radarr integration services
        serviceCollection.AddHttpClient(); // Required for HTTP API calls
        serviceCollection.AddSingleton<ISonarrService, SonarrService>();
        serviceCollection.AddSingleton<IRadarrService, RadarrService>();
        serviceCollection.AddSingleton<IArrIntegrationService, ArrIntegrationService>();

        // Register notification services
        serviceCollection.AddSingleton<INtfyService, NtfyService>();

        // Register event handlers
        serviceCollection.AddTransient<PlaybackStartHandler>();
        serviceCollection.AddTransient<PlaybackStopHandler>();
        serviceCollection.AddTransient<PlaybackPauseHandler>();
        serviceCollection.AddTransient<PlaybackResumeHandler>();
        serviceCollection.AddTransient<ItemAddedHandler>();
        serviceCollection.AddTransient<ItemDeletedHandler>();
        serviceCollection.AddTransient<SeriesEpisodesAddedHandler>();
    }
}
