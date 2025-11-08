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
• **Bash/Shell**: Shell scripts (.sh)
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

**Executor Type**: Select the script type (Python, Bash, Binary)

**Executable Path**: Path to the interpreter or executable
• Python: `/usr/bin/python3` or `C:\Python39\python.exe`
• Bash: `/bin/bash`
• Binary: Leave empty or specify full path to executable

**Script Path**: Absolute path to your script file
• Linux: `/opt/scripts/jellypy_notification.py`
• Windows: `C:\Scripts\jellypy_notification.py`

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

Configure global script execution behavior in the **Global Settings** tab.

### Item Grouping

These settings control how the plugin handles items that are added to the library in quick succession, such as when adding a full season of a TV show.

**Enable Item Grouping**:
- **Enabled** (default): The plugin will wait for a short period to collect all related items (like episodes from the same season) and fire a single `SeriesEpisodesAdded` event. This is highly recommended to avoid spamming your scripts with dozens of individual `ItemAdded` events.
- **Disabled**: The plugin will fire an `ItemAdded` event immediately for every single item added to the library.

**Grouping Delay (seconds)**:
- The number of seconds to wait for more items before processing a group. If you have a slow system or are adding a very large number of items at once, you may want to increase this value.
- Default: `2` seconds.

### Execution Limits

**Max Concurrent Executions**: Number of scripts that can run simultaneously (default: 5)

**Default Timeout**: Default timeout for all scripts unless overridden (default: 300 seconds)

### Logging

**Verbose Logging**: Enable detailed logging for troubleshooting

## Script Development

### Receiving Event Data

Your scripts receive event data in two ways simultaneously:

1.  **As a complete JSON object**: The entire event data is automatically passed as a JSON string as the **first argument** to your script. This is the recommended way to access all event information.
2.  **As individual data attributes**: The data fields you configure in the "Data Attributes" section are passed as environment variables or subsequent command-line arguments. This is useful for backward compatibility or for scripts that only need a few specific pieces of data.

**Accessing the Full JSON Payload (Recommended)**

This method gives you access to all event details, including grouped episodes.

```python
import sys
import json

# The first argument is the script name, the second is the JSON data.
if len(sys.argv) < 2:
    print("No JSON data provided.", file=sys.stderr)
    sys.exit(1)

try:
    event_data = json.loads(sys.argv[1])
    event_type = event_data.get('EventType')
    item_name = event_data.get('ItemName')
    print(f"Processing {event_type} for item {item_name}")
except json.JSONDecodeError:
    print("Failed to decode JSON data.", file=sys.stderr)
    sys.exit(1)
```

**Accessing Individual Data Attributes (Legacy)**

This method is simpler if you only need a few fields and don't need grouped data.

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

### Example: Grouped Discord Notifications for New Media

This example uses the `examples/discord_notification.py` script to send rich, grouped notifications to Discord when new movies or TV show episodes are added. It avoids notification spam by grouping episodes added in the same batch.

**Prerequisites**:
1.  Install the `requests` Python library: `pip install requests`
2.  Have your Discord Webhook URL ready.

**Configuration**:

1.  **Triggers**:
    *   `ItemAdded`
    *   `SeriesEpisodesAdded` (This is crucial for grouping)

2.  **Execution Settings**:
    *   **Executor Type**: `Python`
    *   **Script Path**: `/path/to/your/scripts/discord_notification.py`

3.  **Data Attributes**:
    *   Create one **Environment** variable to hold your secret webhook URL:
        *   **Name**: `DISCORD_WEBHOOK_URL`
        *   **Source Field**: `Custom`
        *   **Custom Value**: `https://discord.com/api/webhooks/your/webhook_url`
        *   **Format**: `Environment`

    No other data attributes are needed, as the script uses the full JSON payload that is passed automatically.

**How It Works**:

*   The `ItemAddedManager` in the plugin automatically buffers new items for a few seconds.
*   If multiple episodes from the same series are detected, it fires a single `SeriesEpisodesAdded` event.
*   If a single movie or episode is added, it fires an `ItemAdded` event.
*   The `discord_notification.py` script receives the event data as a JSON object, checks if it's a grouped event or a single item, and formats the Discord message accordingly.

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
4. **Check executable path**: Verify Python/etc. path is correct
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

**Path issues on Windows**: Use double backslashes in paths: `C:\\Scripts\\script.py`

**Permission denied**: Grant execute permissions and verify Jellyfin user can access script

## Example Scripts

Complete example scripts are available in the `examples/` directory:

• `basic_notification.py`: Demonstrates event handling and notifications
• `legacy_download.py`: Shows integration with download managers

These examples demonstrate best practices for receiving event data, error handling,
logging, and integration patterns.
