# Script Execution Usage Guide

This guide provides comprehensive instructions on how to use the script execution feature
of the JellyPy plugin to automate tasks and integrate with external services.

## Overview

The JellyPy plugin allows you to execute custom scripts in response to Jellyfin events.
Scripts can be triggered by playback events, library changes, user actions, and server
lifecycle events. This enables advanced automation, notifications, and integration with
other services beyond the built-in Sonarr and Radarr support.

### Supported Script Types

• **Python**: Python 3 scripts (.py)
• **PowerShell**: PowerShell scripts (.ps1)
• **Bash/Shell**: Shell scripts (.sh)
• **Node.js**: JavaScript/TypeScript scripts (.js, .ts)
• **Binary**: Direct executable binaries

## Getting Started

### Accessing Script Settings

1. Navigate to **Jellyfin Dashboard**
2. Go to **Plugins** and select **JellyPy**
3. Click the **Script Settings** tab

### Creating Your First Script Setting

1. Click **Add Script Setting** button
2. Configure the basic information:
   • **Name**: Descriptive name for this script configuration
   • **Description**: Optional notes about what the script does
   • **Enabled**: Toggle to enable/disable this script setting
3. Save your configuration

## Event Triggers

Scripts can be triggered by the following Jellyfin events:

### Playback Events

• **PlaybackStart**: When media playback begins
• **PlaybackStop**: When media playback ends
• **PlaybackPause**: When playback is paused
• **PlaybackResume**: When playback resumes from pause

### Library Events

• **ItemAdded**: When new media is added to the library
• **ItemUpdated**: When library item metadata is updated
• **ItemRemoved**: When media is removed from the library

### User Events

• **UserCreated**: When a new user account is created
• **UserUpdated**: When user settings are modified
• **UserDeleted**: When a user account is deleted

### Session Events

• **SessionStart**: When a user starts a new session
• **SessionEnd**: When a user session ends

### Server Events

• **ServerStartup**: When Jellyfin server starts
• **ServerShutdown**: When Jellyfin server shuts down

## Script Execution Configuration

### Executor Settings

Configure how your script is executed:

**Executor Type**: Select the script type (Python, PowerShell, Bash, NodeJs, Binary)

**Executable Path**: Path to the interpreter or executable
• Python: `/usr/bin/python3` or `C:\Python39\python.exe`
• PowerShell: `/usr/bin/pwsh` or `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
• Bash: `/bin/bash`
• Node.js: `/usr/bin/node` or `C:\Program Files\nodejs\node.exe`
• Binary: Leave empty or specify full path to executable

**Script Path**: Absolute path to your script file
• Linux: `/opt/scripts/jellypy_notification.py`
• Windows: `C:\Scripts\jellypy_notification.ps1`

**Working Directory**: Optional directory where script runs (defaults to script location)

**Additional Arguments**: Extra command line arguments to pass to your script

**Timeout (seconds)**: Maximum execution time (default: 300 seconds)

### Execution Conditions

Add conditions to control when scripts execute. Conditions are evaluated against event data
and all conditions must be met for the script to run.

**Available Operators**:

• **Equals**: Exact match (field = value)
• **NotEquals**: Not equal (field != value)
• **Contains**: Field contains substring
• **NotContains**: Field does not contain substring
• **StartsWith**: Field starts with value
• **EndsWith**: Field ends with value
• **Regex**: Regular expression match
• **GreaterThan**: Numeric comparison (field > value)
• **LessThan**: Numeric comparison (field < value)
• **In**: Value exists in comma-separated list
• **NotIn**: Value does not exist in list

**Common Condition Examples**:

```text
Run only for movies:
  Field: ItemType
  Operator: Equals
  Value: Movie

Run for specific genres:
  Field: Genres
  Operator: Contains
  Value: Action

Exclude specific users:
  Field: UserName
  Operator: NotEquals
  Value: TestUser

Run for long videos only:
  Field: RuntimeTicks
  Operator: GreaterThan
  Value: 36000000000
```

### Data Attributes

Configure how event data is passed to your script. Data can be passed in multiple formats
to suit different scripting needs.

**Available Formats**:

• **Environment**: Pass as environment variable (e.g., `EVENT_TYPE=PlaybackStart`)
• **Argument**: Pass as command line argument (e.g., `--event-type PlaybackStart`)
• **Json**: Pass as JSON object
• **String**: Pass as plain string value

**Common Data Attributes**:

```text
Event Information:
  Name: EVENT_TYPE
  Source Field: EventType
  Format: Environment

User Information:
  Name: USER_NAME
  Source Field: UserName
  Format: Environment

Media Item Name:
  Name: ITEM_NAME
  Source Field: ItemName
  Format: Environment

Media Type:
  Name: ITEM_TYPE
  Source Field: ItemType
  Format: Environment

Item ID:
  Name: ITEM_ID
  Source Field: ItemId
  Format: Argument

Playback Position:
  Name: POSITION_TICKS
  Source Field: PositionTicks
  Format: Environment

Runtime:
  Name: RUNTIME_TICKS
  Source Field: RuntimeTicks
  Format: Environment

Client/Device:
  Name: CLIENT_NAME
  Source Field: ClientName
  Format: Environment
