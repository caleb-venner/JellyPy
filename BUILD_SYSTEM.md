# JellyPy Build System

This project uses a comprehensive Makefile for development workflow. The Makefile provides convenient commands for building, testing, and deploying the JellyPy plugin.

## Prerequisites

- .NET 8.0 SDK
- Make (available on macOS/Linux by default, Windows users can use WSL or install make)

## Available Commands

### Development Commands

- `make dev` - Build for development (Debug configuration)
- `make dev-publish` - Build and publish development version
- `make release` - Build for release (Release configuration)
- `make clean` - Clean all build artifacts

### Testing Commands

- `make test` - Run all unit tests
- `make test-coverage` - Run tests with code coverage reports
- `make test-project` - Create test project if it doesn't exist

### Code Quality Commands

- `make lint` - Run code analysis and style checks
- `make format` - Format code using dotnet format
- `make format-check` - Check if code is properly formatted

### Distribution Commands

- `make package` - Create a plugin package for distribution
- `make install-dev` - Build and install plugin to local Jellyfin (development)
- `make install-release` - Build and install plugin to local Jellyfin (release)

### Utility Commands

- `make help` - Show all available commands with descriptions
- `make info` - Show project information
- `make watch` - Watch for changes and rebuild automatically
- `make restore` - Restore NuGet packages
- `make check-dotnet` - Check if .NET SDK is installed

## Quick Start

```bash
# Show all available commands
make help

# Build for development
make dev

# Run linting and code analysis
make lint

# Run tests
make test

# Build release version
make release

# Create distribution package
make package

# Install to local Jellyfin for testing
make install-dev
```

## Build Output

- **Development build**: `Jellyfin.Plugin.Jellypy/bin/Debug/net8.0/publish/`
- **Release build**: `Jellyfin.Plugin.Jellypy/bin/Release/net8.0/publish/`
- **Distribution package**: `dist/jellypy-plugin.zip`
- **Test results**: `TestResults/` (when running with coverage)

## Integration with IDEs

The Makefile commands work seamlessly with:
- VS Code (use integrated terminal)
- JetBrains Rider (use built-in terminal)
- Command line development
- CI/CD pipelines

## Troubleshooting

If you encounter issues:

1. **Check .NET version**: `make check-dotnet`
2. **Clean and rebuild**: `make clean && make dev`
3. **View project info**: `make info`
4. **Check dependencies**: `make restore`

All commands provide colorized output with clear success/error indicators for better visibility during development.
