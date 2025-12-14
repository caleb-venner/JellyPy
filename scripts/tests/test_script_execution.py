#!/usr/bin/env python3
"""
JellyPy Script Execution Test Script

This script is designed to test and debug script execution functionality.
It logs all received data and verifies that the script execution system is
passing data correctly to scripts.

Usage:
1. Configure this script path in JellyPy Script Settings
2. Set various event triggers (PlaybackStart, ItemAdded, etc.)
3. Configure data attributes to pass event information
4. Trigger events in Jellyfin to test
5. Check Jellyfin logs to verify output

This script will log:
- All environment variables received
- All command line arguments received
- Timestamp of execution
- Basic validation of data format
"""

import os
import sys
import json
import logging
from datetime import datetime
from pathlib import Path

# Configure logging to both file and stderr
log_dir = Path('/tmp/jellypy_test')
log_dir.mkdir(exist_ok=True)
log_file = log_dir / 'test_execution.log'

logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(log_file),
        logging.StreamHandler(sys.stderr)
    ]
)

logger = logging.getLogger('JellyPy-ScriptTest')


def log_separator(title):
    """Log a section separator"""
    logger.info('=' * 70)
    logger.info(f' {title}')
    logger.info('=' * 70)


def log_environment_variables():
    """Log all environment variables"""
    log_separator('ENVIRONMENT VARIABLES')

    env_vars = dict(os.environ)
    
    # Log all variables
    for key in sorted(env_vars.keys()):
        value = env_vars[key]
        # Truncate long values for readability
        display_value = value if len(value) < 200 else value[:200] + '...'
        logger.info(f'{key}: {display_value}')

    logger.info(f'Total environment variables: {len(env_vars)}')


def log_command_line_arguments():
    """Log all command line arguments"""
    log_separator('COMMAND LINE ARGUMENTS')

    logger.info(f'Script name: {sys.argv[0]}')
    logger.info(f'Total arguments: {len(sys.argv) - 1}')

    for idx, arg in enumerate(sys.argv[1:], 1):
        # Try to parse as JSON if it looks like JSON
        try:
            if arg.startswith('{') or arg.startswith('['):
                parsed = json.loads(arg)
                logger.info(f'Argument {idx} (JSON): {json.dumps(parsed, indent=2)}')
            else:
                logger.info(f'Argument {idx}: {arg}')
        except json.JSONDecodeError:
            logger.info(f'Argument {idx}: {arg}')


def extract_jellpy_variables(json_payload=None):
    """Extract and log JellyPy-specific variables from env or JSON"""
    log_separator('JELLYPY EVENT DATA')

    # List of expected JellyPy environment variables
    jellypy_vars = [
        'EVENT_TYPE',
        'TIMESTAMP',
        'USER_NAME',
        'USER_ID',
        'ITEM_NAME',
        'ITEM_ID',
        'ITEM_TYPE',
        'CLIENT_NAME',
        'DEVICE_NAME',
        'SERIES_NAME',
        'SEASON_NUMBER',
        'EPISODE_NUMBER',
        'YEAR',
        'GENRES',
        'RUNTIME_TICKS',
        'POSITION_TICKS',
        'RATING',
        'PLOT',
    ]

    event_data = {}
    found_count = 0

    if json_payload:
        for var in jellypy_vars:
            # map uppercase env names to JSON keys (EventType -> EVENT_TYPE)
            key_guess = var.title().replace('_', '')
            value = json_payload.get(key_guess)
            if value is not None:
                event_data[var] = str(value)
                logger.info(f'{var}: {value}')
                found_count += 1
            else:
                logger.debug(f'{var}: (not provided)')
    else:
        for var in jellypy_vars:
            value = os.getenv(var)
            if value is not None:
                event_data[var] = value
                logger.info(f'{var}: {value}')
                found_count += 1
            else:
                logger.debug(f'{var}: (not provided)')

    logger.info(f'Found {found_count}/{len(jellypy_vars)} expected JellyPy variables')

    return event_data


