# JellyPy Script Configuration Data Flow

## Complete Lifecycle: From UI to XML and Back

### 1. Initial Page Load (Reading from XML)

#### Step 1.1: Jellyfin Loads Plugin Configuration
```
Jellyfin Server Startup
  → Reads: /config/plugins/configurations/Jellyfin.Plugin.JellyPy.xml
  → Deserializes XML → PluginConfiguration object
  → Available via: Plugin.Instance.Configuration
```

#### Step 1.2: UI Requests Configuration
```javascript
// configPage.html line ~1318
ApiClient.getPluginConfiguration(JellyPyConfig.pluginUniqueId)
  .then(function (config) {
    JellyPyConfig.currentConfig = config;
    JellyPyConfig.scriptSettings = config.ScriptSettings || [];
    renderScriptList();
  });
```

**Data Structure Loaded:**
```csharp
// PluginConfiguration.cs
public class PluginConfiguration : BasePluginConfiguration
{
    public Collection<ScriptSetting> ScriptSettings { get; }  // Line ~330
    // ScriptSettings is a Collection initialized in constructor
}
```

**XML Structure:**
```xml
<PluginConfiguration>
  <ScriptSettings>
    <ScriptSetting>
      <Id>guid-here</Id>
      <Name>New Script</Name>
      <Description></Description>
      <Enabled>true</Enabled>
      <Triggers>
        <EventType>PlaybackStart</EventType>
      </Triggers>
      <Conditions>
        <ExecutionCondition>
          <Field>User.Name</Field>
          <Operator>Equals</Operator>
          <Value>admin</Value>
          <CaseSensitive>false</CaseSensitive>
        </ExecutionCondition>
      </Conditions>
      <Execution>
        <ExecutorType>Python</ExecutorType>
        <ExecutablePath>/usr/bin/python3</ExecutablePath>
        <ScriptPath>/path/to/script.py</ScriptPath>
        <WorkingDirectory></WorkingDirectory>
        <AdditionalArguments></AdditionalArguments>
        <TimeoutSeconds>300</TimeoutSeconds>
      </Execution>
      <DataAttributes>
        <ScriptDataElement>
          <Name>user_name</Name>
          <SourceField>User.Name</SourceField>
          <Format>Environment</Format>
          <Required>false</Required>
          <DefaultValue></DefaultValue>
        </ScriptDataElement>
      </DataAttributes>
      <Priority>100</Priority>
    </ScriptSetting>
  </ScriptSettings>
</PluginConfiguration>
```

---

### 2. User Interaction (Modifying in Memory)

#### Step 2.1: User Clicks "Add" Button
```javascript
// configPage.html line ~653
function addNewScript() {
  var newScript = {
    Id: generateGuid(),          // New GUID
    Name: 'New Script',
    Description: '',
    Enabled: true,
    Triggers: [],                 // Empty array
    Conditions: [],               // Empty array
    Execution: {
      ExecutorType: 0,            // Python = 0
      ExecutablePath: '/usr/bin/python3',
      ScriptPath: '',
      WorkingDirectory: '',
      AdditionalArguments: '',
      TimeoutSeconds: 300
    },
    DataAttributes: [],           // Empty array
    Priority: 100
  };
  
  // Add to IN-MEMORY array (not saved yet!)
  JellyPyConfig.scriptSettings.push(newScript);
  renderScriptList();
}
```

**Key Point:** At this stage, the script exists ONLY in the browser's memory in the `JellyPyConfig.scriptSettings` array. Nothing is saved to disk yet.

#### Step 2.2: User Selects Script
```javascript
// configPage.html line ~693
function selectScript(scriptId) {
  JellyPyConfig.selectedScriptId = scriptId;
  renderScriptDetails();
}
```

#### Step 2.3: User Modifies Script Properties
```javascript
// Example: User changes script name
// configPage.html line ~801
document.getElementById('script-name').onchange = function() { 
  updateScriptProperty('Name', this.value); 
};

// configPage.html line ~824
function updateScriptProperty(property, value) {
  var script = JellyPyConfig.scriptSettings.find(
    s => s.Id === JellyPyConfig.selectedScriptId
  );
  if (script) {
    script[property] = value;  // Updates IN-MEMORY only!
    if (property === 'Name' || property === 'Enabled') {
      renderScriptList();  // Refresh UI
    }
  }
}
```

