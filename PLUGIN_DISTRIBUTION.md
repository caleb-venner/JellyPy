# JellyPy Plugin Repository

This repository provides the JellyPy plugin for Jellyfin
through a third-party plugin catalog.

## About JellyPy

JellyPy automatically manages your Sonarr and Radarr
monitoring based on what you watch in Jellyfin.

### Features

- **Native Sonarr Integration**: Automatically monitors next N episodes after watching
- **Native Radarr Integration**: Smart movie unmonitoring with quality cutoff checking
- **Configurable Watch Detection**: Percentage-based thresholds
- **Smart Episode Management**: Buffer management and season control
- **Legacy Script Support**: Execute custom Python scripts on Jellyfin events

## Installation

### Option 1: Add Repository to Jellyfin

1. In Jellyfin, go to **Administration** →
**Dashboard** → **Plugins** → **Repositories**
2. Click **Add Repository**
3. Enter the following details:
   - **Repository Name**: `JellyPy`
   - **Repository URL**: `https://YOUR_HOSTED_DOMAIN/manifest.json`
4. Click **Save**
5. Go to **Catalog** tab and install **JellyPy**

### Option 2: Manual Installation

1. Download the latest `jellypy_x.x.x.x.zip` from
 [Releases](https://github.com/caleb-venner/jellypy/releases)
2. Extract the zip file
3. Copy the `.dll` files to your Jellyfin plugins directory:
   - **Linux**: `/var/lib/jellyfin/plugins/Jellypy/`
   - **Windows**: `C:\ProgramData\Jellyfin\Server\plugins\Jellypy\`
   - **macOS**: `/var/lib/jellyfin/plugins/Jellypy/`
4. Restart Jellyfin
5. Configure the plugin
in **Administration** → **Dashboard** → **Plugins** → **JellyPy**

## Configuration

After installation, configure your Sonarr and Radarr connections
 in the plugin settings:

1. **Sonarr Settings**: API URL, API Key, monitoring preferences
2. **Radarr Settings**: API URL, API Key, unmonitoring preferences
3. **Watch Detection**: Configure percentage thresholds
4. **Advanced Options**: Episode buffers, quality checking, etc.

## Support

- **Issues**: [GitHub Issues](https://github.com/caleb-venner/jellypy/issues)
- **Documentation**: [Main Repository](https://github.com/caleb-venner/jellypy)

## License

This plugin is licensed under GPLv3 - see
the [LICENSE](https://github.com/caleb-venner/jellypy/blob/main/LICENSE)
file for details.
