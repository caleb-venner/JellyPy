# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.1.0] - 2025-10-14

### Added

- Test Connection buttons for Sonarr and Radarr configuration
- Real-time connection validation with server version detection
- Automatic API key encryption using AES-256-CBC with Jellyfin server-specific keys

### Security

- API keys are now automatically encrypted using Jellyfin server identification
- Server-bound encryption keys survive OS updates, machine renames, and user changes
- Encryption keys unique per Jellyfin installation using Plugin GUID + Server ID + Static Salt
- Backward compatibility with existing plaintext API keys (auto-migrated on first save)
- Secure decryption for web interface display without exposing plaintext in storage

### Fixed

- Test Connection functionality works correctly after saving encrypted settings
- Password fields display proper masked values instead of base64 encrypted strings

## [1.0.0] - 2025-10-07

### Initial Release

- Native Sonarr integration with automatic episode monitoring
- Native Radarr integration with smart movie unmonitoring
- Configurable watch percentage detection
- Quality cutoff checking for upgrade-eligible content
- Season-based monitoring control
- Dynamic episode buffer management
- Custom script execution support (legacy feature)
- Comprehensive configuration UI
- Plugin distribution system with third-party repository

### Details

- **Sonarr Integration**:
  - Automatically monitors next N episodes after watching
  - Smart episode buffer management with configurable minimum unwatched queue
  - Season-by-season monitoring control
  - Unmonitor watched episodes to reduce clutter
  - Automatic search triggering for newly monitored content

- **Radarr Integration**:
  - Automatically unmonitor movies after watching
  - Percentage-based watch detection (configurable threshold)
  - Quality cutoff checking to prevent premature unmonitoring
  - Keep movies monitored if upgrades are still possible

- **Legacy Python Script Support**:
  - Execute custom Python scripts on Jellyfin events
  - Flexible conditional execution based on media properties
  - Custom environment variables and data attributes

[Unreleased]: https://github.com/caleb-venner/jellypy/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/caleb-venner/jellypy/releases/tag/v1.0.0
