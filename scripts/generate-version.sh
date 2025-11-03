#!/bin/bash
# Script to parse build.yaml and generate Directory.Build.props
# This ensures version consistency between build.yaml (source of truth) and MSBuild configuration

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
BUILD_YAML="$PROJECT_ROOT/build.yaml"
BUILD_PROPS="$PROJECT_ROOT/Directory.Build.props"

# Check if build.yaml exists
if [ ! -f "$BUILD_YAML" ]; then
    echo -e "${RED}❌ Error: build.yaml not found at $BUILD_YAML${NC}"
    exit 1
fi

# Extract version from build.yaml using grep and sed
# Look for line like: version: "2.1.0.0"
VERSION=$(grep -E '^\s*version:\s*"[0-9.]+"\s*$' "$BUILD_YAML" | sed 's/^[^"]*"\([^"]*\)".*/\1/')

if [ -z "$VERSION" ]; then
    echo -e "${RED}❌ Error: Could not extract version from build.yaml${NC}"
    exit 1
fi

echo -e "${YELLOW}📝 Generating Directory.Build.props with version: $VERSION${NC}"

# Generate Directory.Build.props
cat > "$BUILD_PROPS" << EOF
<Project>
    <PropertyGroup>
        <Version>$VERSION</Version>
        <AssemblyVersion>$VERSION</AssemblyVersion>
        <FileVersion>$VERSION</FileVersion>
    </PropertyGroup>
</Project>
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Successfully generated Directory.Build.props${NC}"
else
    echo -e "${RED}❌ Failed to generate Directory.Build.props${NC}"
    exit 1
fi
