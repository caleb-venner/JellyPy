#!/usr/bin/env python
# -*- coding: utf-8 -*-

"""
When a user starts watching an episode of a TV show, download the next EPISODE_BUFFER of episodes.
Setup script with
    Triggers: Playback Start
    Conditions: Media Type is episode
    Arguments: -tvid {thetvdb_id} -sn {season_num} -en {episode_num}
"""
from __future__ import print_function
from __future__ import unicode_literals

import argparse
import json
import os
import sys

import requests

# ## EDIT THESE SETTINGS ##
SONARR_APIKEY = '*********************************'  # Your SONARR API key
SONARR_URL = 'http://sonarr:8989'  # Your SONARR URL
RADARR_APIKEY = '*********************************'
RADARR_URL = 'http://radarr:7878'

EPISODE_BUFFER = 6 # The number of next episodes you want to set wanted and search for
SET_WANTED = True # Whether to mark the next episodes as wanted
AUTO_SEARCH = True # Whether to automatically search for the next episodes
MONITOR_FUTURE_EPISODES = True # Whether to monitor future episodes when remaining episodes is less than EPISODE_BUFFER

def get_series_id(tvdbid):
    # Get the metadata for a media item.
    payload = {'tvdbId': tvdbid,
               'includeSeasonImages': False}

    try:
        r = requests.get(SONARR_URL.rstrip('/') + '/api/v3/series', headers=get_headers(), params=payload)
        response = r.json()
        # sys.stderr.write("Sonarr API 'get_series_id' return data: {0}.".format(r))
        return response[0]['id']

    except Exception as e:
        sys.stderr.write("Sonarr API 'get_series_id' request failed: {0}.".format(e))
        pass

def get_episodes(series_id):
    payload = {'seriesId': series_id}

    try:
        r = requests.get(SONARR_URL.rstrip('/') + '/api/v3/episode', headers=get_headers(), params=payload)
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'get_episodes' request failed: {0}.".format(e))
        pass

def get_next_episodes(episodes, season_number, episode_number):
    nextEpisodes = []
    foundCurEpisode = False
    for episode in episodes:
        if foundCurEpisode:
            nextEpisodes.append(episode)
        if len(nextEpisodes) >= EPISODE_BUFFER:
            return nextEpisodes
        if int(episode['seasonNumber']) == int(season_number) and int(episode['episodeNumber']) == int(episode_number):
            foundCurEpisode = True
            episodeId = int(episode['id'])
            set_wanted(episodeId, False)


def set_wanted(episodeId, wanted):
    payload = {'episodeIds': [episodeId], 'monitored':wanted}

    try:
        r = requests.put(SONARR_URL.rstrip('/') + '/api/v3/episode/monitor', headers=get_headers(True), data=json.dumps(payload))
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'set_wanted' request failed: {0}.".format(e))
        pass

def search_episode(episodeId):
    payload = {'episodeIds': [episodeId], 'name':'EpisodeSearch'}
    try:
        r = requests.post(SONARR_URL.rstrip('/') + '/api/v3/command', headers=get_headers(True), data=json.dumps(payload))
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'search_episode' request failed: {0}.".format(e))
        pass

def monitor_future_episodes(series_id):
    payload = {'series':[{'id':series_id}],'monitoringOptions':{'monitor':'future'}}

    try:
        r = requests.post(SONARR_URL.rstrip('/') + '/api/v3/seasonPass', headers=get_headers(True), params=payload)
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'monitor_future_episodes' request failed: {0}.".format(e))
        pass

def get_series(series_id):
    try:
        r = requests.get(SONARR_URL.rstrip('/') + '/api/v3/series/' + str(series_id), headers=get_headers())
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'get_series' request failed: {0}.".format(e))
        pass

def monitor_new_season(series_id):

    payload = get_series(series_id)
    payload['monitored'] = True
    payload['monitorNewItems'] = 'all'
    payload['episodesChanged'] = False

    try:
        r = requests.put(SONARR_URL.rstrip('/') + '/api/v3/series/' + str(series_id), headers=get_headers(True), json=payload)
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Sonarr API 'monitor_new_season' request failed: {0}.".format(e))
        pass