```

**Required vs Optional Attributes**:

• **Required**: Script execution fails if the field is missing
• **Optional**: Uses default value if field is missing

## Global Settings

Configure global script execution behavior:

**Max Concurrent Executions**: Number of scripts that can run simultaneously (default: 5)

**Default Timeout**: Default timeout for all scripts unless overridden (default: 300 seconds)

**Verbose Logging**: Enable detailed logging for troubleshooting

## Script Development

### Receiving Event Data

Your scripts receive event data based on the configured data attributes:

**Environment Variables** (Python example):

```python
import os

event_type = os.environ.get('EVENT_TYPE')
user_name = os.environ.get('USER_NAME')
item_name = os.environ.get('ITEM_NAME')
item_type = os.environ.get('ITEM_TYPE')
```

**Command Line Arguments** (Python example):

```python
import sys
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--item-id', required=True)
parser.add_argument('--event-type', required=True)
args = parser.parse_args()

print(f"Processing {args.event_type} for item {args.item_id}")
```

**PowerShell Example**:

```powershell
$EventType = $env:EVENT_TYPE
$UserName = $env:USER_NAME
$ItemName = $env:ITEM_NAME

Write-Host "Event: $EventType by $UserName for $ItemName"
```

### Best Practices

**Error Handling**: Implement robust error handling to prevent script failures from
affecting Jellyfin.

```python
try:
    # Your script logic
    pass
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
    sys.exit(1)
```

**Logging**: Use logging to debug and monitor script execution. Write to a file rather
than relying on console output.

```python
import logging

logging.basicConfig(
    filename='/var/log/jellypy/script.log',
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

logging.info(f"Processing event: {event_type}")
```

**Timeouts**: Keep scripts fast and efficient. Long-running operations should be
asynchronous or use background tasks.

**Security**: Validate all input data. Never execute arbitrary code or commands based on
event data without validation.

**Testing**: Test scripts manually before enabling them in production:

```bash
# Test with environment variables
EVENT_TYPE=PlaybackStart USER_NAME=TestUser ITEM_NAME="Test Movie" python3 script.py

# Test with arguments
python3 script.py --event-type PlaybackStart --item-id 12345
```

## Example Configurations

### Send Discord Notification on Playback

**Triggers**: PlaybackStart

**Conditions**:
• Field: `ItemType`, Operator: `In`, Value: `Movie,Episode`

**Data Attributes**:
• `EVENT_TYPE` from `EventType` as Environment
• `USER_NAME` from `UserName` as Environment
• `ITEM_NAME` from `ItemName` as Environment
• `ITEM_TYPE` from `ItemType` as Environment

**Script**: See `examples/basic_notification.py` for implementation

### Log Library Changes

**Triggers**: ItemAdded, ItemUpdated, ItemRemoved

**Conditions**: None (run for all library events)

**Data Attributes**:
• `EVENT_TYPE` from `EventType` as Environment
• `ITEM_NAME` from `ItemName` as Environment
• `ITEM_ID` from `ItemId` as Argument
• `TIMESTAMP` from `Timestamp` as Environment

### Monitor Specific User Activity

**Triggers**: PlaybackStart, PlaybackStop

**Conditions**:
• Field: `UserName`, Operator: `Equals`, Value: `AdminUser`

**Data Attributes**:
• `EVENT_TYPE` from `EventType` as Environment
• `ITEM_NAME` from `ItemName` as Environment
• `POSITION_TICKS` from `PositionTicks` as Environment

## Troubleshooting

### Scripts Not Executing

1. **Check script setting is enabled**: Verify the Enabled checkbox is checked
2. **Verify triggers are configured**: At least one event trigger must be selected
3. **Review conditions**: Ensure conditions match your event data
4. **Check executable path**: Verify Python/PowerShell/etc. path is correct
5. **Confirm script path**: Ensure script file exists and has execute permissions

### Permission Issues

Scripts must have appropriate permissions:

```bash
# Linux/macOS
chmod +x /path/to/script.py
chown jellyfin:jellyfin /path/to/script.py

# Verify Jellyfin user can execute
sudo -u jellyfin python3 /path/to/script.py
```

### Viewing Logs

Enable verbose logging in Global Settings and check Jellyfin logs:

```text
Dashboard → Logs → Filter: "JellyPy"
```

Look for messages about script execution, conditions evaluation, and any errors.

### Script Errors

If your script fails:

1. Test the script manually with sample data
2. Check script syntax and dependencies
3. Review error output in Jellyfin logs
4. Verify all required data attributes are configured
5. Ensure timeout is sufficient for script execution

### Common Issues

**Environment variables not available**: Ensure data attributes are set to Environment format

**Script timeout**: Increase timeout value or optimize script performance

**Path issues on Windows**: Use double backslashes in paths: `C:\\Scripts\\script.ps1`

**Permission denied**: Grant execute permissions and verify Jellyfin user can access script

## Example Scripts

Complete example scripts are available in the `examples/` directory:

• `basic_notification.py`: Demonstrates event handling and notifications
• `legacy_download.py`: Shows integration with download managers

These examples demonstrate best practices for receiving event data, error handling,
logging, and integration patterns.