**Key Point:** All changes update the `JellyPyConfig.scriptSettings` array in memory. The XML file is still untouched.

---

### 3. Saving Configuration (Memory → XML)

#### Step 3.1: User Clicks "Save" Button
```javascript
// configPage.html line ~1358
document.querySelector('#JellyPyConfigForm')
  .addEventListener('submit', function(e) {
    var config = JellyPyConfig.currentConfig;
    
    // Assign the IN-MEMORY script settings to config
    config.ScriptSettings = JellyPyConfig.scriptSettings;
    
    // Send to Jellyfin API
    ApiClient.updatePluginConfiguration(JellyPyConfig.pluginUniqueId, config)
      .then(function (result) {
        Dashboard.processPluginConfigurationUpdateResult(result);
      });
    
    e.preventDefault();
    return false;
  });
```

#### Step 3.2: Jellyfin API Processes Save Request
```
Browser → HTTP POST → Jellyfin API
  → BasePluginConfiguration.SaveConfiguration() called
  → Serializes PluginConfiguration object to XML
  → Writes to: /config/plugins/configurations/Jellyfin.Plugin.JellyPy.xml
```

**Jellyfin's Save Process:**
```csharp
// Jellyfin's BasePluginConfiguration (framework code)
public void SaveConfiguration()
{
    // Serialize this object to XML
    var xml = XmlSerializer.Serialize(this);
    
    // Write to plugin config directory
    File.WriteAllText(
        Path.Combine(PluginDirectory, "Jellyfin.Plugin.JellyPy.xml"),
        xml
    );
    
    // Trigger ConfigurationUpdated event
    OnConfigurationChanged();
}
```

---

### 4. Configuration Updated Event (XML → Runtime)

#### Step 4.1: Plugin Receives Configuration Change Notification
```csharp
// Plugin.cs line ~70
public Plugin(/* ... */)
{
    Instance = this;
    ConfigurationChanged += OnConfigurationChanged;
}

private void OnConfigurationChanged(object? sender, BasePluginConfiguration e)
{
    // New configuration loaded from XML
    var config = (PluginConfiguration)e;
    
    // Script settings are now available via:
    // config.ScriptSettings (Collection<ScriptSetting>)
    
    _logger.LogInformation(
        "Configuration reloaded: {Count} script settings",
        config.ScriptSettings.Count
    );
}
```

#### Step 4.2: Services Access Updated Configuration
```csharp
// ScriptExecutionService.cs accesses scripts via:
var scriptSettings = Plugin.Instance.Configuration.ScriptSettings;

foreach (var setting in scriptSettings)
{
    // Check if this script should run for this event
    if (setting.Enabled && setting.Triggers.Contains(eventType))
    {
        await ExecuteScriptSettingAsync(setting, eventData);
    }
}
```

---

### 5. Data Persistence Layer

#### XML File Location
```
/config/plugins/configurations/Jellyfin.Plugin.JellyPy.xml
```

#### Serialization Format
- **Serializer:** .NET XmlSerializer
- **Collections:** XML elements with repeated child elements
- **Enums:** Serialized as integer values
- **Nested Objects:** Nested XML elements

