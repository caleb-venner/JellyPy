#!/usr/bin/env python3
"""
JellyPy Event Type Validator

This script validates that the correct event data is passed for specific
event types. Use this to test individual event triggers.

Each event type has specific required and optional data fields. This script
verifies that the data received matches expectations for the event type.

Event Types Tested:
  • PlaybackStart/Stop/Pause/Resume - Playback events
  • ItemAdded/Updated/Removed - Library events
  • UserCreated/Updated/Deleted - User events
  • SessionStart/End - Session events
  • ServerStartup/Shutdown - Server events

Usage:
Configure this script for specific event triggers you want to test,
then trigger those events in Jellyfin.
"""

import os
import sys
import json
import logging
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Tuple

# Setup logging
log_dir = Path('/tmp/jellypy_test')
log_dir.mkdir(exist_ok=True)
log_file = log_dir / 'event_validator.log'

logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(log_file),
        logging.StreamHandler(sys.stderr)
    ]
)

logger = logging.getLogger('JellyPy-EventValidator')


class EventValidator:
    """Validates event data for different event types"""

    # Define expected fields for each event type
    EVENT_REQUIREMENTS = {
        'PlaybackStart': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME', 'ITEM_NAME', 'ITEM_TYPE'],
            'optional': ['CLIENT_NAME', 'DEVICE_NAME', 'POSITION_TICKS', 'RUNTIME_TICKS'],
            'description': 'When playback of media begins'
        },
        'PlaybackStop': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME', 'ITEM_NAME'],
            'optional': ['CLIENT_NAME', 'POSITION_TICKS', 'RUNTIME_TICKS'],
            'description': 'When playback stops'
        },
        'PlaybackPause': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME', 'ITEM_NAME'],
            'optional': ['CLIENT_NAME', 'POSITION_TICKS'],
            'description': 'When playback is paused'
        },
        'PlaybackResume': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME', 'ITEM_NAME'],
            'optional': ['CLIENT_NAME', 'POSITION_TICKS'],
            'description': 'When playback resumes from pause'
        },
        'ItemAdded': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'ITEM_NAME', 'ITEM_TYPE'],
            'optional': ['YEAR', 'GENRES', 'RATING'],
            'description': 'When new media is added to library'
        },
        'ItemUpdated': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'ITEM_NAME', 'ITEM_TYPE'],
            'optional': ['YEAR', 'GENRES', 'RATING'],
            'description': 'When media metadata is updated'
        },
        'ItemRemoved': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'ITEM_NAME'],
            'optional': ['ITEM_TYPE'],
            'description': 'When media is removed from library'
        },
        'UserCreated': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME'],
            'optional': [],
            'description': 'When new user is created'
        },
        'UserUpdated': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME'],
            'optional': [],
            'description': 'When user settings are updated'
        },
        'UserDeleted': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME'],
            'optional': [],
            'description': 'When user is deleted'
        },
        'SessionStart': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME'],
            'optional': ['CLIENT_NAME'],
            'description': 'When user session starts'
        },
        'SessionEnd': {
            'required': ['EVENT_TYPE', 'TIMESTAMP', 'USER_NAME'],
            'optional': [],
            'description': 'When user session ends'
        },
        'ServerStartup': {
            'required': ['EVENT_TYPE', 'TIMESTAMP'],
            'optional': [],
            'description': 'When Jellyfin server starts'
        },
        'ServerShutdown': {
            'required': ['EVENT_TYPE', 'TIMESTAMP'],
            'optional': [],
            'description': 'When Jellyfin server shuts down'
        },
    }

    def __init__(self):
        self.event_data = self._collect_event_data()
        self.event_type = self.event_data.get('EVENT_TYPE', 'Unknown')
        self.validation_results = []

    def _collect_event_data(self) -> Dict[str, str]:
        """Collect event data from environment variables"""
        data = {}
        for key, value in os.environ.items():
            # Only collect uppercase environment variables (JellyPy convention)
            if key.isupper() and not key.startswith('_'):
                data[key] = value
        return data

    def validate(self) -> bool:
        """Run validation for the event type"""
        logger.info('=' * 70)
        logger.info(f'Event Type: {self.event_type}')
        logger.info('=' * 70)

        if self.event_type not in self.EVENT_REQUIREMENTS:
            logger.warning(f'Unknown event type: {self.event_type}')
            self._log_all_data()
            return False

        requirements = self.EVENT_REQUIREMENTS[self.event_type]
        logger.info(f'Description: {requirements["description"]}')
        logger.info('')

        # Log requirements
        logger.info('Expected Data:')
        logger.info(f'  Required fields: {", ".join(requirements["required"])}')
        logger.info(f'  Optional fields: {", ".join(requirements["optional"])}')
        logger.info('')

        # Validate required fields
        logger.info('Validation Results:')
        required_valid = self._validate_required_fields(requirements['required'])
        optional_info = self._validate_optional_fields(requirements['optional'])

        # Log received data
        logger.info('')
        logger.info('Received Data:')
        for key in sorted(self.event_data.keys()):
            value = self.event_data[key]
            display_value = value if len(value) < 100 else value[:100] + '...'
            logger.info(f'  {key}: {display_value}')

        # Summary
        logger.info('')
        logger.info('Summary:')
        passed = sum(1 for _, result in self.validation_results if result)
        total = len(self.validation_results)
        logger.info(f'  Validation: {passed}/{total} checks passed')

        # Recommendations
        logger.info('')
        logger.info('Recommendations:')
        if required_valid:
            logger.info('  ✓ All required fields are present')
        else:
            logger.info('  • Configure missing required data attributes in JellyPy settings')
            logger.info('  • Verify event is being triggered correctly')
            logger.info('  • Check Jellyfin logs for event processing errors')

        if optional_info:
            logger.info('  • Optional fields would provide additional context')
            logger.info('  • Consider adding optional data attributes for richer information')

        return required_valid

    def _validate_required_fields(self, required_fields: List[str]) -> bool:
        """Validate that all required fields are present"""
        all_present = True
        for field in required_fields:
            if field in self.event_data and self.event_data[field]:
                logger.info(f'  ✓ {field}: {self.event_data[field]}')
                self.validation_results.append((field, True))
            else:
                logger.warning(f'  ✗ {field}: MISSING')
                self.validation_results.append((field, False))
                all_present = False
        return all_present

    def _validate_optional_fields(self, optional_fields: List[str]) -> bool:
        """Log optional fields if present"""
        has_optional = False
        for field in optional_fields:
            if field in self.event_data and self.event_data[field]:
                logger.info(f'  ◆ {field} (optional): {self.event_data[field]}')
                has_optional = True
        return has_optional

    def _log_all_data(self):
        """Log all received data when event type is unknown"""
        logger.info('Received Environment Data:')
        for key in sorted(self.event_data.keys()):
            logger.info(f'  {key}: {self.event_data[key]}')


class EventTypeDocumentation:
    """Provides documentation for event types"""

    @staticmethod
    def print_documentation():
        """Print documentation for all event types"""
        logger.info('')
        logger.info('=' * 70)
        logger.info('Event Type Reference')
        logger.info('=' * 70)

        for event_type, info in EventValidator.EVENT_REQUIREMENTS.items():
            logger.info('')
            logger.info(f'{event_type}:')
            logger.info(f'  Description: {info["description"]}')
            logger.info(f'  Required: {", ".join(info["required"])}')
            if info["optional"]:
                logger.info(f'  Optional: {", ".join(info["optional"])}')


def main():
    """Main entry point"""
    logger.info('JellyPy Event Type Validator Started')
    logger.info(f'Python version: {sys.version}')

    validator = EventValidator()
    is_valid = validator.validate()

    EventTypeDocumentation.print_documentation()

    logger.info('')
    logger.info(f'Results written to: {log_file}')

    sys.exit(0 if is_valid else 1)


if __name__ == '__main__':
    main()
