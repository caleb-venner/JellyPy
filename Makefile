# JellyPy Jellyfin Plugin Makefile
# Provides convenient targets for development workflow

# Configuration
PLUGIN_NAME := Jellyfin.Plugin.JellyPy
SOLUTION_FILE := $(PLUGIN_NAME).sln
PROJECT_DIR := $(PLUGIN_NAME)
BUILD_DIR := $(PROJECT_DIR)/bin
PUBLISH_DIR := $(BUILD_DIR)/Release/net8.0/publish
DEV_PUBLISH_DIR := $(BUILD_DIR)/Debug/net8.0/publish
TEST_PROJECT := $(PLUGIN_NAME).Tests

# Colors for output
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color
BOLD := \033[1m

# Default target
.PHONY: help
help: ## Show this help message
	@echo "$(BOLD)JellyPy Plugin Build System$(NC)"
	@echo "============================="
	@echo
	@echo "$(BOLD)Available targets:$(NC)"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2}' $(MAKEFILE_LIST)
	@echo
	@echo "$(BOLD)Examples:$(NC)"
	@echo "  make dev          # Build for development and copy to local Jellyfin"
	@echo "  make release      # Build optimized release version"
	@echo "  make test         # Run all tests"
	@echo "  make lint         # Run code analysis and formatting checks"
	@echo "  make clean        # Clean all build artifacts"

.PHONY: check-dotnet
check-dotnet: ## Check if dotnet is installed
	@command -v dotnet >/dev/null 2>&1 || { echo "$(RED)❌ dotnet is not installed$(NC)"; exit 1; }

.PHONY: restore
restore: check-dotnet ## Restore NuGet packages
	@echo "$(YELLOW)📦 Restoring NuGet packages...$(NC)"
	@dotnet restore $(SOLUTION_FILE)

.PHONY: dev
dev: restore ## Build for dev 
	@echo "$(YELLOW)🔨 Building for dev...$(NC)"
	@dotnet build $(SOLUTION_FILE) --configuration=Release --no-restore
	@echo "$(YELLOW)📦 Publishing release build...$(NC)"
	@dotnet publish $(PROJECT_DIR)/$(PLUGIN_NAME).csproj --configuration=Release --no-build --output $(PUBLISH_DIR)
	@echo "$(GREEN)✅ Release build complete!$(NC)"
	@echo "$(YELLOW)📁 Output: $(PUBLISH_DIR)$(NC)"

.PHONY: test-project
test-project: ## Create test project if it doesn't exist
	@if [ ! -d "$(TEST_PROJECT)" ]; then \
		echo "$(YELLOW)🧪 Creating test project...$(NC)"; \
		dotnet new xunit -n $(TEST_PROJECT); \
		echo '<Project Sdk="Microsoft.NET.Sdk">' > $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  <PropertyGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <TargetFramework>net8.0</TargetFramework>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <ImplicitUsings>enable</ImplicitUsings>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <Nullable>enable</Nullable>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <IsPackable>false</IsPackable>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  </PropertyGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  <ItemGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <PackageReference Include="xunit" Version="2.6.2" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <PackageReference Include="coverlet.collector" Version="6.0.0" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <PackageReference Include="Moq" Version="4.20.69" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  </ItemGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  <ItemGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '    <ProjectReference Include="../$(PROJECT_DIR)/$(PLUGIN_NAME).csproj" />' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '  </ItemGroup>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		echo '</Project>' >> $(TEST_PROJECT)/$(TEST_PROJECT).csproj; \
		dotnet sln add $(TEST_PROJECT); \
		echo "$(GREEN)✅ Test project created!$(NC)"; \
	else \
		echo "$(GREEN)✅ Test project already exists$(NC)"; \
	fi

.PHONY: test
test: restore test-project ## Run all tests
	@echo "$(YELLOW)🧪 Running tests...$(NC)"
	@if [ -d "$(TEST_PROJECT)" ]; then \
		dotnet test $(SOLUTION_FILE) --configuration=Debug --no-restore --verbosity=normal; \
	else \
		echo "$(YELLOW)⚠️  No tests found. Run 'make test-project' to create a test project.$(NC)"; \
	fi

.PHONY: lint
lint: restore ## Run code analysis and style checks
	@echo "$(YELLOW)🔍 Running code analysis...$(NC)"
	@echo "$(YELLOW)   StyleCop, Code Analysis, and Formatting checks$(NC)"
	@dotnet clean $(SOLUTION_FILE) --verbosity=quiet
	@dotnet build $(SOLUTION_FILE) --configuration=Debug --no-restore
	@echo "$(YELLOW)📝 Checking markdown formatting...$(NC)"
	@markdownlint "**/*.md" --ignore-path .markdownlintignore --config .markdownlint.json || true
	@echo "$(GREEN)✅ Linting complete!$(NC)"
	@echo "$(YELLOW)💡 Review warnings above for style improvements$(NC)"

.PHONY: clean
clean: ## Clean all build artifacts
	@echo "$(YELLOW)🧹 Cleaning build artifacts...$(NC)"
	@dotnet clean $(SOLUTION_FILE)
	@rm -rf $(BUILD_DIR)
	@rm -rf TestResults
	@echo "$(GREEN)✅ Clean complete!$(NC)"

.PHONY: release
release: ## Prepare version for automated GitHub Actions release
	@echo "$(YELLOW)📦 Preparing release with scripts/release.sh...$(NC)"
	@bash scripts/release.sh
	@echo "$(GREEN)✅ Release preparation complete!$(NC)"

.PHONY: info
info: ## Show project information
	@echo "$(BOLD)JellyPy Plugin Information$(NC)"
	@echo "=========================="
	@echo "$(YELLOW)Project:$(NC)      $(PLUGIN_NAME)"
	@echo "$(YELLOW)Solution:$(NC)     $(SOLUTION_FILE)"
	@echo "$(YELLOW)Framework:$(NC)    .NET 8.0"
	@echo "$(YELLOW)Build Dir:$(NC)    $(BUILD_DIR)"
	@echo "$(YELLOW)Dev Output:$(NC)   $(DEV_PUBLISH_DIR)"
	@echo "$(YELLOW)Release Output:$(NC) $(PUBLISH_DIR)"
	@if [ -d "$(TEST_PROJECT)" ]; then \
		echo "$(YELLOW)Tests:$(NC)        $(GREEN)✅ Available$(NC)"; \
	else \
		echo "$(YELLOW)Tests:$(NC)        $(RED)❌ Not created (run 'make test-project')$(NC)"; \
	fi

# Prevent make from interpreting files as targets
.PHONY: all
all: dev ## Build everything (same as dev)
