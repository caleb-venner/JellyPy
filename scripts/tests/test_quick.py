#!/usr/bin/env python3
"""
JellyPy Script Execution Quick Test

Minimal test script to quickly verify basic script execution is working.
Useful for rapid validation during deployment.

This script:
- Writes timestamp to log
- Logs event type and user
- Verifies basic functionality
- Creates a marker file to prove execution

Usage:
1. Configure minimal settings in JellyPy
2. Trigger any event in Jellyfin
3. Check for marker file in /tmp/jellypy_quick_test/
"""

import os
import sys
from pathlib import Path
from datetime import datetime

# Create test directory and marker file
test_dir = Path('/tmp/jellypy_quick_test')
test_dir.mkdir(exist_ok=True)

# Get event data
event_type = os.getenv('EVENT_TYPE', 'UNKNOWN')
user_name = os.getenv('USER_NAME', 'SYSTEM')
item_name = os.getenv('ITEM_NAME', 'N/A')
timestamp = datetime.now().isoformat()

# Write marker file with event data
marker_file = test_dir / f'event_{timestamp.replace(":", "-").split(".")[0]}.txt'
marker_file.write_text(
    f"Event Type: {event_type}\n"
    f"User: {user_name}\n"
    f"Item: {item_name}\n"
    f"Timestamp: {timestamp}\n"
    f"Script Path: {sys.argv[0]}\n"
)

# Write to stderr so it appears in Jellyfin logs
print(f"[JellyPy Quick Test] {event_type} by {user_name} - {item_name}", file=sys.stderr)

sys.exit(0)
