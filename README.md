# Jellypy - Jellyfin Plugin for Sonarr & Radarr Integration

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Jellyfin](https://img.shields.io/badge/Jellyfin-10.9+-aa5cc3)](https://jellyfin.org/)

A Jellyfin plugin that automatically updates your Sonarr and Radarr monitoring
based on what you watch. Keep your media organised and automatic without manual intervention.

## Features

### Native Sonarr Integration

- **Automatic Episode Monitoring**: Monitors next N episodes after watching
- **Smart File Detection**: Skip re-downloading episodes that already exist
- **Season Control**: Limit monitoring to current season only
- **Dynamic Buffer**: Maintains minimum unwatched episode queue
- **Automatic Search**: Triggers Sonarr searches for newly monitored episodes
- **Future Episode Monitoring**: Automatically monitor episodes when they air

### Native Radarr Integration

- **Automatic Movie Unmonitoring**: Unmonitor movies after watching
- **Percentage-Based Logic**: Only unmonitor if watched past configurable
threshold (default 90%)
- **Quality Cutoff Checking**: Only unmonitor movies that have reached their
quality target
- **Upgrade Prevention**: Keep monitoring movies that can still be upgraded

### Custom Script Execution

- Execute Python scripts on Jellyfin events
- Flexible event triggers (PlaybackStart, PlaybackStop, etc.)
- Custom environment variables and data attributes
- Conditional execution based on media properties

### Security & Configuration

- **Encrypted API Keys**: Automatic AES-256 encryption for Sonarr/Radarr API keys
- **Test Connections**: Built-in connection testing for API endpoints  
- **Server-Bound Encryption**: API keys encrypted with Jellyfin server-specific keys
- **System-Independent**: Encryption survives OS updates, machine renames,
and user changes
- **Auto Migration**: Existing plaintext keys automatically encrypted on first save

## Requirements

- **Jellyfin**: 10.10.0 or higher
- **Sonarr**: v3 API (optional)
- **Radarr**: v3 API (optional)
- **.NET**: 8.0 (bundled with Jellyfin)

## 🔧 Installation

### Via Jellyfin Plugin Repository (Recommended)

1. Open Jellyfin Dashboard → Plugins → Repositories
2. Add repository: `https://raw.githubusercontent.com/caleb-venner/jellypy/main/manifest.json`
3. Go to Catalog → Search "Jellypy"
4. Click Install → Restart Jellyfin

### Manual Installation

1. Download the latest `Jellyfin.Plugin.Jellypy.dll`
from [Releases](https://github.com/caleb-venner/jellypy/releases)
2. Copy to your Jellyfin plugins directory:
   - Linux: `/var/lib/jellyfin/plugins/Jellypy/`
   - Windows: `C:\ProgramData\Jellyfin\Server\plugins\Jellypy\`
   - Docker: `/config/plugins/Jellypy/`
3. Restart Jellyfin

## ⚙️ Configuration

### Sonarr Setup

1. Navigate to **Dashboard → Plugins → Jellypy → Settings → Native Integration**
2. Configure Sonarr:

   ```text
   Enable Sonarr Integration: ✓
   Sonarr URL: http://localhost:8989
   Sonarr API Key: [Your API Key from Sonarr → Settings → General → Security]
   ```

3. Click **Test Sonarr Connection** to verify your settings

4. **Configuration Options:**

   | Setting | Default | Description |
   |---------|---------|-------------|
   | Episodes to Monitor | 5 | Number of upcoming episodes to keep monitored |
   | Auto-Search Episodes | ✓ | Automatically trigger searches for monitored episodes |
   | Monitor Future Episodes | ✓ | Auto-monitor episodes when they become available |
   | Skip Episodes With Files | ✓ | Don't re-monitor episodes that already have files |
   | **Unmonitor Watched Episodes** | ✓ | Unmonitor episodes after watching them |
   | **Monitor Only Current Season** | ✗ | Only monitor episodes in the current season |
   | **Minimum Episode Buffer** | 2 | Minimum unwatched episodes to maintain |

### Radarr Setup

1. Navigate to **Dashboard → Plugins → Jellypy → Settings → Native Integration**
2. Configure Radarr:

   ```text
   Enable Radarr Integration: ✓
   Radarr URL: http://localhost:7878
   Radarr API Key: [Your API Key from Radarr → Settings → General → Security]
   ```

3. Click **Test Radarr Connection** to verify your settings

4. **Configuration Options:**

   | Setting | Default | Description |
   |---------|---------|-------------|
   | Unmonitor Watched Movies | ✓ | Set movies to unmonitored after watching |
   | **Unmonitor Only If Watched** | ✗ | Only unmonitor if watch percentage exceeds threshold |
   | **Minimum Watch Percentage** | 90% | Percentage required to consider movie "watched" |
   | **Unmonitor After Quality Cutoff** | ✗ | Only unmonitor movies at their quality target |

## How It Works

### TV Shows (Sonarr)

**When you watch an episode:**

1. Current episode is unmonitored (if enabled)
2. Plugin calculates next episodes to monitor based on:
   - Episode buffer setting
   - Minimum unwatched episode count
   - Season filtering (if enabled)
   - Existing file check (if enabled)
3. Next N episodes are monitored in Sonarr
4. Automatic searches are triggered (if enabled)
5. Future episodes are set to auto-monitor (if enabled)

**Example Workflow:**

```text
You watch: The Office S03E05
Result:
   - S03E05 unmonitored
   - S03E06, S03E07, S03E08, S03E09, S03E10 monitored
   - Searches triggered for episodes without files
   - Future episodes set to auto-monitor when released
```

### Movies (Radarr)

**When you finish watching a movie:**

1. Plugin calculates watch percentage (position ÷ runtime)
2. Checks if percentage meets threshold (default 90%)
3. If "Unmonitor After Quality Cutoff" enabled:
   - Queries Radarr for movie quality details
   - Checks if current file quality meets cutoff
   - Keeps monitoring if upgrade possible
4. Unmonitors movie in Radarr if all checks pass

**Example Workflow:**

```text
You watch: The Matrix (1999) - 85% complete
Result: Movie stays monitored (below 90% threshold)

You watch: The Matrix (1999) - 95% complete
Result:
   - Check quality: 1080p Bluray vs cutoff (1080p)
   - Quality met → Movie unmonitored
   - Prevents unnecessary upgrade searches
```

## 🔍 Use Cases

### Scenario 1: Binge Watching

- Watch S01E01 of a new show
- Plugin automatically queues next 5 episodes
- Sonarr downloads them overnight
- Continue watching seamlessly

### Scenario 2: Quality Upgrades

- Disable "Skip Episodes With Files"
- Re-monitor episodes for quality upgrades
- Sonarr automatically upgrades to better quality

### Scenario 3: Season-by-Season

- Enable "Monitor Only Current Season"
- Finish Season 1
- Season 2 automatically starts monitoring when you begin watching

### Scenario 4: Movie Collection

- Watch movies from your library
- Movies automatically unmonitor after watching
- Only keep unwatched movies monitored
- Reduce unnecessary Radarr activity

## 🛠️ Troubleshooting

### Plugin Not Working

1. **Check Jellyfin Logs**:

   ```text
   Dashboard → Logs → Filter: "Jellypy"
   ```

   Look for configuration errors or API connection issues

2. **Verify API Keys**:
   - Test in Sonarr/Radarr directly
   - Check network connectivity from Jellyfin server

3. **Confirm Integration Enabled**:
   - Native Integration tab → Checkboxes enabled
   - URLs correctly formatted (include http://)

### Episodes Not Monitoring

- Verify "Episodes to Monitor" > 0
- Check "Skip Episodes With Files" setting
- Ensure series exists in Sonarr
- Check Sonarr logs for API errors

### Movies Not Unmonitoring

- Verify "Unmonitor Watched Movies" enabled
- Check watch percentage in logs
- Confirm movie exists in Radarr
- Review quality cutoff settings if enabled

### Getting More Information

Enable verbose logging:

```text
Dashboard → Plugins → Jellypy → Global Settings
Enable Verbose Logging: ✓
```

## 📚 Advanced Configuration

### Custom Scripts (Legacy Feature)

The plugin also supports custom Python script execution:

1. **Script Settings Tab** → Add Script Setting
2. Configure:
   - Script path
   - Python executable (auto-detected)
   - Event triggers (PlaybackStart, PlaybackStop)
   - Conditions (media type, genres, etc.)
   - Data attributes to pass

See `examples/` directory for sample scripts.

## Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes with tests
4. Submit a pull request

## 📝 License

This project is licensed under the GNU General Public
License v3.0 - see [LICENSE](LICENSE) file for details.

**Why GPLv3?** Jellyfin is licensed under GPLv3, and to
ensure compatibility and maintain the free and open-source
nature of the ecosystem, this plugin uses the same license.

---
