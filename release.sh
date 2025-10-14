#!/bin/bash

# Jellypy Plugin Release Script
# This script automates the release process for new versions

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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
    grep '^version:' build.yaml | sed 's/version: *"//' | sed 's/"//'
}

# Function to update version in build.yaml
update_build_version() {
    local new_version=$1
    print_status "Updating build.yaml version to $new_version"
    
    # Create backup
    cp build.yaml build.yaml.bak
    
    # Update version
    sed -i.tmp "s/^version: \".*\"/version: \"$new_version\"/" build.yaml
    rm build.yaml.tmp
    
    print_success "Updated build.yaml version to $new_version"
}

# Function to build the plugin
build_plugin() {
    print_status "Building plugin..."
    
    # Clean previous build
    dotnet clean Jellyfin.Plugin.Jellypy.sln --configuration Release > /dev/null 2>&1
    
    # Build plugin
    dotnet build Jellyfin.Plugin.Jellypy.sln --configuration Release
    
    if [ $? -eq 0 ]; then
        print_success "Plugin built successfully"
    else
        print_error "Plugin build failed"
        exit 1
    fi
}

# Function to create release zip
create_release_zip() {
    local version=$1
    local zip_file="releases/jellypy_$version.zip"
    
    print_status "Creating release zip: $zip_file"
    
    # Ensure releases directory exists
    mkdir -p releases
    
    # Remove existing zip if it exists
    if [ -f "$zip_file" ]; then
        rm "$zip_file"
        print_warning "Removed existing $zip_file"
    fi
    
    # Create zip with only necessary files
    cd Jellyfin.Plugin.Jellypy/bin/Release/net8.0
    
    # Check if required files exist
    required_files=("Jellyfin.Plugin.Jellypy.dll" "Jellyfin.Plugin.Jellypy.deps.json")
    for file in "${required_files[@]}"; do
        if [ ! -f "$file" ]; then
            print_error "Required file $file not found in build output"
            exit 1
        fi
    done
    
    # Create zip
    zip -r "../../../../$zip_file" \
        Jellyfin.Plugin.Jellypy.dll \
        Jellyfin.Plugin.Jellypy.deps.json \
        Jellyfin.Plugin.Jellypy.pdb \
        Jellyfin.Plugin.Jellypy.xml \
        > /dev/null
    
    cd - > /dev/null
    
    if [ -f "$zip_file" ]; then
        print_success "Created $zip_file"
    else
        print_error "Failed to create $zip_file"
        exit 1
    fi
}

# Function to verify release
verify_release() {
    local version=$1
    local zip_file="releases/jellypy_$version.zip"
    
    print_status "Verifying release..."
    
    # Check file exists
    if [ ! -f "$zip_file" ]; then
        print_error "Release file $zip_file not found"
        exit 1
    fi
    
    # Check file size (should be reasonable, not empty, not too large)
    local file_size=$(stat -f%z "$zip_file" 2>/dev/null || stat -c%s "$zip_file" 2>/dev/null)
    local file_size_kb=$((file_size / 1024))
    
    print_status "File size: ${file_size_kb}KB"
    
    if [ $file_size -lt 10000 ]; then  # Less than 10KB
        print_error "Release file seems too small ($file_size_kb KB)"
        exit 1
    fi
    
    if [ $file_size -gt 10485760 ]; then  # More than 10MB
        print_warning "Release file seems large ($file_size_kb KB)"
    fi
    
    # List contents of zip
    print_status "Release contents:"
    unzip -l "$zip_file"
    
    print_success "Release verification completed"
}

# Function to calculate checksum
calculate_checksum() {
    local version=$1
    local zip_file="releases/jellypy_$version.zip"
    
    print_status "Calculating MD5 checksum..."
    
    # Calculate MD5 checksum
    local checksum=""
    if command -v md5sum >/dev/null 2>&1; then
        checksum=$(md5sum "$zip_file" | cut -d' ' -f1)
    elif command -v md5 >/dev/null 2>&1; then
        checksum=$(md5 -q "$zip_file")
    else
        print_error "No MD5 utility found (tried md5sum and md5)"
        exit 1
    fi
    
    print_success "Checksum: $checksum"
    echo "$checksum"
}

