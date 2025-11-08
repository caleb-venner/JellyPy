#!/usr/bin/env python3

"""
JellyPy Discord Notification Script

This script sends a notification to a Discord webhook when an item is added to Jellyfin.
It's designed to be called by the JellyPy plugin.

Setup:
1. Install the 'requests' library:
   pip install requests (if Jellyfin deployed via a docker container use lsio version with docker mods.)

2. Set the DISCORD_WEBHOOK_URL environment variable to your Discord webhook URL.
   This can be done in the JellyPy plugin settings under "Environment Variables" for the script setting.

3. Configure a script setting in JellyPy:
   - Trigger: ItemAdded, SeriesEpisodesAdded
   - Executor: Python
   - Script Path: /path/to/this/discord_notification.py

The script receives a single command-line argument: a JSON string containing the event data.
"""

import json
import os
import sys
import requests

def get_webhook_url():
    """Gets the Discord webhook URL from the environment variable."""
    url = os.environ.get("DISCORD_WEBHOOK_URL")
    if not url:
        print("Error: DISCORD_WEBHOOK_URL environment variable not set.", file=sys.stderr)
        sys.exit(1)
    return url

def format_movie_message(event_data):
    """Formats a Discord message for a single movie."""
    item_name = event_data.get("ItemName", "Unknown Movie")
    year = event_data.get("Year")
    title = f"{item_name} ({year})" if year else item_name
    genres = ", ".join(event_data.get("Genres", []))
    rating = event_data.get("ContentRating", "Not Rated")
    runtime_ticks = event_data.get("RunTimeTicks", 0)
    runtime = f"{runtime_ticks // 36000000000}h {(runtime_ticks // 600000000) % 60}m" if runtime_ticks else "N/A"

    embed = {
        "title": title,
        "description": f"'{item_name}' has been added to Jellyfin.",
        "color": 0x00A4DC,  # Blue
        "fields": [
            {"name": "Type", "value": "Movie", "inline": True},
            {"name": "Genres", "value": genres, "inline": True},
            {"name": "Rating", "value": rating, "inline": True},
            {"name": "Runtime", "value": runtime, "inline": True}
        ]
    }
    return {"embeds": [embed]}

def format_episode_message(event_data):
    """Formats a Discord message for a single episode."""
    series_name = event_data.get("SeriesName", "Unknown Series")
    season_number = event_data.get("SeasonNumber")
    episode_number = event_data.get("EpisodeNumber")
    item_name = event_data.get("ItemName", "Unknown Episode")

    title = f"{series_name} - S{season_number:02d}E{episode_number:02d}"
    
    embed = {
        "title": title,
        "description": f"'{item_name}' has been added to Jellyfin.",
        "color": 0x8E44AD,  # Purple
        "fields": [
            {"name": "Type", "value": "Episode", "inline": True},
            {"name": "Series", "value": series_name, "inline": True}
        ]
    }
    return {"embeds": [embed]}

def format_grouped_episodes_message(event_data):
    """Formats a Discord message for a group of episodes."""
    series_name = event_data.get("SeriesName", "Unknown Series")
    episode_count = event_data.get("EpisodeGroupCount", 0)
    episode_range = event_data.get("EpisodeRange", "")
    season_range = event_data.get("SeasonRange", "")
    genres = ", ".join(event_data.get("Genres", []))
    rating = event_data.get("ContentRating", "Not Rated")

    title = f"{series_name} - {episode_count} New Episodes Added"
    
    description = f"**{series_name}** has {episode_count} new episodes available."

    embed = {
        "title": title,
        "description": description,
        "color": 0x8E44AD,  # Purple
        "fields": [
            {"name": "Type", "value": "TV Show Season/Episodes", "inline": True},
            {"name": "Series", "value": series_name, "inline": True},
            {"name": "Season(s)", "value": season_range, "inline": True},
            {"name": "Episodes", "value": episode_range, "inline": True},
            {"name": "Genres", "value": genres, "inline": True},
            {"name": "Rating", "value": rating, "inline": True}
        ]
    }
    return {"embeds": [embed]}

def format_playback_start_message(event_data):
    """Formats a Discord message for a playback start event."""
    item_name = event_data.get("ItemName", "Unknown Item")
    item_type = event_data.get("ItemType", "Unknown Type")
    user_name = event_data.get("Username", "Unknown User")
    client_name = event_data.get("ClientName", "Unknown Client")

    title = f"Playback Started: {item_name}"
    
    description = f"**{user_name}** started playing **{item_name}** on **{client_name}**."

    embed = {
        "title": title,
        "description": description,
        "color": 0x32CD32,  # Green
        "fields": [
            {"name": "Type", "value": item_type, "inline": True},
            {"name": "User", "value": user_name, "inline": True},
            {"name": "Client", "value": client_name, "inline": True}
        ]
    }
    return {"embeds": [embed]}

def send_discord_notification(webhook_url, payload):
    """Sends the payload to the Discord webhook."""
    try:
        response = requests.post(webhook_url, json=payload, timeout=10)
        response.raise_for_status()
        print(f"Successfully sent notification to Discord. Status: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"Error sending notification to Discord: {e}", file=sys.stderr)
        sys.exit(1)

def main():
    """Main function."""
    if len(sys.argv) < 2:
        print("Error: No JSON data provided.", file=sys.stderr)
        print("Usage: python discord_notification.py '<json_data>'", file=sys.stderr)
        sys.exit(1)

    webhook_url = get_webhook_url()
    
    try:
        event_data = json.loads(sys.argv[1])
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON data provided. {e}", file=sys.stderr)
        sys.exit(1)

    event_type = event_data.get("EventType")
    payload = None

    if event_type == "SeriesEpisodesAdded":
        payload = format_grouped_episodes_message(event_data)
    elif event_type == "ItemAdded":
        item_type = event_data.get("ItemType")
        if item_type == "Movie":
            payload = format_movie_message(event_data)
        elif item_type == "Episode":
            payload = format_episode_message(event_data)
        else:
            print(f"Ignoring ItemAdded event for unhandled item type: {item_type}")
            return # Not an error, just not handling this type
    elif event_type == "PlaybackStart":
        payload = format_playback_start_message(event_data)
    else:
        print(f"Ignoring unhandled event type: {event_type}")
        return # Not an error, just not handling this type

    if payload:
        send_discord_notification(webhook_url, payload)

if __name__ == "__main__":
    main()
