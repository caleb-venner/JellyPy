#!/usr/bin/env python3
"""
Standalone Test for JellyPy Script Execution

This script simulates how JellyPy executes scripts, allowing you to test
your scripts without deploying to Jellyfin.

Usage:
    python3 standalone_test.py [--mode env|args|json] [--script path/to/script.py]

Examples:
    # Test with environment variables
    python3 standalone_test.py --mode env --script test_execution_mode.py

    # Test with command arguments
    python3 standalone_test.py --mode args --script test_execution_mode.py

    # Test with JSON payload (new ExecutionMode feature)
    python3 standalone_test.py --mode json --script test_execution_mode.py
"""

import sys
import os
import json
import subprocess
import argparse
from datetime import datetime, timezone
from pathlib import Path


def create_test_event_data():
    """Create mock EventData similar to what Jellyfin would generate"""
    return {
        "EventType": "PlaybackStart",
        "Timestamp": datetime.now(timezone.utc).isoformat(),
        "UserName": "TestUser",
        "UserId": "12345678-1234-1234-1234-123456789abc",
        "ItemName": "Test Movie",
        "ItemId": "87654321-4321-4321-4321-cba987654321",
        "ItemType": "Movie",
        "ItemPath": "/media/movies/test_movie.mkv",
        "ClientName": "Jellyfin Web",
        "DeviceName": "Firefox Browser",
        "Year": 2024,
        "Genres": "Action, Adventure, Sci-Fi",
        "RuntimeTicks": 72000000000,  # 2 hours in ticks
        "PositionTicks": 0,
        "Rating": "PG-13",
        "Plot": "A test movie for validating script execution functionality."
    }


def execute_script_env_mode(script_path, event_data):
    """Execute script using environment variables (traditional mode)"""
    print("\n" + "="*70)
    print("Testing ExecutionMode: Environment Variables")
    print("="*70 + "\n")

    # Prepare environment variables
    env = os.environ.copy()
    for key, value in event_data.items():
        # Convert to uppercase with underscores (standard env var naming)
        env_key = ''.join(['_' + c.upper() if c.isupper() else c.upper() for c in key]).lstrip('_')
        env[env_key] = str(value)

    print("Environment variables set:")
    for key, value in event_data.items():
        env_key = ''.join(['_' + c.upper() if c.isupper() else c.upper() for c in key]).lstrip('_')
        print(f"  {env_key}={value}")

    # Execute script
    print(f"\nExecuting: python3 {script_path}")
    result = subprocess.run(
        ['python3', str(script_path)],
        env=env,
        capture_output=True,
        text=True,
        timeout=30
    )

    print("\n--- Script Output ---")
    if result.stdout:
        print(result.stdout)
    if result.stderr:
        print("STDERR:", result.stderr)
    print(f"Exit code: {result.returncode}")


def execute_script_args_mode(script_path, event_data):
    """Execute script using command line arguments"""
    print("\n" + "="*70)
    print("Testing ExecutionMode: Command Arguments")
    print("="*70 + "\n")

    # Select a few key fields to pass as arguments
    args = [
        event_data["EventType"],
        event_data["UserName"],
        event_data["ItemName"],
        event_data["ItemId"],
        event_data["Timestamp"]
    ]

    print("Command line arguments:")
    for i, arg in enumerate(args, 1):
        print(f"  arg{i}: {arg}")

    # Execute script
    cmd = ['python3', str(script_path)] + args
    print(f"\nExecuting: {' '.join(cmd)}")
    
    result = subprocess.run(
        cmd,
        capture_output=True,
        text=True,
        timeout=30
    )

    print("\n--- Script Output ---")
    if result.stdout:
        print(result.stdout)
    if result.stderr:
        print("STDERR:", result.stderr)
    print(f"Exit code: {result.returncode}")


def execute_script_json_mode(script_path, event_data):
    """Execute script using JSON payload (new ExecutionMode)"""
    print("\n" + "="*70)
    print("Testing ExecutionMode: JSON Payload")
    print("="*70 + "\n")

    # Serialize event data to JSON
    json_payload = json.dumps(event_data)

    print("JSON payload being passed:")
    print(json.dumps(event_data, indent=2))

    # Execute script with JSON as second argument (first is script path)
    cmd = ['python3', str(script_path), json_payload]
    print(f"\nExecuting: python3 {script_path} '<json_payload>'")
    
    result = subprocess.run(
        cmd,
        capture_output=True,
        text=True,
        timeout=30
    )

    print("\n--- Script Output ---")
    if result.stdout:
        print(result.stdout)
    if result.stderr:
        print("STDERR:", result.stderr)
    print(f"Exit code: {result.returncode}")


def main():
    parser = argparse.ArgumentParser(
        description='Test JellyPy script execution modes standalone',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --mode env --script test_execution_mode.py
  %(prog)s --mode args --script test_execution_mode.py
  %(prog)s --mode json --script test_execution_mode.py
  %(prog)s --mode all --script test_execution_mode.py
        """
    )
    
    parser.add_argument(
        '--mode',
        choices=['env', 'args', 'json', 'all'],
        default='all',
        help='Execution mode to test (default: all)'
    )
    
    parser.add_argument(
        '--script',
        default='test_execution_mode.py',
        help='Path to script to test (default: test_execution_mode.py)'
    )

    args = parser.parse_args()

    # Resolve script path
    script_path = Path(args.script)
    if not script_path.is_absolute():
        # Look in current directory or examples directory
        if script_path.exists():
            script_path = script_path.resolve()
        else:
            examples_path = Path(__file__).parent / script_path
            if examples_path.exists():
                script_path = examples_path
            else:
                print(f"Error: Script not found: {args.script}")
                sys.exit(1)

    if not script_path.exists():
        print(f"Error: Script does not exist: {script_path}")
        sys.exit(1)

    print(f"Testing script: {script_path}")
    
    # Create mock event data
    event_data = create_test_event_data()

    # Execute based on mode
    try:
        if args.mode == 'env' or args.mode == 'all':
            execute_script_env_mode(script_path, event_data)
        
        if args.mode == 'args' or args.mode == 'all':
            execute_script_args_mode(script_path, event_data)
        
        if args.mode == 'json' or args.mode == 'all':
            execute_script_json_mode(script_path, event_data)

        print("\n" + "="*70)
        print("Test completed successfully!")
        print("="*70)

        # Show log file location if it exists
        log_file = Path('/tmp/jellypy_test.log')
        if log_file.exists():
            print(f"\nCheck log file for details: {log_file}")
            print("\nTo view the log:")
            print(f"  cat {log_file}")
            print(f"  tail -f {log_file}  # watch in real-time")

    except subprocess.TimeoutExpired:
        print("\nError: Script execution timed out (30 seconds)")
        sys.exit(1)
    except Exception as e:
        print(f"\nError executing script: {e}")
        sys.exit(1)


if __name__ == '__main__':
    main()
