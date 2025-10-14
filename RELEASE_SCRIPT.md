# Release Script Documentation

## Overview
The `release.sh` script automates the complete release process for Jellypy plugin versions.

## Features
- Updates build.yaml version
- Builds the plugin using dotnet
- Creates optimized release zip files
- Verifies release integrity  
- Calculates MD5 checksums
- Updates manifest checksums automatically
- Supports both stable and beta branches

## Usage

### Basic Usage (Current Version)
```bash
./release.sh
```
This will use the current version from build.yaml and ask if you want to update it.

### Specify New Version
```bash
./release.sh 1.1.3.0
```
This will update build.yaml to the specified version and create the release.

### Interactive Mode
The script will:
1. Show current version from build.yaml
2. Ask if you want to update the version
3. Validate version format (must be x.x.x.x)
4. Execute all release steps
5. Show a summary with next steps

## What It Does

### 1. Version Management
- Reads current version from build.yaml
- Updates version if requested
- Validates 4-digit version format (x.x.x.x)

### 2. Build Process
- Cleans previous Release build
- Builds plugin using dotnet build
- Verifies build succeeded

### 3. Package Creation
- Creates releases/ directory if needed
- Removes existing zip if present
- Packages only necessary files:
  - Jellyfin.Plugin.Jellypy.dll
  - Jellyfin.Plugin.Jellypy.deps.json
  - Jellyfin.Plugin.Jellypy.pdb
  - Jellyfin.Plugin.Jellypy.xml

### 4. Verification
- Checks file exists and has reasonable size
- Lists zip contents for review
- Warns if file size is suspicious

### 5. Checksum Calculation
- Calculates MD5 checksum
- Compatible with both md5sum (Linux) and md5 (macOS)

### 6. Manifest Updates
- Automatically detects current branch
- Updates manifest-beta.json on beta branch
- Updates manifest.json on main branch
- Updates checksum for the specific version
- Creates backup before modification

## Branch Behavior
- **beta branch**: Updates manifest-beta.json
- **main branch**: Updates manifest.json
- Automatically detects branch and uses appropriate manifest

## Output
The script provides:
- Colored status messages for easy reading
- Detailed progress information
- Release summary with all necessary information
- Next steps for completing the release

## Error Handling
- Exits on any build errors
- Validates all required files exist
- Creates backups before modifying files
- Restores backups on failure

## Example Output
```
[INFO] Starting Jellypy release process...
[INFO] Using current version from build.yaml: 1.1.2.0
[INFO] Creating release for version 1.1.2.0
[INFO] Building plugin...
[SUCCESS] Plugin built successfully
[INFO] Creating release zip: releases/jellypy_1.1.2.0.zip
[SUCCESS] Created releases/jellypy_1.1.2.0.zip
[INFO] Verifying release...
[INFO] File size: 89KB
[SUCCESS] Release verification completed
[INFO] Calculating MD5 checksum...
[SUCCESS] Checksum: 6d070d6f8f0ef97ca8f60c5cf1e08ab8
[INFO] Updating checksum in manifest-beta.json
[SUCCESS] Updated checksum in manifest-beta.json
[SUCCESS] Release process completed successfully!
```

## Requirements
- Bash shell
- .NET SDK (for dotnet build)
- Python 3 (for JSON manipulation)
- zip utility
- md5sum or md5 utility
- git (for branch detection)

## Files Modified
- build.yaml (version update)
- manifest.json or manifest-beta.json (checksum update)
- releases/jellypy_x.x.x.x.zip (created)