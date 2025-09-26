# JellyPy Plugin Improvements Plan

## Immediate Fixes Required

### 1. Configuration Inconsistencies

- [ ] Fix plugin name in `.vscode/settings.json` from "Template" to "Jellypy"
- [ ] Update `build.yaml` artifact name to match actual DLL name
- [ ] Fix HTML page title in `configPage.html`
- [ ] Fix constructor documentation in `Plugin.cs`

### 2. Build System Issues

- [ ] Ensure tasks.json references correct solution file name
- [ ] Add proper error handling in build tasks

## Architectural Redesign for Event System

### Current Limitations

- Only handles PlaybackStart events
- Hardcoded to Python execution
- Tightly coupled to specific external services (Sonarr/Radarr)
- No support for multiple scripts per event
- No conditional execution based on event context

### Proposed Event-Driven Architecture

```csharp
// Event Types Enumeration
public enum SupportedEventType
{
    PlaybackStart,
    PlaybackStop,
    PlaybackPause,
    PlaybackResume,
    ItemAdded,
    ItemUpdated,
    ItemRemoved,
    UserCreated,
    UserDeleted,
    SessionStart,
    SessionEnd,
    ServerStartup,
    ServerShutdown
}

// Script Configuration Model
public class ScriptConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public SupportedEventType EventType { get; set; }
    public ScriptExecutorType ExecutorType { get; set; } = ScriptExecutorType.Python;
    public string ExecutablePath { get; set; } = string.Empty;
    public string ScriptPath { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public List<ExecutionCondition> Conditions { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 300;
    public int Priority { get; set; } = 0; // For execution ordering
}

// Execution Conditions
public class ExecutionCondition
{
    public ConditionType Type { get; set; }
    public string Property { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}

public enum ConditionType
{
    MediaType,      // Movie, Episode, Music, etc.
    UserName,       // Specific user
    LibraryName,    // Specific library
    GenreName,      // Content genre
    Custom          // Custom property
}

public enum ConditionOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    Regex
}
```

### 3. Enhanced Event Handler System

```csharp
// Generic Event Handler Interface
public interface IEventHandler<T>
{
    Task HandleAsync(T eventArgs, CancellationToken cancellationToken = default);
    bool CanHandle(T eventArgs);
}

// Script Executor Factory
public interface IScriptExecutorFactory
{
    IScriptExecutor CreateExecutor(ScriptExecutorType type);
}

public enum ScriptExecutorType
{
    Python,
    PowerShell,
    Bash,
    Node,
    Binary
}

// Enhanced Script Executor
public interface IScriptExecutor
{
    Task<ScriptExecutionResult> ExecuteAsync(
        ScriptConfiguration config,
        Dictionary<string, object> eventData,
        CancellationToken cancellationToken = default);
}

public class ScriptExecutionResult
{
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
}
```

### 4. Event Data Serialization

```csharp
// Event Data Provider
public interface IEventDataProvider
{
    Dictionary<string, object> ExtractEventData(object eventArgs);
    string SerializeForScript(Dictionary<string, object> data, SerializationFormat format);
}

public enum SerializationFormat
{
    Json,
    Xml,
    EnvironmentVariables,
    CommandLineArguments
}
```

## Configuration Improvements

### Enhanced Plugin Configuration

```csharp
public class PluginConfiguration : BasePluginConfiguration
{
    public List<ScriptConfiguration> Scripts { get; set; } = new();
    public GlobalScriptSettings GlobalSettings { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
}

public class GlobalScriptSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 300;
    public int MaxConcurrentScripts { get; set; } = 5;
    public bool EnableScriptLogging { get; set; } = true;
    public string LogDirectory { get; set; } = "/config/logs/scripts";
    public RetryPolicy RetryPolicy { get; set; } = new();
}

public class RetryPolicy
{
    public bool Enabled { get; set; } = true;
    public int MaxAttempts { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 1000;
    public RetryStrategy Strategy { get; set; } = RetryStrategy.Linear;
}
```

## Security Improvements

### 1. Input Validation
- [ ] Sanitize all script paths and arguments
- [ ] Validate executable permissions
- [ ] Implement script signature verification option
- [ ] Add sandbox execution option

### 2. Credential Management

- [ ] Encrypt sensitive configuration data
- [ ] Support external secret providers (Azure Key Vault, etc.)
- [ ] Implement credential rotation capabilities

## Performance Optimizations

### 1. Execution Management

- [ ] Implement proper connection pooling for external services
- [ ] Add script result caching where appropriate
- [ ] Use background queues for non-critical script execution
- [ ] Implement circuit breaker pattern for failing scripts

### 2. Resource Management

- [ ] Replace static semaphore with proper dependency injection
- [ ] Implement script execution quotas per user/library
- [ ] Add memory and CPU usage monitoring

## Testing Strategy

### 1. Unit Tests

```csharp
// Example test structure
[TestClass]
public class ScriptExecutorTests
{
    [TestMethod]
    public async Task ExecuteAsync_ValidPythonScript_ReturnsSuccess()
    {
        // Arrange
        var config = new ScriptConfiguration
        {
            ExecutorType = ScriptExecutorType.Python,
            ScriptPath = "/path/to/test_script.py"
        };
        var eventData = new Dictionary<string, object>
        {
            ["mediaType"] = "Movie",
            ["title"] = "Test Movie"
        };

        // Act & Assert
        var result = await _scriptExecutor.ExecuteAsync(config, eventData);
        Assert.IsTrue(result.Success);
    }
}
```

### 2. Integration Tests

- [ ] Test with actual Jellyfin instance
- [ ] Validate different event types
- [ ] Test script failure scenarios
- [ ] Performance testing with concurrent executions

## Documentation Requirements

### 1. API Documentation

- [ ] Document all event types and their available data
- [ ] Provide script development guidelines
- [ ] Create example scripts for common scenarios

### 2. Configuration Guide

- [ ] Step-by-step setup instructions
- [ ] Troubleshooting guide
- [ ] Security best practices

### 3. Developer Documentation

- [ ] Plugin architecture overview
- [ ] Extension points for custom executors
- [ ] Contributing guidelines

## Migration Strategy

### Phase 1: Fix Immediate Issues (Week 1)

1. Fix configuration inconsistencies
2. Update documentation
3. Add basic unit tests
4. Improve error handling

### Phase 2: Expand Event Support (Week 2-3)

1. Add support for more Jellyfin events
2. Implement event filtering conditions
3. Create flexible script configuration system

### Phase 3: Enhanced Architecture (Week 4-6)

1. Implement multi-language script support
2. Add advanced execution features (retries, queuing)
3. Implement proper security measures
4. Add comprehensive monitoring and logging

### Phase 4: Advanced Features (Week 7-8)

1. Web UI for script management
2. Script templates and marketplace
3. Performance optimizations
4. Advanced integration capabilities
