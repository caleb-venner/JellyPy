#!/bin/bash

# Build and package Jellyfin plugin for distribution
# Usage: ./build-release.sh [version]

set -e

VERSION=${1:-"1.0.0.0"}
PLUGIN_NAME="jellypy"
BUILD_DIR="Jellyfin.Plugin.Jellypy/bin/Release/net8.0"
RELEASE_DIR="releases"
PACKAGE_NAME="${PLUGIN_NAME}_${VERSION}.zip"

echo "Building Jellyfin.Plugin.Jellypy release package v${VERSION}..."

# Clean and build
echo "Cleaning previous builds..."
dotnet clean --configuration Release

echo "Building plugin..."
dotnet publish --configuration Release --framework net8.0

# Create release directory
mkdir -p "${RELEASE_DIR}"

# Create zip package
echo "Creating release package..."
cd "${BUILD_DIR}"
zip -r "../../../../${RELEASE_DIR}/${PACKAGE_NAME}" \
    Jellyfin.Plugin.Jellypy.dll \
    Jellyfin.Plugin.Jellypy.xml \
    Jellyfin.Plugin.Jellypy.deps.json

cd - > /dev/null

# Calculate MD5 checksum
echo "Calculating checksum..."
if command -v md5sum &> /dev/null; then
    CHECKSUM=$(md5sum "${RELEASE_DIR}/${PACKAGE_NAME}" | cut -d' ' -f1)
elif command -v md5 &> /dev/null; then
    CHECKSUM=$(md5 -q "${RELEASE_DIR}/${PACKAGE_NAME}")
else
    echo "Warning: Neither md5sum nor md5 command found. Please calculate checksum manually."
    CHECKSUM="CALCULATE_MANUALLY"
fi

echo ""
echo "✅ Release package created: ${RELEASE_DIR}/${PACKAGE_NAME}"
echo "📦 Size: $(du -h "${RELEASE_DIR}/${PACKAGE_NAME}" | cut -f1)"
echo "🔒 MD5 Checksum: ${CHECKSUM}"
echo ""
echo "Update your manifest.json with:"
echo "  \"checksum\": \"${CHECKSUM}\""
echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\""
echo ""
echo "Next steps:"
echo "1. Upload ${PACKAGE_NAME} to GitHub releases"
echo "2. Update sourceUrl in manifest.json with the GitHub release URL"
echo "3. Update checksum and timestamp in manifest.json"
echo "4. Host manifest.json somewhere accessible (GitHub Pages, etc.)"
