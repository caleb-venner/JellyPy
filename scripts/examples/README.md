# JellyPy Script Testing Examples

This directory contains test scripts and utilities for developing and testing JellyPy scripts without deploying to Jellyfin.

## Testing Script Execution Modes

### Quick Start

Test your script with all three execution modes:

```bash
cd examples
python3 standalone_test.py --script test_execution_mode.py
```

This will simulate how JellyPy executes your script using:
• Environment Variables (traditional mode)
• Command Arguments
• JSON Payload (new ExecutionMode feature)

### Test Scripts

#### `standalone_test.py`

Standalone test harness that simulates JellyPy script execution without requiring a Jellyfin instance.

**Usage:**

```bash
# Test all modes
python3 standalone_test.py --mode all --script your_script.py

# Test specific mode
python3 standalone_test.py --mode json --script your_script.py
python3 standalone_test.py --mode env --script your_script.py
python3 standalone_test.py --mode args --script your_script.py
```

#### `test_execution_mode.py`

Example script that demonstrates how to handle all three execution modes. It automatically detects which mode is being used and logs the received data.

**View the logs:**

```bash
cat /tmp/jellypy_test.log
tail -f /tmp/jellypy_test.log  # watch in real-time
```

#### `test_script_execution.py`

Comprehensive test script with detailed logging and validation.

#### `test_script_execution.sh`

Bash version of the test script for shell script testing.

## Execution Modes Explained

### 1. Environment Variables (Traditional)

Data is passed via environment variables:

```python
import os
event_type = os.getenv('EVENT_TYPE')
user_name = os.getenv('USER_NAME')
item_name = os.getenv('ITEM_NAME')
```

**Available variables:**
• `EVENT_TYPE` - Type of event (PlaybackStart, ItemAdded, etc.)
• `TIMESTAMP` - ISO 8601 timestamp
• `USER_NAME` - Jellyfin username
• `USER_ID` - User GUID
• `ITEM_NAME` - Media item name
• `ITEM_ID` - Media item GUID
• `ITEM_TYPE` - Type (Movie, Episode, etc.)
• `ITEM_PATH` - File path
• `CLIENT_NAME` - Client application
• `DEVICE_NAME` - Device name
• `SERIES_NAME` - TV series name (for episodes)
• `SEASON_NUMBER` - Season number (for episodes)
• `EPISODE_NUMBER` - Episode number (for episodes)
• `YEAR` - Release year
• `GENRES` - Comma-separated genres
• `RUNTIME_TICKS` - Duration in ticks
• `POSITION_TICKS` - Playback position in ticks
• `RATING` - Content rating
• `PLOT` - Description/plot summary

### 2. Command Arguments

Data is passed as command line arguments:

```python
import sys
event_type = sys.argv[1]
user_name = sys.argv[2]
item_name = sys.argv[3]
```

Arguments are configured in the Script Settings UI using DataAttribute format settings.

### 3. JSON Payload (New - ExecutionMode)

Complete event data is passed as a single JSON argument:

```python
import sys
import json

event_data = json.loads(sys.argv[1])
event_type = event_data['EventType']
user_name = event_data['UserName']
item_name = event_data['ItemName']
```

**Benefits:**
• Access to all event fields without configuration
• Structured data format
• Easy to parse and validate
• No need to configure DataAttributes individually

## Example Scripts

### Basic Notification Script

```python
#!/usr/bin/env python3
import sys
import json

# Parse JSON payload
event_data = json.loads(sys.argv[1])

# Extract data
event_type = event_data['EventType']
user = event_data['UserName']
item = event_data['ItemName']

# Send notification
print(f"{user} triggered {event_type} on {item}")
```

### Discord Webhook Script

See `discord_notification.py` for a complete example of sending Discord notifications.

### Notifiarr Integration

See `notifiarr_notification.py` for integration with Notifiarr notification service.

## Testing Your Own Scripts

1. **Create your script** following one of the example patterns
2. **Test standalone** using `standalone_test.py`:

   ```bash
   python3 standalone_test.py --script your_script.py
   ```

3. **Check the logs** to verify data is received correctly:

   ```bash
   cat /tmp/jellypy_test.log
   ```

4. **Deploy to Jellyfin** once testing is successful

## Troubleshooting

### Script not receiving data

• Verify script has execute permissions: `chmod +x your_script.py`
• Check Python shebang line: `#!/usr/bin/env python3`
• Test with `standalone_test.py` first before deploying

### JSON parsing errors

• Ensure you're checking `sys.argv[1]` (not `sys.argv[2]`)
• Add error handling for JSON decode exceptions
• Use `test_execution_mode.py` as a reference

### Environment variables missing

• Environment mode requires DataAttributes to be configured
• JSON Payload mode provides all fields automatically
• Check variable names match expected format (UPPERCASE with underscores)

## Additional Resources

• Script execution documentation: `../SCRIPT_EXECUTION.md`
• Plugin configuration guide: `../README.md`
• Example integration scripts in this directory
