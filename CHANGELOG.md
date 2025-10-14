# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [1.1.2.0] - 2025-10-14

### Added

- Dual-manifest release system for stable and beta channels
- Automated release scripts (`release.sh` for stable, `release-beta.sh` for beta)
- Beta channel manifest (`manifest-beta.json`) for pre-release testing
- Version-based channel distinction (x.x.x.0 for stable, x.x.x.y for beta)
- Test suite initialization with proper EncryptionHelper setup
- TestFixtureBase class for consistent test configuration

### Changed

- Simplified to single-branch workflow (main branch only)
- Release process now validates version format based on channel
- Test suite now properly initializes encryption helper with mocked dependencies

### Removed

- Beta branch (replaced with beta manifest on main branch)
- Redundant test files (ConfigMigrationTest.cs, XmlSerializationTest.cs)
- Outdated build-release.sh script

### Fixed

- All unit tests now pass (9/9) after EncryptionHelper initialization fix
- Test suite properly synchronized with current codebase

## [1.1.1.0] - 2025-10-14

### Added

- Test Connection buttons for Sonarr and Radarr configuration
- Real-time connection validation with server version detection
- Automatic API key encryption using AES-256-CBC with Jellyfin server-specific keys

### Security

- API keys are now automatically encrypted using Jellyfin server identification
- Server-bound encryption keys survive OS updates, machine renames, and user changes
- Encryption keys unique per Jellyfin installation using Plugin GUID + Server
ID + Static Salt
- Backward compatibility with existing plaintext API keys
(auto-migrated on first save)
- Secure decryption for web interface display without exposing plaintext in storage

### Fixed

- Test Connection functionality works correctly after saving encrypted settings
- Password fields display proper masked values instead of base64 encrypted strings

## [1.0.0.0] - 2025-10-07

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

[Unreleased]: https://github.com/caleb-venner/jellypy/compare/v1.1.2.0...HEAD
[1.1.2.0]: https://github.com/caleb-venner/jellypy/compare/v1.1.0...v1.1.2.0