# Function to update manifest checksum
update_manifest_checksum() {
    local version=$1
    local checksum=$2
    local manifest_file=""
    
    # Determine which manifest to update based on current branch
    local current_branch=$(git branch --show-current)
    
    if [ "$current_branch" = "beta" ]; then
        manifest_file="manifest-beta.json"
    else
        manifest_file="manifest.json"
    fi
    
    if [ ! -f "$manifest_file" ]; then
        print_error "Manifest file $manifest_file not found"
        exit 1
    fi
    
    print_status "Updating checksum in $manifest_file"
    
    # Create backup
    cp "$manifest_file" "$manifest_file.bak"
    
    # Update checksum for the specific version
    # This is a bit complex as we need to update the right version entry
    python3 -c "
import json
import sys

try:
    with open('$manifest_file', 'r') as f:
        data = json.load(f)
    
    updated = False
    for plugin in data:
        for version in plugin['versions']:
            if version['version'] == '$version':
                version['checksum'] = '$checksum'
                updated = True
                print(f'Updated checksum for version $version')
                break
    
    if not updated:
        print(f'Version $version not found in manifest')
        sys.exit(1)
    
    with open('$manifest_file', 'w') as f:
        json.dump(data, f, indent=4)
    
    print('Manifest updated successfully')
except Exception as e:
    print(f'Error updating manifest: {e}')
    sys.exit(1)
"
    
    if [ $? -eq 0 ]; then
        print_success "Updated checksum in $manifest_file"
        rm "$manifest_file.bak"
    else
        print_error "Failed to update manifest"
        mv "$manifest_file.bak" "$manifest_file"
        exit 1
    fi
}

# Function to show release summary
show_release_summary() {
    local version=$1
    local checksum=$2
    local current_branch=$(git branch --show-current)
    
    echo ""
    echo "=========================================="
    print_success "RELEASE SUMMARY"
    echo "=========================================="
    echo "Version: $version"
    echo "Branch: $current_branch"
    echo "File: releases/jellypy_$version.zip"
    echo "Checksum: $checksum"
    if [ "$current_branch" = "beta" ]; then
        echo "Download URL: https://raw.githubusercontent.com/caleb-venner/jellypy/beta/releases/jellypy_$version.zip"
        echo "Repository URL: https://caleb-venner.github.io/jellypy/manifest-beta.json"
    else
        echo "Download URL: https://raw.githubusercontent.com/caleb-venner/jellypy/main/releases/jellypy_$version.zip"
        echo "Repository URL: https://caleb-venner.github.io/jellypy/manifest.json"
    fi
    
    echo ""
    echo "Next steps:"
    echo "1. Review the changes with 'git diff'"
    echo "2. Commit the changes: git add . && git commit -m 'Release $version'"
    echo "3. Push to GitHub: git push origin $current_branch"
    echo "4. Plugin will be available immediately via repository URL"
    echo "5. No GitHub release creation needed - files served from /releases directory"
    echo "=========================================="
}

# Main function
main() {
    print_status "Starting Jellypy release process..."
    
    # Check if we're in the right directory
    if [ ! -f "build.yaml" ]; then
        print_error "build.yaml not found. Please run this script from the project root directory."
        exit 1
    fi
    
    # Get version argument or current version
    local version=""
    if [ $# -eq 1 ]; then
        version=$1
        print_status "Using provided version: $version"
    else
        version=$(get_current_version)
        print_status "Using current version from build.yaml: $version"
        
        # Ask if user wants to increment version
        echo -n "Do you want to update the version? (current: $version) [y/N]: "
        read -r update_version
        
        if [[ $update_version =~ ^[Yy]$ ]]; then
            echo -n "Enter new version (x.x.x.x format): "
            read -r new_version
            
            # Validate version format
            if [[ ! $new_version =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
                print_error "Invalid version format. Use x.x.x.x format (e.g., 1.1.2.0)"
                exit 1
            fi
            
            version=$new_version
            update_build_version "$version"
        fi
    fi
    
    # Validate version format
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format: $version. Use x.x.x.x format (e.g., 1.1.2.0)"
        exit 1
    fi
    
    print_status "Creating release for version $version"
    
    # Execute release steps
    build_plugin
    create_release_zip "$version"
    verify_release "$version"
    local checksum=$(calculate_checksum "$version")
    update_manifest_checksum "$version" "$checksum"
    
    # Show summary
    show_release_summary "$version" "$checksum"
    
    print_success "Release process completed successfully!"
}

# Script entry point
if [ "${BASH_SOURCE[0]}" == "${0}" ]; then
    main "$@"
fi