def get_headers(withJson = False):
    headers = {'X-Api-Key': SONARR_APIKEY}
    if withJson:
        headers['Content-Type'] = 'application/json'
    return headers


def get_tvdb_id(series_name):
    # Get the TVdb series id for episode.
    payload = {'term': series_name}

    try:
        r = requests.get(SONARR_URL.rstrip('/') + '/api/v3/series/lookup', headers=get_headers(), params=payload)
        response = r.json()
        # sys.stderr.write("Sonarr API 'get_tvdb_id' : \"{0}\".\n".format(response[0]['tvdbId']))
        return response[0]['tvdbId']

    except Exception as e:
        sys.stderr.write("Sonarr API 'get_tvdb_id' request failed: {0}.\n".format(e))
        pass

#   --------------------------------------------Movies----------------------------------------------------------
def get_movie_headers(withJson = False):
    headers = {'Content-Type': 'application/json', 'X-Api-Key': RADARR_APIKEY}
    # if withJson:
    #     headers['accept'] = 'application/json'
    return headers

def set_movie_wanted(movieID, wanted):
    payload = {'movieIds': [movieID], 'monitored':wanted}

    try:
        r = requests.put(RADARR_URL.rstrip('/') + '/api/v3/movie/editor', headers=get_movie_headers(True), data=json.dumps(payload))
        response = r.json()
        return response

    except Exception as e:
        sys.stderr.write("Radarr API 'set_wanted' request failed: {0}.".format(e))
        pass

def get_movie_id(movie_name):
    # Get the radarr id for movie.
    payload = {'term': movie_name}
    try:
        r = requests.get(RADARR_URL.rstrip('/') + '/api/v3/movie/lookup', headers=get_movie_headers(), params=payload)
        response = r.json()
        return response[0]['id']

    except Exception as e:
        sys.stderr.write("Radarr API 'get_movie_id' request failed: {0}.\n".format(e))
        pass
#   --------------------------------------------Movies----------------------------------------------------------

if __name__ == '__main__':
    # Parse arguments from Jellyfin
    parser = argparse.ArgumentParser()

    parser.add_argument('-mt', '--media_title', action='store', default='',
                        help='The series name')
    parser.add_argument('-sn', '--season_number', action='store', required = False, default='',
                        help='The season number')
    parser.add_argument('-en', '--episode_number', action='store', required = False, default='',
                        help='The episode')

    p = parser.parse_args()

    if not SONARR_APIKEY:
        sys.stderr.write("Missing SONARR_APIKEY environment variable.\n")
    if not RADARR_APIKEY:
        sys.stderr.write("Missing RADARR_APIKEY environment variable.\n")
    if p.media_title:
        if p.season_number:
            if not SONARR_APIKEY:
                sys.stderr.write("Cannot process episodes without SONARR_APIKEY.\n")
                sys.exit(1)
            tvdb_id = get_tvdb_id(p.media_title)
            series_id = get_series_id(tvdb_id)
            episodes = get_episodes(series_id)
            nextEpisodes = get_next_episodes(episodes, p.season_number, p.episode_number)
            if nextEpisodes is not None:
                for episode in nextEpisodes:
                    episodeId = int(episode['id'])
                    if not episode['hasFile']:
                        if SET_WANTED:
                            set_wanted(episodeId, True)
                        if AUTO_SEARCH:
                            search_episode(episodeId)
            if MONITOR_FUTURE_EPISODES and (nextEpisodes is None or len(nextEpisodes) < EPISODE_BUFFER):
                monitor_future_episodes(series_id)
            sys.stderr.write("Done processing")
        else:
            if not RADARR_APIKEY:
                sys.stderr.write("Cannot process movies without RADARR_APIKEY.\n")
                sys.exit(1)
            movie_id = get_movie_id(p.media_title)
            set_movie_wanted(movie_id, False)
            sys.stderr.write("Done processing")
            # Get movieId with Movie Lookup; /api/v3/movie/lookup - payload is term.
            # Set wanted to False -
            # movieIds : ...
            # wanted : False    similar to setWanted for series.
    else:
        sys.stdout.write("No Title Received.")
