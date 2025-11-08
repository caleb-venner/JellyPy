# Copilot Instructions for JellyPy Plugin Development

## Project Overview
JellyPy is a Jellyfin plugin that automatically manages Sonarr and Radarr monitoring based on what users watch in Jellyfin. It provides native integration with both services and includes legacy Python script execution support.

## Core Development Guidelines

### Versioning
- **ALWAYS use 4-digit versioning**: `x.x.x.x` (e.g., `1.2.3.0`)
- **Never use 3-digit versions**: Avoid `1.2.3` format
- **Version locations**: `Directory.Build.props` (for .NET builds) and `manifest.json` (for Jellyfin)
- **Target ABI**: Currently `10.9.0.0` for Jellyfin compatibility

### Branch Strategy
- **main branch**: All releases
- **Manifest files**:
  - `manifest.json`: Single release channel (deployed from main branch)

### Release Process
- **Automated GitHub Actions releases**: Triggered by version tags (vx.x.x.x)
- **Release naming**: `jellypy_x.x.x.x.zip` format
- **GitHub release tags**: `vx.x.x.x` format (e.g., v2.1.0.0)
- **File locations**: GitHub Release assets (no longer using /releases directory)
- **Single manifest**: All releases published to manifest.json
- **Workflow**: Run `make release`, commit, create annotated tag, push - GitHub Actions handles the rest
- **Dev builds**: Use `make dev` for local testing and development
- **Release notes**: Stored in git tag annotations, extracted by GitHub Actions for release description

### Code Style and Content
- **No emojis or icons**: Never use emojis in code, comments, changelogs, or documentation
- **Clean formatting**: Use professional, plain text formatting
- **Bullet points**: Use `•` character for lists, not `-` or `*`
- **HTTP clients**: Never add Content-Type to DefaultRequestHeaders (use HttpContent headers)

## Technical Architecture

### Key Components
- **RadarrService**: Handles Radarr API communication and movie monitoring
- **SonarrService**: Manages Sonarr API integration and episode monitoring
- **ArrIntegrationService**: Coordinates between services and Jellyfin events
- **ScriptExecutionService**: Legacy Python script execution support
- **EncryptionHelper**: Handles API key encryption using Jellyfin SystemId

### Common Issues to Avoid
- **HTTP Header Misuse**: Don't add Content-Type to request headers, only to content
- **Version Mismatches**: Ensure Directory.Build.props matches git tag version
- **API Key Security**: Always encrypt API keys, never store in plain text
- **Dependency Bloat**: Only include necessary plugin files, exclude framework dependencies
- **Lightweight Tags**: Always use annotated tags (`git tag -a`) for releases, never lightweight tags

## Configuration
- **Plugin GUID**: `a5bd541f-38dc-467e-9a9a-15fe3f3bcf5c`
- **Framework**: .NET 8.0
- **Target ABI**: Jellyfin 10.9.0.0+
- **Category**: Notifications
- **Version Source**: Jellyfin reads `manifest.json` only; `Directory.Build.props` is for .NET builds

## File Structure
```
Jellyfin.Plugin.Jellypy/
├── Configuration/           # Plugin configuration and web interface
├── Events/                 # Event handling and data structures  
├── Services/               # Core business logic
│   └── Arr/               # Sonarr/Radarr integration services
└── Plugin.cs              # Main plugin entry point
```

## Repository URLs
- **Plugin repository**: `https://raw.githubusercontent.com/caleb-venner/jellypy/main/manifest.json`

## Development Workflow
1. **Feature development**: Work on main branch (all testing done locally)
2. **Version updates**: Run `make release` to prepare new version
3. **Build testing**: Script verifies compilation automatically
4. **Releases**: Create annotated git tags (GitHub Actions creates releases automatically)
5. **Manifest updates**: Update `manifest.json` after GitHub Actions completes (includes checksum)
6. **Deployment**: Push manifest changes to trigger GitHub Pages deployment

### Release Steps
1. Run `make release` - prompts for version and changelog
2. Review and commit `Directory.Build.props` and `CHANGELOG.md`
3. Create annotated tag: `git tag -a v2.1.0.0 -F changelog-file`
4. Push: `git push origin main && git push origin v2.1.0.0`
5. Done! GitHub Actions automatically:
   - Builds and creates release
   - Updates `manifest.json` with version, changelog, sourceUrl, and checksum
   - Commits and pushes manifest changes
   - Deploys to GitHub Pages

## Security Guidelines
- **API Key Encryption**: Use AES-256-CBC with PBKDF2 key derivation (10,000 iterations)
- **Key Components**: Plugin GUID + Server ID + Static Salt for key generation
- **Automatic Encryption**: Encrypt API keys on save in configuration
- **System Independence**: Encryption survives OS updates and machine changes

## Testing Strategy
- **Beta channel**: Use beta branch and manifest for pre-release testing
- **Integration testing**: Test with real Sonarr/Radarr instances
- **Error handling**: Comprehensive logging and graceful failure handling
- **Configuration validation**: Test connection functionality for external services

## Documentation Standards
- **Professional tone**: No casual language or emojis
- **Clear structure**: Use consistent formatting and bullet points
- **Code examples**: Include practical configuration examples
- **Troubleshooting**: Document common issues and solutions

Remember: This is a production plugin used by real users. Prioritize stability, security, and clear communication over flashy features.