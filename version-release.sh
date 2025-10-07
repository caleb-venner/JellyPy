#!/bin/bash

# Jellyfin Plugin Versioning and Release Script
# Usage: ./version-release.sh [patch|minor|major] [message]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
PLUGIN_NAME="jellypy"
BUILD_DIR="Jellyfin.Plugin.Jellypy/bin/Release/net8.0"
RELEASE_DIR="releases"

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to get current version from build.yaml
get_current_version() {
    grep '^version:' build.yaml | sed 's/version: "\(.*\)"/\1/' | tr -d '"'
}

# Function to increment version
increment_version() {
    local version=$1
    local bump_type=$2

    # Split version into parts (remove .0 suffix for processing)
    local base_version=$(echo $version | sed 's/\.0$//')
    IFS='.' read -ra VERSION_PARTS <<< "$base_version"

    local major=${VERSION_PARTS[0]}
    local minor=${VERSION_PARTS[1]}
    local patch=${VERSION_PARTS[2]}

    case $bump_type in
        "major")
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        "minor")
            minor=$((minor + 1))
            patch=0
            ;;
        "patch")
            patch=$((patch + 1))
            ;;
        *)
            print_error "Invalid bump type. Use: patch, minor, or major"
            exit 1
            ;;
    esac

    echo "${major}.${minor}.${patch}.0"
}

# Function to update version in files
update_version_files() {
    local new_version=$1
    local short_version=$(echo $new_version | sed 's/\.0$//')

    print_status "Updating version to $new_version in project files..."

    # Update build.yaml
    sed -i '' "s/^version: \".*\"/version: \"$new_version\"/" build.yaml

    # Update manifest.json version (add new version to array)
    # This is more complex - we'll handle it separately
    print_status "build.yaml updated"
}

# Function to build and package
build_package() {
    local version=$1
    local package_name="${PLUGIN_NAME}_${version}.zip"

    print_status "Building plugin package v${version}..."

    # Clean and build
    dotnet clean --configuration Release >/dev/null 2>&1
    dotnet publish --configuration Release --framework net8.0 >/dev/null 2>&1

    # Create release directory
    mkdir -p "${RELEASE_DIR}"

    # Create zip package
    cd "${BUILD_DIR}"
    zip -q -r "../../../../${RELEASE_DIR}/${package_name}" \
        Jellyfin.Plugin.Jellypy.dll \
        Jellyfin.Plugin.Jellypy.xml \
        Jellyfin.Plugin.Jellypy.deps.json
    cd - >/dev/null

    # Calculate checksum
    if command -v md5 &> /dev/null; then
        local checksum=$(md5 -q "${RELEASE_DIR}/${package_name}")
    elif command -v md5sum &> /dev/null; then
        local checksum=$(md5sum "${RELEASE_DIR}/${package_name}" | cut -d' ' -f1)
    else
        print_error "No MD5 command found"
        exit 1
    fi

    echo "$checksum:$package_name"
}

# Function to update manifest.json
update_manifest() {
    local version=$1
    local checksum=$2
    local package_name=$3
    local commit_message=$4
    local short_version=$(echo $version | sed 's/\.0$//')
    local timestamp=$(date -u +%Y-%m-%dT%H:%M:%SZ)

    print_status "Updating manifest.json..."

    if [[ ! -f "manifest.json" ]]; then
        print_error "manifest.json not found"
        exit 1
    fi

    # Create new version entry JSON
    local new_version_json=$(cat <<EOF
            {
                "version": "$version",
                "changelog": "$commit_message",
                "targetAbi": "10.9.0.0",
                "sourceUrl": "https://github.com/caleb-venner/jellypy/releases/download/v$short_version/$package_name",
                "checksum": "$checksum",
                "timestamp": "$timestamp"
            }
EOF
    )

    # Create a temporary file for the updated manifest
    local temp_file=$(mktemp)

    # Use Python to properly update the JSON (more reliable than sed)
    python3 << PYTHON_EOF
import json
import sys

# Read the current manifest
with open('manifest.json', 'r') as f:
    manifest = json.load(f)

# Create new version entry
new_version = {
    "version": "${version}",
    "changelog": "${commit_message}",
    "targetAbi": "10.9.0.0",
    "sourceUrl": "https://github.com/caleb-venner/jellypy/releases/download/v${short_version}/${package_name}",
    "checksum": "${checksum}",
    "timestamp": "${timestamp}"
}

# Add new version to the beginning of versions array
manifest[0]['versions'].insert(0, new_version)

# Write updated manifest
with open('${temp_file}', 'w') as f:
    json.dump(manifest, f, indent=4)
    f.write('\n')  # Add trailing newline
PYTHON_EOF

    if [[ $? -eq 0 ]]; then
        mv "$temp_file" manifest.json
        print_success "manifest.json updated with version $version"
    else
        print_error "Failed to update manifest.json"
        rm -f "$temp_file"
        exit 1
    fi
}

# Main script
main() {
    local bump_type=${1:-"patch"}
    local commit_message=${2:-"Version bump"}

    if [[ ! -f "build.yaml" ]]; then
        print_error "build.yaml not found. Run from project root directory."
        exit 1
    fi

    # Get current version
    local current_version=$(get_current_version)
    print_status "Current version: $current_version"

    # Calculate new version
    local new_version=$(increment_version "$current_version" "$bump_type")
    local short_version=$(echo $new_version | sed 's/\.0$//')
    print_status "New version: $new_version"

    # Confirm with user
    echo
    print_warning "This will:"
    echo "  - Update version from $current_version to $new_version"
    echo "  - Build and package the plugin"
    echo "  - Update manifest.json with new version"
    echo "  - Create git commit and tags"
    echo
    read -p "Continue? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_status "Cancelled."
        exit 0
    fi

    # Update version files
    update_version_files "$new_version"

    # Build package
    local build_result=$(build_package "$new_version")
    local checksum=$(echo "$build_result" | cut -d':' -f1)
    local package_name=$(echo "$build_result" | cut -d':' -f2)

    print_success "Package created: ${RELEASE_DIR}/${package_name}"
    print_success "Checksum: $checksum"

    # Update manifest
    update_manifest "$new_version" "$checksum" "$package_name" "$commit_message"

    # Git operations
    print_status "Creating git commit and tags..."
    git add build.yaml manifest.json "${RELEASE_DIR}/${package_name}"
    git commit -m "chore: Bump version to $new_version

$commit_message"

    # Create tags
    git tag "v$new_version"
    git tag "v$short_version"

    print_success "Version $new_version ready!"
    echo
    print_status "Next steps:"
    echo "  1. git push origin main"
    echo "  2. git push origin v$new_version v$short_version"
    echo "  3. Create GitHub release and upload $package_name"
    echo ""
    print_success "All files updated automatically:"
    echo "  ✅ build.yaml version updated"
    echo "  ✅ Plugin package built: ${RELEASE_DIR}/${package_name}"
    echo "  ✅ manifest.json updated with new version"
    echo "  ✅ Git commit and tags created"
}

# Show help
if [[ "$1" == "-h" || "$1" == "--help" ]]; then
    echo "Usage: $0 [patch|minor|major] [\"commit message\"]"
    echo
    echo "Examples:"
    echo "  $0 patch \"Fix configuration bug\""
    echo "  $0 minor \"Add new Radarr features\""
    echo "  $0 major \"Breaking API changes\""
    echo
    echo "Version format: MAJOR.MINOR.PATCH.0 (Jellyfin convention)"
    exit 0
fi

main "$@"