#### Example Full XML Structure
```xml
<?xml version="1.0" encoding="utf-8"?>
<PluginConfiguration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ScriptSettings>
    <ScriptSetting>
      <Id>a1b2c3d4-e5f6-7890-abcd-ef1234567890</Id>
      <Name>Playback Notification</Name>
      <Description>Send notification when user starts watching</Description>
      <Enabled>true</Enabled>
      <Triggers>
        <EventType>0</EventType>  <!-- PlaybackStart -->
        <EventType>1</EventType>  <!-- PlaybackStop -->
      </Triggers>
      <Conditions>
        <ExecutionCondition>
          <Field>User.Name</Field>
          <Operator>0</Operator>  <!-- Equals -->
          <Value>admin</Value>
          <CaseSensitive>false</CaseSensitive>
        </ExecutionCondition>
      </Conditions>
      <Execution>
        <ExecutorType>0</ExecutorType>  <!-- Python -->
        <ExecutablePath>/usr/bin/python3</ExecutablePath>
        <ScriptPath>/scripts/notify.py</ScriptPath>
        <WorkingDirectory>/scripts</WorkingDirectory>
        <AdditionalArguments>--verbose</AdditionalArguments>
        <TimeoutSeconds>300</TimeoutSeconds>
      </Execution>
      <DataAttributes>
        <ScriptDataElement>
          <Name>USER_NAME</Name>
          <SourceField>User.Name</SourceField>
          <Format>2</Format>  <!-- Environment -->
          <Required>true</Required>
          <DefaultValue>unknown</DefaultValue>
        </ScriptDataElement>
        <ScriptDataElement>
          <Name>item_id</Name>
          <SourceField>Item.Id</SourceField>
          <Format>3</Format>  <!-- Argument -->
          <Required>true</Required>
          <DefaultValue></DefaultValue>
        </ScriptDataElement>
      </DataAttributes>
      <Priority>100</Priority>
    </ScriptSetting>
  </ScriptSettings>
  
  <GlobalSettings>
    <MaxConcurrentExecutions>5</MaxConcurrentExecutions>
    <DefaultTimeoutSeconds>300</DefaultTimeoutSeconds>
    <QueueSize>100</QueueSize>
    <EnableVerboseLogging>false</EnableVerboseLogging>
  </GlobalSettings>
  
  <EnableNativeSonarrIntegration>true</EnableNativeSonarrIntegration>
  <SonarrUrl>http://localhost:8989</SonarrUrl>
  <SonarrApiKeyEncrypted>encrypted-base64-string-here</SonarrApiKeyEncrypted>
  <!-- ... other settings ... -->
</PluginConfiguration>
```

---

### 6. Critical Data Flow Points

#### 6.1: Collection Initialization
```csharp
// PluginConfiguration.cs constructor
public PluginConfiguration()
{
    // IMPORTANT: Initialize Collection in constructor
    ScriptSettings = new Collection<ScriptSetting>();
    GlobalSettings = new GlobalScriptSettings();
}
```

**Why This Matters:**
- Collections MUST be initialized in constructor
- XML deserializer adds items to existing collection
- If not initialized, deserialization fails with NullReferenceException

#### 6.2: Enum Serialization
```csharp
// ScriptSetting.cs
public enum ScriptExecutorType
{
    Python,      // 0
    PowerShell,  // 1
    Bash,        // 2
    NodeJs,      // 3
    Binary       // 4
}

public enum DataAttributeFormat
{
    String,      // 0
    Json,        // 1
    Environment, // 2
    Argument     // 3
}
```

**XML Representation:**
```xml
<ExecutorType>0</ExecutorType>      <!-- Python -->
<Format>2</Format>                  <!-- Environment -->
```

#### 6.3: Empty Collections
```javascript
// JavaScript empty array
Triggers: []

// Serializes to XML as empty element
<Triggers />

// Deserializes back to empty Collection<T>
```

---

### 7. State Management

#### Browser State
```javascript
var JellyPyConfig = {
  pluginUniqueId: 'a5bd541f-38dc-467e-9a9a-15fe3f3bcf5c',
  currentConfig: null,      // Full config from API
  selectedScriptId: null,   // Currently selected script ID
  scriptSettings: []        // IN-MEMORY array of scripts
};
```

#### Server State
```csharp
// Plugin.cs - Singleton instance
public static Plugin? Instance { get; private set; }

// Access configuration anywhere:
Plugin.Instance.Configuration.ScriptSettings
```

---

### 8. Common Issues & Solutions

#### Issue 1: Scripts Not Persisting
**Problem:** User adds scripts, they appear in UI, but disappear on refresh

