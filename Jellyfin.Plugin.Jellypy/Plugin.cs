using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Jellypy.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Jellypy;

// Need to register something as a running service, currently ExecuteScript, this will
// create an instance of that class, also need to have eventplaybackstart registered as
// being active. Running service/entry-point then creates ... ?

// Also make a simple config page? Not sure if that is necessary?
// References to .Instance! --- so if the instance exists? For this current instance?

// Create "EntryPoint-service" that receives playback start notifications, then creates a
// new ExecuteScript object that handles each instance of playback = RunScript.

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">mmm.</param>
    /// <param name="xmlSerializer">mmnm.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "JellyPy";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a5bd541f-38dc-467e-9a9a-15fe3f3bcf5c");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