def validate_event_data(event_data):
    """Validate the received event data"""
    log_separator('EVENT DATA VALIDATION')

    validation_results = []

    # Check for required fields
    required_fields = ['EVENT_TYPE', 'TIMESTAMP']
    for field in required_fields:
        if field in event_data:
            logger.info(f'✓ Required field {field} present')
            validation_results.append(True)
        else:
            logger.warning(f'✗ Required field {field} missing')
            validation_results.append(False)

    # Validate timestamp format
    if 'TIMESTAMP' in event_data:
        try:
            # Try to parse as ISO format
            datetime.fromisoformat(event_data['TIMESTAMP'].replace('Z', '+00:00'))
            logger.info('✓ Timestamp is valid ISO format')
            validation_results.append(True)
        except ValueError:
            logger.warning(f'✗ Timestamp format invalid: {event_data["TIMESTAMP"]}')
            validation_results.append(False)

    # Validate event type
    if 'EVENT_TYPE' in event_data:
        valid_types = [
            'PlaybackStart', 'PlaybackStop', 'PlaybackPause', 'PlaybackResume',
            'ItemAdded', 'ItemUpdated', 'ItemRemoved',
            'UserCreated', 'UserUpdated', 'UserDeleted',
            'SessionStart', 'SessionEnd',
            'ServerStartup', 'ServerShutdown'
        ]
        if event_data['EVENT_TYPE'] in valid_types:
            logger.info(f'✓ Event type is valid: {event_data["EVENT_TYPE"]}')
            validation_results.append(True)
        else:
            logger.warning(f'✗ Unknown event type: {event_data["EVENT_TYPE"]}')
            validation_results.append(False)

    # Check for item information on playback events
    event_type = event_data.get('EVENT_TYPE', '')
    if 'Playback' in event_type or 'Item' in event_type:
        if 'ITEM_NAME' in event_data:
            logger.info(f'✓ Item name provided: {event_data["ITEM_NAME"]}')
            validation_results.append(True)
        else:
            logger.warning('✗ No item name provided for item/playback event')
            validation_results.append(False)

    # Summary
    passed = sum(validation_results)
    total = len(validation_results)
    logger.info(f'Validation result: {passed}/{total} checks passed')


def generate_summary_report(event_data):
    """Generate a summary report of the execution"""
    log_separator('EXECUTION SUMMARY')

    report = {
        'timestamp': datetime.now().isoformat(),
        'event_type': event_data.get('EVENT_TYPE', 'Unknown'),
        'user': event_data.get('USER_NAME', 'Unknown'),
        'item': event_data.get('ITEM_NAME', 'Unknown'),
        'item_type': event_data.get('ITEM_TYPE', 'Unknown'),
        'script_path': sys.argv[0],
        'working_directory': os.getcwd(),
        'environment_vars_count': len(os.environ),
        'command_args_count': len(sys.argv) - 1,
        'log_file': str(log_file),
    }

    logger.info('Event Processing Summary:')
    for key, value in report.items():
        logger.info(f'  {key}: {value}')

    # Write summary to JSON file for easy parsing
    summary_file = log_dir / f'summary_{datetime.now().strftime("%Y%m%d_%H%M%S")}.json'
    with open(summary_file, 'w') as f:
        json.dump(report, f, indent=2)
    logger.info(f'Summary written to: {summary_file}')


def main():
    """Main entry point"""
    try:
        logger.info('JellyPy Script Execution Test Started')
        logger.info(f'Python version: {sys.version}')
        logger.info(f'Log file: {log_file}')

        json_payload = None
        if len(sys.argv) > 1:
            try:
                json_payload = json.loads(sys.argv[1])
                logger.info('Detected JsonPayload argument')
            except json.JSONDecodeError:
                logger.info('First argument is not JSON; proceeding with env/args')

        # Collect and log all data
        log_environment_variables()
        log_command_line_arguments()
        event_data = extract_jellpy_variables(json_payload)

        # Validate the data
        validate_event_data(event_data)

        # Generate summary
        generate_summary_report(event_data)

        logger.info('JellyPy Script Execution Test Completed Successfully')
        logger.info(f'Results written to: {log_file}')
        sys.exit(0)

    except Exception as e:
        logger.exception(f'Error during test execution: {e}')
        sys.exit(1)


if __name__ == '__main__':
    main()