**Cause:** User didn't click "Save" button

**Solution:** Ensure form submit calls `ApiClient.updatePluginConfiguration()`

#### Issue 2: Scripts Get Jumbled
**Problem:** Adding new script causes existing scripts to merge/duplicate

**Cause:** Auto-selecting new script while previous script form is still rendered

**Solution:** Don't auto-select new scripts (line ~668 fix)

#### Issue 3: Empty Collections Become Null
**Problem:** `Collection<T>` properties become null after save/load

**Cause:** Not initializing collections in constructor

**Solution:** Initialize all collections in `PluginConfiguration()` constructor

#### Issue 4: Enum Values Wrong
**Problem:** Enums show as numbers in XML, load incorrectly

**Cause:** XML serializes enums as integers by default

**Solution:** This is correct behavior - ensure enum order matches expectations

---

### 9. Data Validation

#### Frontend Validation
```javascript
// Happens on form input changes
function updateScriptProperty(property, value) {
  var script = JellyPyConfig.scriptSettings.find(/*...*/);
  if (script) {
    script[property] = value;  // Direct assignment, no validation
  }
}
```

**Current State:** Minimal frontend validation

#### Backend Validation
```csharp
// ScriptExecutionService.cs validates before execution
if (string.IsNullOrEmpty(setting.Execution.ScriptPath))
{
    _logger.LogError("Script path not configured");
    return;
}

if (!File.Exists(setting.Execution.ScriptPath))
{
    _logger.LogError("Script file not found: {Path}", scriptPath);
    return;
}
```

**Validation Points:**
- Script path exists
- Executable path is valid
- Required data attributes have values
- Timeout is within bounds (30-3600 seconds)

---

### 10. Flow Summary Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    USER INTERACTION                         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  1. Page Load: ApiClient.getPluginConfiguration()          │
│     → Reads from Plugin.Instance.Configuration              │
│     → Loads into JellyPyConfig.scriptSettings array         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  2. User Edits: updateScriptProperty()                     │
│     → Updates JellyPyConfig.scriptSettings IN MEMORY        │
│     → renderScriptList() refreshes UI                       │
│     → NO SAVE YET                                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  3. User Saves: ApiClient.updatePluginConfiguration()      │
│     → Sends JellyPyConfig.scriptSettings to API            │
│     → Jellyfin serializes to XML                           │
│     → Writes: /config/plugins/.../JellyPy.xml              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  4. Config Change Event: OnConfigurationChanged()          │
│     → Plugin receives notification                          │
│     → Services reload from Plugin.Instance.Configuration    │
│     → Scripts available for execution                       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  5. Script Execution: ScriptExecutionService               │
│     → Reads Plugin.Instance.Configuration.ScriptSettings    │
│     → Matches events to enabled scripts                     │
│     → Executes matching scripts                            │
└─────────────────────────────────────────────────────────────┘
```

---

### 11. Key Takeaways

1. **Three States:**
   - Browser Memory (`JellyPyConfig.scriptSettings`)
   - Server Memory (`Plugin.Instance.Configuration.ScriptSettings`)
   - Disk Storage (`/config/plugins/configurations/Jellyfin.Plugin.JellyPy.xml`)

2. **Save Triggers State Sync:**
   - Browser → Server: HTTP POST via `ApiClient.updatePluginConfiguration()`
   - Server → Disk: Automatic XML serialization
   - Disk → Server: On plugin load or config change event

3. **Collections Must Be Initialized:**
   - In C# constructor: `ScriptSettings = new Collection<ScriptSetting>()`
   - Empty in JavaScript: `[]`
   - Empty in XML: `<ScriptSettings />`

4. **No Auto-Save:**
   - All changes are in-memory until "Save" is clicked
   - Navigating away without saving = changes lost
   - Browser refresh = reloads from disk, losing unsaved changes

5. **Enum Serialization:**
   - JavaScript uses integers: `ExecutorType: 0`
   - XML stores integers: `<ExecutorType>0</ExecutorType>`
   - C# deserializes to enum: `ScriptExecutorType.Python`
