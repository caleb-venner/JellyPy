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
check-dotnet: ## Check if .NET SDK is installed
	@if ! command -v dotnet >/dev/null 2>&1; then \
		echo "$(RED)❌ Error: .NET SDK not found$(NC)"; \
		echo "   Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"; \
		exit 1; \
	fi
	@echo "$(GREEN)✅ .NET SDK found:$(NC) $$(dotnet --version)"

.PHONY: restore
restore: check-dotnet ## Restore NuGet packages
	@echo "$(YELLOW)📦 Restoring NuGet packages...$(NC)"
	@dotnet restore $(SOLUTION_FILE)

.PHONY: dev
dev: restore ## Build for development (Debug configuration)
	@echo "$(YELLOW)🔨 Building for development...$(NC)"
	@dotnet build $(SOLUTION_FILE) --configuration=Debug --no-restore
	@echo "$(GREEN)✅ Development build complete!$(NC)"
	@echo "$(YELLOW)📁 Output: $(DEV_PUBLISH_DIR)$(NC)"

.PHONY: dev-publish
dev-publish: dev ## Build and publish development version
	@echo "$(YELLOW)📦 Publishing development build...$(NC)"
	@dotnet publish $(PROJECT_DIR)/$(PLUGIN_NAME).csproj --configuration=Debug --no-build --output $(DEV_PUBLISH_DIR)
	@echo "$(GREEN)✅ Development publish complete!$(NC)"

.PHONY: release
release: restore ## Build for release (Release configuration)
	@echo "$(YELLOW)🔨 Building for release...$(NC)"
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

.PHONY: test-coverage
test-coverage: restore test-project ## Run tests with code coverage
	@echo "$(YELLOW)🧪 Running tests with coverage...$(NC)"
	@if [ -d "$(TEST_PROJECT)" ]; then \
		dotnet test $(SOLUTION_FILE) --configuration=Debug --no-restore \
			--collect:"XPlat Code Coverage" \
			--results-directory:./TestResults \
			--verbosity=normal; \
		echo "$(GREEN)✅ Coverage report generated in TestResults/$(NC)"; \
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

.PHONY: format
format: restore ## Format code using dotnet format
	@echo "$(YELLOW)✨ Formatting code...$(NC)"
	@dotnet format $(SOLUTION_FILE) --verbosity=diagnostic
	@echo "$(GREEN)✅ Code formatting complete!$(NC)"

.PHONY: format-check
format-check: restore ## Check if code is properly formatted
	@echo "$(YELLOW)🔍 Checking code formatting...$(NC)"
	@dotnet format $(SOLUTION_FILE) --verify-no-changes --verbosity=diagnostic

.PHONY: clean
clean: ## Clean all build artifacts
	@echo "$(YELLOW)🧹 Cleaning build artifacts...$(NC)"
	@dotnet clean $(SOLUTION_FILE)
	@rm -rf $(BUILD_DIR)
	@rm -rf TestResults
	@echo "$(GREEN)✅ Clean complete!$(NC)"

.PHONY: install-dev
install-dev: dev-publish ## Build and install plugin to local Jellyfin (development)
	@echo "$(YELLOW)📦 Installing development build to Jellyfin...$(NC)"
	@$(MAKE) -s install-to-jellyfin BUILD_TYPE=Debug

.PHONY: install-release
install-release: release ## Build and install plugin to Jellyfin (release)
	@echo "$(YELLOW)📦 Installing release build to Jellyfin...$(NC)"
	@$(MAKE) -s install-to-jellyfin BUILD_TYPE=Release

.PHONY: install-to-jellyfin
install-to-jellyfin: ## Internal target for Jellyfin installation
	@JELLYFIN_PLUGIN_DIR=""; \
	for dir in "/var/lib/jellyfin/plugins" "/usr/lib/jellyfin/plugins" "$$HOME/.local/share/jellyfin/plugins" "/opt/jellyfin/plugins" "/config/plugins"; do \
		if [ -d "$$dir" ]; then \
			JELLYFIN_PLUGIN_DIR="$$dir"; \
			break; \
		fi; \
	done; \
	if [ -z "$$JELLYFIN_PLUGIN_DIR" ]; then \
		echo "$(RED)❌ Jellyfin plugin directory not found$(NC)"; \
		echo "   Please install Jellyfin or specify JELLYFIN_PLUGIN_DIR"; \
		exit 1; \
	fi; \
	PLUGIN_DIR="$$JELLYFIN_PLUGIN_DIR/JellyPy"; \
	mkdir -p "$$PLUGIN_DIR"; \
	if [ "$(BUILD_TYPE)" = "Release" ]; then \
		cp -r $(PUBLISH_DIR)/* "$$PLUGIN_DIR/"; \
	else \
		cp -r $(DEV_PUBLISH_DIR)/* "$$PLUGIN_DIR/"; \
	fi; \
	echo "$(GREEN)✅ Plugin installed to: $$PLUGIN_DIR$(NC)"; \
	echo "$(YELLOW)🔄 Please restart Jellyfin to load the updated plugin$(NC)"

.PHONY: watch
watch: restore ## Watch for changes and rebuild automatically
	@echo "$(YELLOW)👀 Watching for changes (Ctrl+C to stop)...$(NC)"
	@dotnet watch --project $(PROJECT_DIR) build

.PHONY: package
package: release ## Create a plugin package for distribution
	@echo "$(YELLOW)📦 Creating plugin package...$(NC)"
	@mkdir -p dist
	@cd "$(PUBLISH_DIR)" && zip -r ../../../../../dist/jellypy-plugin.zip .
	@echo "$(GREEN)✅ Package created: dist/jellypy-plugin.zip$(NC)"

.PHONY: check-style
check-style: restore ## Run style checks only
	@echo "$(YELLOW)🎨 Checking code style...$(NC)"
	@dotnet build $(SOLUTION_FILE) --configuration=Debug --no-restore --verbosity=quiet /p:TreatWarningsAsErrors=false

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
