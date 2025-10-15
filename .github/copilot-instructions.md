# Copilot Instructions for JellyPy Plugin Development

## Project Overview
JellyPy is a Jellyfin plugin that automatically manages Sonarr and Radarr monitoring based on what users watch in Jellyfin. It provides native integration with both services and includes legacy Python script execution support.

## Core Development Guidelines

### Versioning
- **ALWAYS use 4-digit versioning**: `x.x.x.x` (e.g., `1.2.3.0`)
- **Never use 3-digit versions**: Avoid `1.2.3` format
- **Beta versions**: Use 4-digit versions without beta suffix when on beta branch
- **Version locations**: Update both `build.yaml` and manifest files consistently
- **Target ABI**: Currently `10.9.0.0` for Jellyfin compatibility

### Branch Strategy
- **main branch**: Stable releases only
- **beta branch**: Test releases and unstable features
- **Manifest files**:
  - `manifest.json`: Stable channel (deployed from main branch)
  - `manifest-beta.json`: Beta channel (deployed from beta branch)

### Release Process
- **Manual releases only**: No automated GitHub workflows for releases
- **Release naming**: `jellypy_x.x.x.x.zip` format
- **GitHub release tags**: `vx.x.x.x` format
- **File locations**: Store release files in `/releases` directory
- **Beta testing**: Use beta branch and beta manifest for testing

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
- **Version Mismatches**: Ensure build.yaml, manifest, and releases use same version format
- **API Key Security**: Always encrypt API keys, never store in plain text
- **Dependency Bloat**: Only include necessary plugin files, exclude framework dependencies

## Configuration
- **Plugin GUID**: `a5bd541f-38dc-467e-9a9a-15fe3f3bcf5c`
- **Framework**: .NET 8.0
- **Target ABI**: Jellyfin 10.9.0.0+
- **Category**: Notifications

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
- **Stable repository**: `https://caleb-venner.github.io/jellypy/manifest.json`
- **Beta repository**: `https://caleb-venner.github.io/jellypy/manifest-beta.json`

## Development Workflow
1. **Feature development**: Work on appropriate branch (main for stable, beta for testing)
2. **Version updates**: Update `build.yaml` version first
3. **Build testing**: Use VS Code build tasks to test compilation
4. **Manual releases**: Create GitHub releases manually with proper 4-digit tags
5. **Manifest updates**: Update corresponding manifest file after release creation
6. **Deployment**: Push manifest changes to trigger GitHub Pages deployment

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