#!/bin/bash

# JellyPy Plugin Release Script
# Simplified workflow: version -> build -> tag -> GitHub Actions -> manifest update
# No more build.yaml dependency!

set -e  # Exit on any error

# Change to project root (script is in scripts/ directory)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

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

# Function to validate version format
validate_version() {
    local version=$1
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        print_error "Invalid version format: $version"
        print_error "Expected format: x.x.x.x (e.g., 2.1.0.0)"
        exit 1
    fi
}

# Function to get current version from manifest.json
get_current_version() {
    if [ -f "manifest.json" ]; then
        local version=$(jq -r '.[0].versions[0].version' manifest.json 2>/dev/null)
        if [ "$version" != "null" ] && [ -n "$version" ]; then
            echo "$version"
            return 0
        fi
    fi
    echo "0.0.0.0"
}

# Function to update Directory.Build.props
update_directory_build_props() {
    local version=$1
    
    print_status "Updating Directory.Build.props with version $version..."
    
    cat > "Directory.Build.props" << EOF
<Project>
    <PropertyGroup>
        <Version>$version</Version>
        <AssemblyVersion>$version</AssemblyVersion>
        <FileVersion>$version</FileVersion>
    </PropertyGroup>
</Project>
EOF
    
    print_success "Updated Directory.Build.props"
}

# Function to verify CHANGELOG.md has entry
verify_changelog() {
    local version=$1
    
    print_status "Checking CHANGELOG.md for version $version..."
    
    if ! grep -q "\[$version\]" CHANGELOG.md 2>/dev/null; then
        print_warning "Version $version not found in CHANGELOG.md"
        print_warning "Please add a changelog entry before releasing"
        echo ""
        echo -n "Continue anyway? [y/N]: "
        read -r continue_without_changelog
        
        if [[ ! $continue_without_changelog =~ ^[Yy]$ ]]; then
            print_error "Please update CHANGELOG.md before releasing"
            exit 1
        fi
    else
        print_success "CHANGELOG.md contains entry for version $version"
    fi
}

# Function to build and verify
build_and_verify() {
    print_status "Building plugin to verify compilation..."
    
    # Restore dependencies
    if ! dotnet restore Jellyfin.Plugin.JellyPy.sln > /dev/null 2>&1; then
        print_error "Failed to restore dependencies"
        exit 1
    fi
    
    # Build in Release mode
    if ! dotnet build Jellyfin.Plugin.JellyPy.sln --configuration Release --no-restore > /dev/null 2>&1; then
        print_error "Plugin build failed. Fix build errors before releasing."
        exit 1
    fi
    
    print_success "Plugin builds successfully"
}

# Function to collect changelog text for GitHub release
collect_changelog_text() {
    local version=$1
    local changelog_file=$(mktemp)
    
    echo "" >&2
    print_status "Enter release notes for version $version" >&2
    print_status "(These will be used for the GitHub release description)" >&2
    print_status "Press Ctrl+D when done, or Ctrl+C to cancel" >&2
    echo "" >&2
    
    # Collect multi-line input
    cat > "$changelog_file"
    
    # Check if anything was entered
    if [ ! -s "$changelog_file" ]; then
        print_error "No changelog text provided" >&2
        rm "$changelog_file"
        exit 1
    fi
    
    echo "$changelog_file"
}

# Function to show next steps
show_next_steps() {
    local version=$1
    local changelog_file=$2
    
    echo ""
    echo "=========================================="
    print_success "RELEASE PREPARATION COMPLETE"
    echo "=========================================="
    echo "Version: $version"
    echo ""
    echo "Files updated:"
    echo "  • Directory.Build.props (version $version)"
    echo "  • CHANGELOG.md (if updated)"
    echo ""
    echo "Release notes saved to: $changelog_file"
    echo ""
    echo "Next Steps:"
    echo ""
    echo "1. Review changes:"
    echo "   git diff"
    echo ""
    echo "2. Commit version update:"
    echo "   git add Directory.Build.props CHANGELOG.md"
    echo "   git commit -m 'Prepare release v$version'"
    echo ""
    echo "3. Create and push tag:"
    echo "   git tag -a v$version -F $changelog_file"
    echo "   git push origin main"
    echo "   git push origin v$version"
    echo ""
    echo "4. GitHub Actions will automatically:"
    echo "   • Build the plugin for both 10.9 and 10.10"
    echo "   • Create release packages and calculate checksums"
    echo "   • Create GitHub Release with your changelog"
    echo "   • Update manifest.json automatically on the main branch"
    echo ""
    echo "=========================================="
    print_success "Ready to ship!"
    echo "=========================================="
}

# Main function
main() {
    print_status "Starting JellyPy release preparation..."
    
    # Check if we're in the right directory
    if [ ! -f "Jellyfin.Plugin.JellyPy.sln" ]; then
        print_error "Solution file not found. Please run this script from the project root."
        exit 1
    fi
    
    # Get current version
    local current_version=$(get_current_version)
    print_status "Current version in manifest.json: $current_version"
    
    # Get new version
    local version=""
    if [ $# -eq 1 ]; then
        version=$1
        print_status "Using provided version: $version"
    else
        echo ""
        echo -n "Enter new version (x.x.x.x format) [$current_version]: "
        read -r new_version
        
        if [ -z "$new_version" ]; then
            version=$current_version
        else
            version=$new_version
        fi
    fi
    
    # Validate version format
    validate_version "$version"
    
    print_status "Preparing release for version $version"
    
    # Update Directory.Build.props
    update_directory_build_props "$version"
    
    # Verify CHANGELOG.md
    verify_changelog "$version"
    
    # Build and verify
    build_and_verify
    
    # Collect changelog text
    local changelog_file=$(collect_changelog_text "$version")
    
    # Show next steps
    show_next_steps "$version" "$changelog_file"
    
    print_success "Release preparation completed successfully!"
}

# Script entry point
if [ "${BASH_SOURCE[0]}" == "${0}" ]; then
    main "$@"
fi
