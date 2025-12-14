#!/usr/bin/env python3
"""
Test script for JellyPy ExecutionMode functionality

This script exercises both plugin execution modes:
1. JsonPayload (default) - full EventData provided as first argument
2. Compatibility        - data attributes mapped to env/args per setting

The script logs execution details to /tmp/jellypy_test.log and will try to
parse the JSON payload when present so it works out of the box with the
default JsonPayload mode.
"""

import sys
import os
import json
from datetime import datetime


def log_execution():
    """Log execution details to file"""
    log_file = '/tmp/jellypy_test.log'
    
    with open(log_file, 'a') as f:
        f.write('\n' + '='*70 + '\n')
        f.write(f'Execution at: {datetime.now().isoformat()}\n')
        f.write('='*70 + '\n\n')
        
        # Log script info
        f.write(f'Script: {sys.argv[0]}\n')
        f.write(f'Python: {sys.version}\n')
        f.write(f'Working Directory: {os.getcwd()}\n\n')
        
        # Detect execution mode and log accordingly
        execution_mode = detect_execution_mode()
        f.write(f'Detected ExecutionMode: {execution_mode}\n\n')
        
        if execution_mode == 'JSON Payload':
            log_json_mode(f)
        elif execution_mode == 'Environment Variables':
            log_env_mode(f)
        elif execution_mode == 'Command Arguments':
            log_args_mode(f)
        
        f.write('\n' + '='*70 + '\n')
        f.write('End of execution\n')
        f.write('='*70 + '\n\n')
    
    print(f"Execution logged to: {log_file}")


def detect_execution_mode():
    """Detect which execution mode is being used"""
    # Check for JSON payload (first argument after script path)
    if len(sys.argv) > 1:
        try:
            json.loads(sys.argv[1])
            return 'JsonPayload'
        except (json.JSONDecodeError, ValueError):
            pass
    
    # Check for environment variables
    jellypy_env_vars = [k for k in os.environ.keys() 
                        if k in ['EVENT_TYPE', 'USER_NAME', 'ITEM_NAME', 'TIMESTAMP']]
    if jellypy_env_vars:
        return 'Compatibility'
    
    # Otherwise assume arguments mode
    if len(sys.argv) > 1:
        return 'Command Arguments'
    
    return 'Unknown'


def log_json_mode(f):
    """Log JSON payload execution mode"""
    f.write('MODE: JsonPayload (default)\n')
    f.write('-' * 70 + '\n\n')
    
    try:
        json_payload = json.loads(sys.argv[1])
        f.write('JSON payload received:\n')
        f.write(json.dumps(json_payload, indent=2))
        f.write('\n\n')
        
        # Extract and display key fields
        f.write('Key Event Data:\n')
        f.write(f"  Event Type: {json_payload.get('EventType', 'N/A')}\n")
        f.write(f"  User: {json_payload.get('UserName', 'N/A')}\n")
        f.write(f"  Item: {json_payload.get('ItemName', 'N/A')}\n")
        f.write(f"  Item Type: {json_payload.get('ItemType', 'N/A')}\n")
        f.write(f"  Timestamp: {json_payload.get('Timestamp', 'N/A')}\n")
        
        # Show all available fields
        f.write(f"\n  Total fields received: {len(json_payload)}\n")
        f.write(f"  Available fields: {', '.join(json_payload.keys())}\n")
        
    except (json.JSONDecodeError, IndexError) as e:
        f.write(f'Error parsing JSON payload: {e}\n')


def log_env_mode(f):
    """Log environment variable execution mode"""
    f.write('MODE: Compatibility (env/args)\n')
    f.write('-' * 70 + '\n\n')
    
    # List of expected JellyPy environment variables
    jellypy_vars = [
        'EVENT_TYPE', 'TIMESTAMP', 'USER_NAME', 'USER_ID',
        'ITEM_NAME', 'ITEM_ID', 'ITEM_TYPE', 'ITEM_PATH',
        'CLIENT_NAME', 'DEVICE_NAME', 'SERIES_NAME',
        'SEASON_NUMBER', 'EPISODE_NUMBER', 'YEAR',
        'GENRES', 'RUNTIME_TICKS', 'POSITION_TICKS',
        'RATING', 'PLOT'
    ]
    
    f.write('Environment Variables:\n')
    found = 0
    for var in jellypy_vars:
        value = os.getenv(var)
        if value:
            f.write(f'  {var}: {value}\n')
            found += 1
    
    f.write(f'\nFound {found}/{len(jellypy_vars)} expected variables\n')
    
    # Log command line arguments if any
    if len(sys.argv) > 1:
        f.write('\nAdditional Arguments:\n')
        for i, arg in enumerate(sys.argv[1:], 1):
            f.write(f'  arg[{i}]: {arg}\n')


def log_args_mode(f):
    """Log command arguments execution mode"""
    f.write('MODE: Command Arguments\n')
    f.write('-' * 70 + '\n\n')
    
    f.write(f'Total arguments: {len(sys.argv) - 1}\n\n')
    
    if len(sys.argv) > 1:
        f.write('Arguments received:\n')
        for i, arg in enumerate(sys.argv[1:], 1):
            # Truncate long arguments
            display_arg = arg if len(arg) < 200 else arg[:200] + '...'
            f.write(f'  arg[{i}]: {display_arg}\n')
    else:
        f.write('No arguments provided\n')


def main():
    """Main entry point"""
    try:
        log_execution()
        print("✓ Script executed successfully")
        return 0
    except Exception as e:
        print(f"✗ Error: {e}", file=sys.stderr)
        with open('/tmp/jellypy_test.log', 'a') as f:
            f.write(f'\nERROR: {e}\n')
        return 1


if __name__ == '__main__':
    sys.exit(main())
