#!/usr/bin/env python3
"""
Basic JellyPy Notification Script

This script demonstrates how to handle Jellyfin events using the JellyPy plugin.
It receives event data through environment variables and command line arguments.

Usage:
1. Configure this script path in JellyPy Advanced Script Settings
2. Set event triggers (e.g., PlaybackStart, ItemAdded)
3. Configure data attributes to pass event information
4. The script will be called automatically when events occur

Environment Variables Available:
- EVENT_TYPE: Type of event (PlaybackStart, ItemAdded, etc.)
- TIMESTAMP: When the event occurred
- USER_NAME: Name of the user who triggered the event
- ITEM_NAME: Name of the media item
- ITEM_TYPE: Type of media (Movie, Episode, etc.)
- CLIENT_NAME: Name of the playback client/device
- And many more based on your data attribute configuration

Command Line Arguments:
- Script can also receive data as JSON arguments
- Use --help to see available options
"""

import os
import sys
import json
import logging
from datetime import datetime
from typing import Dict, Any, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/tmp/jellypy.log'),
        logging.StreamHandler()
    ]
)

logger = logging.getLogger('JellyPy-Notification')


class JellyPyEventHandler:
    """Handler for JellyPy events"""

    def __init__(self):
        self.event_data = self._gather_event_data()

    def _gather_event_data(self) -> Dict[str, Any]:
        """Gather event data from environment variables and arguments"""

        # Get data from environment variables
        env_data = {
            'event_type': os.getenv('EVENT_TYPE', 'Unknown'),
            'timestamp': os.getenv('TIMESTAMP', datetime.now().isoformat()),
            'user_name': os.getenv('USER_NAME', 'System'),
            'item_name': os.getenv('ITEM_NAME', 'Unknown'),
            'item_type': os.getenv('ITEM_TYPE', 'Unknown'),
            'client_name': os.getenv('CLIENT_NAME', ''),
            'device_name': os.getenv('DEVICE_NAME', ''),
            'series_name': os.getenv('SERIES_NAME', ''),
            'season_number': os.getenv('SEASON_NUMBER', ''),
            'episode_number': os.getenv('EPISODE_NUMBER', ''),
            'year': os.getenv('YEAR', ''),
            'genres': os.getenv('GENRES', ''),
        }

        # Check for JSON argument data
        if len(sys.argv) > 1:
            try:
                json_data = json.loads(sys.argv[1])
                env_data.update(json_data)
                logger.info("Loaded additional data from JSON argument")
            except json.JSONDecodeError:
                logger.warning("Failed to parse JSON argument")

        return env_data

    def handle_event(self):
        """Main event handling logic"""

        event_type = self.event_data['event_type']

        logger.info(f"Processing {event_type} event")
        logger.info(f"Event data: {json.dumps(self.event_data, indent=2)}")

        # Route to specific handlers based on event type
        if event_type == 'PlaybackStart':
            self._handle_playback_start()
        elif event_type == 'PlaybackStop':
            self._handle_playback_stop()
        elif event_type == 'ItemAdded':
            self._handle_item_added()
        elif event_type == 'UserCreated':
            self._handle_user_created()
        else:
            self._handle_generic_event()

    def _handle_playback_start(self):
        """Handle playback start events"""
        user = self.event_data['user_name']
        item = self.event_data['item_name']
        item_type = self.event_data['item_type']
        client = self.event_data['client_name']

        message = f"🎬 {user} started watching {item_type.lower()}: '{item}'"
        if client:
            message += f" on {client}"

        logger.info(message)

        # Add your custom logic here:
        # - Send notifications (Discord, Slack, email, etc.)
        # - Log to external systems
        # - Update watch statistics
        # - Trigger other automations

        self._send_notification(f"Playback Started", message)

        # Example: Special handling for movies vs episodes
        if item_type == 'Movie':
            self._handle_movie_playback(item)
        elif item_type == 'Episode':
            self._handle_episode_playback(item)

    def _handle_playback_stop(self):
        """Handle playback stop events"""
        user = self.event_data['user_name']
        item = self.event_data['item_name']

        message = f"⏹️ {user} stopped watching: '{item}'"
        logger.info(message)

        self._send_notification("Playback Stopped", message)

        # Add your custom logic here:
        # - Mark content as watched
        # - Update progress tracking
        # - Clean up temporary files
        # - Generate viewing reports

    def _handle_item_added(self):
        """Handle new item added to library"""
        item = self.event_data['item_name']
        item_type = self.event_data['item_type']

        message = f"📚 New {item_type.lower()} added to library: '{item}'"
        logger.info(message)

        self._send_notification("Library Update", message)

        # Add your custom logic here:
        # - Process new media files
        # - Update metadata
        # - Notify users of new content
        # - Trigger download completion workflows

    def _handle_user_created(self):
        """Handle new user creation"""
        user = self.event_data['user_name']

        message = f"👋 Welcome new user: {user}"
        logger.info(message)

        self._send_notification("New User", message)

        # Add your custom logic here:
        # - Send welcome emails
        # - Set up user preferences
        # - Add to external systems
        # - Configure permissions

    def _handle_generic_event(self):
        """Handle any other event types"""
        event_type = self.event_data['event_type']

        message = f"ℹ️ Generic event: {event_type}"
        logger.info(message)

        # Add your custom logic for other event types

    def _handle_movie_playback(self, movie_title: str):
        """Special handling for movie playback"""
        year = self.event_data.get('year', '')
        genres = self.event_data.get('genres', '')

        logger.info(f"Movie playback: {movie_title} ({year}) - Genres: {genres}")

        # Example: Movie-specific automations
        # - Update Radarr watched status
        # - Log to movie tracking systems
        # - Generate movie recommendations

    def _handle_episode_playback(self, episode_title: str):
        """Special handling for episode playback"""
        series = self.event_data.get('series_name', '')
        season = self.event_data.get('season_number', '')
        episode = self.event_data.get('episode_number', '')

        logger.info(f"Episode playback: {series} S{season}E{episode} - {episode_title}")

        # Example: Episode-specific automations
        # - Update Sonarr watched status
        # - Track binge-watching patterns
        # - Download next episodes
        # - Update series progress

    def _send_notification(self, title: str, message: str):
        """Send notifications to external services"""

        # Example notification implementations:

        # 1. Log to file (always happens)
        logger.info(f"NOTIFICATION: {title} - {message}")

        # 2. Send to Discord webhook (if configured)
        discord_webhook = os.getenv('DISCORD_WEBHOOK_URL')
        if discord_webhook:
            self._send_discord_notification(discord_webhook, title, message)

        # 3. Send email (if configured)
        smtp_server = os.getenv('SMTP_SERVER')
        if smtp_server:
            self._send_email_notification(title, message)

        # 4. Write to system notification
        try:
            os.system(f'notify-send "{title}" "{message}"')
        except:
            pass  # Ignore if notify-send is not available

    def _send_discord_notification(self, webhook_url: str, title: str, message: str):
        """Send notification to Discord webhook"""
        try:
            import requests

            payload = {
                "embeds": [{
                    "title": title,
                    "description": message,
                    "color": 0x00ff00,  # Green color
                    "timestamp": datetime.now().isoformat()
                }]
            }

            response = requests.post(webhook_url, json=payload, timeout=10)
            if response.status_code == 204:
                logger.info("Discord notification sent successfully")
            else:
                logger.warning(f"Discord notification failed: {response.status_code}")

        except ImportError:
            logger.warning("requests library not available for Discord notifications")
        except Exception as e:
            logger.error(f"Failed to send Discord notification: {e}")

    def _send_email_notification(self, title: str, message: str):
        """Send email notification"""
        try:
            import smtplib
            from email.mime.text import MIMEText

            smtp_server = os.getenv('SMTP_SERVER')
            smtp_port = int(os.getenv('SMTP_PORT', '587'))
            smtp_user = os.getenv('SMTP_USER')
            smtp_pass = os.getenv('SMTP_PASS')
            email_to = os.getenv('EMAIL_TO')

            if not all([smtp_server, smtp_user, smtp_pass, email_to]):
                logger.warning("Email configuration incomplete")
                return

            msg = MIMEText(message)
            msg['Subject'] = f"JellyPy: {title}"
            msg['From'] = smtp_user
            msg['To'] = email_to

            with smtplib.SMTP(smtp_server, smtp_port) as server:
                server.starttls()
                server.login(smtp_user, smtp_pass)
                server.send_message(msg)

            logger.info("Email notification sent successfully")

        except Exception as e:
            logger.error(f"Failed to send email notification: {e}")


def main():
    """Main entry point"""

    # Handle command line arguments
    if len(sys.argv) > 1 and sys.argv[1] in ['--help', '-h']:
        print(__doc__)
        sys.exit(0)

    try:
        # Create event handler and process the event
        handler = JellyPyEventHandler()
        handler.handle_event()

        logger.info("Event processing completed successfully")

    except Exception as e:
        logger.error(f"Error processing event: {e}", exc_info=True)
        sys.exit(1)


if __name__ == '__main__':
    main()
