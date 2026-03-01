using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyPy.Configuration;
using Jellyfin.Plugin.JellyPy.Events;
using Jellyfin.Plugin.JellyPy.Events.Models;
using Jellyfin.Plugin.JellyPy.Services.Notifications.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyPy.Services.Notifications;

/// <summary>
/// Service for sending notifications via ntfy.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NtfyService"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="httpClientFactory">The HTTP client factory.</param>
public class NtfyService(ILogger<NtfyService> logger, IHttpClientFactory httpClientFactory) : INtfyService
{
    private readonly ILogger<NtfyService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc/>
    public bool IsEnabled()
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            return false;
        }

        return config.EnableNtfyNotifications
            && !string.IsNullOrWhiteSpace(config.NtfyUrl)
            && !string.IsNullOrWhiteSpace(config.NtfyTopic);
    }

    /// <inheritdoc/>
    public async Task<bool> SendItemAddedNotificationAsync(EventData eventData)
    {
        if (!IsEnabled())
        {
            _logger.LogVerbose("ntfy notifications disabled or not configured");
            return false;
        }

        var config = Plugin.Instance!.Configuration;

        // Check if notifications are enabled for this item type
        if (!ShouldNotifyForItemType(eventData.ItemType, config))
        {
            _logger.LogVerbose("Notifications disabled for item type: {ItemType}", eventData.ItemType);
            return false;
        }

        try
        {
            var notification = BuildNotificationFromEventData(eventData, config);

            _logger.LogInformation(
                "Sending ntfy notification for {EventType}: {Title}",
                eventData.EventType,
                notification.Title);

            return await SendNotificationAsync(notification).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ntfy notification for {EventType}", eventData.EventType);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendTestNotificationAsync()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Cannot send test notification - ntfy is not enabled or configured");
            return false;
        }

        var config = Plugin.Instance!.Configuration;

        var notification = new NtfyNotification
        {
            Topic = config.NtfyTopic,
            Title = "JellyPy Test Notification",
            Message = "ntfy integration is working correctly!",
            Priority = 3
        };

        notification.Tags?.Add("white_check_mark");
        notification.Tags?.Add("jellypy");

        _logger.LogInformation("Sending ntfy test notification");
        return await SendNotificationAsync(notification).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> SendQueueingStartedNotificationAsync(string seriesName, int? seasonNumber, int episodeCount, Guid? seriesId)
    {
        if (!IsEnabled())
        {
            _logger.LogVerbose("ntfy notifications disabled or not configured");
            return false;
        }

        var config = Plugin.Instance!.Configuration;

        // Only send if episode notifications are enabled
        if (!config.NtfyNotifyOnEpisodes)
        {
            _logger.LogVerbose("Episode notifications disabled, skipping queueing started notification");
            return false;
        }

        try
        {
            var seasonText = seasonNumber.HasValue ? $"Season {seasonNumber}" : "Unknown Season";
            var notification = new NtfyNotification
            {
                Topic = config.NtfyTopic,
                Title = $"Queueing: {seriesName}",
                Message = $"Collecting {episodeCount}+ episodes for {seasonText}...",
                Priority = Math.Max(1, config.NtfyDefaultPriority - 1) // Slightly lower priority than final notification
            };

            AddTags(notification, "hourglass_flowing_sand", "tv");

            // Add series poster if configured and available
            if (config.NtfyIncludeMediaImage && seriesId.HasValue && seriesId.Value != Guid.Empty && !string.IsNullOrWhiteSpace(config.JellyfinExternalUrl))
            {
                var baseUrl = config.JellyfinExternalUrl.TrimEnd('/');
                notification.Attach = $"{baseUrl}/Items/{seriesId}/Images/Primary?quality=90&maxWidth=400";
                notification.Filename = $"{seriesName}.jpg";
            }

            _logger.LogInformation(
                "Sending ntfy queueing started notification for {SeriesName} {SeasonText} ({EpisodeCount} episodes)",
                seriesName,
                seasonText,
                episodeCount);

            return await SendNotificationAsync(notification).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send queueing started notification for {SeriesName}", seriesName);
            return false;
        }
    }

    private static NtfyNotification BuildNotificationFromEventData(EventData eventData, PluginConfiguration config)
    {
        var notification = new NtfyNotification
        {
            Topic = config.NtfyTopic,
            Priority = config.NtfyDefaultPriority
        };

        // Build notification content based on event type
        switch (eventData.EventType)
        {
            case EventType.ItemAdded:
                BuildItemAddedNotification(notification, eventData, config);
                break;

            case EventType.SeriesEpisodesAdded:
                BuildSeriesEpisodesAddedNotification(notification, eventData, config);
                break;

            default:
                notification.Title = $"New {eventData.ItemType}";
                notification.Message = eventData.ItemName ?? "Unknown item";
                break;
        }

        return notification;
    }

    private static void BuildItemAddedNotification(NtfyNotification notification, EventData eventData, PluginConfiguration config)
    {
        var itemType = eventData.ItemType ?? "Item";

        switch (itemType)
        {
            case "Episode":
                notification.Title = $"New Episode: {eventData.SeriesName}";
                var seasonEp = FormatSeasonEpisode(eventData.SeasonNumber, eventData.EpisodeNumber);
                var episodeName = eventData.AdditionalData.TryGetValue("EpisodeName", out var epName) ? epName?.ToString() : null;
                notification.Message = string.IsNullOrEmpty(episodeName)
                    ? seasonEp
                    : $"{seasonEp} - {episodeName}";
                AddTags(notification, "tv", "new");
                break;

            case "Movie":
                var year = eventData.Year.HasValue ? $" ({eventData.Year})" : string.Empty;
                notification.Title = "New Movie Added";
                notification.Message = $"{eventData.ItemName}{year}";
                AddTags(notification, "movie_camera", "new");
                break;

            case "Season":
                notification.Title = $"New Season: {eventData.SeriesName}";
                notification.Message = eventData.SeasonNumber.HasValue
                    ? $"Season {eventData.SeasonNumber}"
                    : eventData.ItemName ?? "New Season";
                AddTags(notification, "tv", "new");
                break;

            case "Series":
                notification.Title = "New Series Added";
                notification.Message = eventData.SeriesName ?? eventData.ItemName ?? "Unknown Series";
                AddTags(notification, "tv", "new");
                break;

            default:
                notification.Title = $"New {itemType} Added";
                notification.Message = eventData.ItemName ?? "Unknown item";
                AddTags(notification, "new");
                break;
        }

        // Add poster/thumbnail if configured and available
        AddMediaImage(notification, eventData, config);
    }

    private static void BuildSeriesEpisodesAddedNotification(NtfyNotification notification, EventData eventData, PluginConfiguration config)
    {
        notification.Title = $"New Episodes: {eventData.SeriesName}";

        var episodeCount = eventData.EpisodeGroupCount ?? eventData.Episodes.Count;

        if (!string.IsNullOrEmpty(eventData.EpisodeRange))
        {
            notification.Message = $"{episodeCount} episode{(episodeCount != 1 ? "s" : string.Empty)} added: {eventData.EpisodeRange}";
        }
        else
        {
            notification.Message = $"{episodeCount} new episode{(episodeCount != 1 ? "s" : string.Empty)} added";
        }

        AddTags(notification, "tv", "new", "package");

        // Add series poster if configured
        AddMediaImage(notification, eventData, config);
    }

    private static bool ShouldNotifyForItemType(string? itemType, PluginConfiguration config)
    {
        if (string.IsNullOrEmpty(itemType))
        {
            return false;
        }

        return itemType switch
        {
            "Episode" => config.NtfyNotifyOnEpisodes,
            "SeriesEpisodes" => config.NtfyNotifyOnEpisodes,
            "Movie" => config.NtfyNotifyOnMovies,
            "Season" => config.NtfyNotifyOnSeasons,
            "Series" => config.NtfyNotifyOnSeries,
            _ => false
        };
    }

    private static void AddTags(NtfyNotification notification, params string[] tags)
    {
        if (notification.Tags == null)
        {
            return;
        }

        foreach (var tag in tags)
        {
            notification.Tags.Add(tag);
        }
    }

    private static void AddMediaImage(NtfyNotification notification, EventData eventData, PluginConfiguration config)
    {
        if (!config.NtfyIncludeMediaImage || string.IsNullOrWhiteSpace(config.JellyfinExternalUrl))
        {
            return;
        }

        // Determine which item ID to use for the image
        Guid? imageItemId = null;

        // For episodes, always use the series ID for the poster (both for single episode and episode groups)
        if (eventData.ItemType == "Episode" || eventData.EventType == EventType.SeriesEpisodesAdded)
        {
            if (eventData.AdditionalData.TryGetValue("SeriesId", out var seriesIdObj))
            {
                if (seriesIdObj is Guid seriesId && seriesId != Guid.Empty)
                {
                    imageItemId = seriesId;
                }
                else if (seriesIdObj is string seriesIdStr && Guid.TryParse(seriesIdStr, out var parsedSeriesId) && parsedSeriesId != Guid.Empty)
                {
                    imageItemId = parsedSeriesId;
                }
            }
        }

        // Fall back to the item ID
        imageItemId ??= eventData.ItemId;

        // Validate that we have a non-empty GUID
        if (!imageItemId.HasValue || imageItemId.Value == Guid.Empty)
        {
            return;
        }

        // Construct Jellyfin image URL
        var baseUrl = config.JellyfinExternalUrl.TrimEnd('/');
        var imageUrl = $"{baseUrl}/Items/{imageItemId}/Images/Primary?quality=90&maxWidth=400";

        notification.Attach = imageUrl;
        notification.Filename = $"{eventData.ItemName ?? "media"}.jpg";
    }

    private static string FormatSeasonEpisode(int? season, int? episode)
    {
        if (!season.HasValue && !episode.HasValue)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        if (season.HasValue)
        {
            sb.Append(CultureInfo.InvariantCulture, $"S{season.Value:D2}");
        }

        if (episode.HasValue)
        {
            sb.Append(CultureInfo.InvariantCulture, $"E{episode.Value:D2}");
        }

        return sb.ToString();
    }

    private async Task<bool> SendNotificationAsync(NtfyNotification notification)
    {
        var config = Plugin.Instance!.Configuration;

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Configure authentication if provided
            ConfigureAuthentication(client, config, _logger);

            var url = config.NtfyUrl.TrimEnd('/');
            var json = JsonSerializer.Serialize(notification, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogVerbose("ntfy notification sent successfully to topic {Topic}", notification.Topic);
                return true;
            }

            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogWarning(
                "ntfy notification failed with status {StatusCode}: {Response}",
                response.StatusCode,
                responseBody);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when sending ntfy notification");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out when sending ntfy notification");
            return false;
        }
    }

    private static void ConfigureAuthentication(HttpClient client, PluginConfiguration config, ILogger logger)
    {
        // Check for access token first (preferred)
        var accessToken = config.NtfyAccessToken;
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogDebug("Using ntfy access token authentication");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return;
        }

        // Fall back to username/password
        var username = config.NtfyUsername;
        var password = config.NtfyPassword;

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            logger.LogDebug("Using ntfy basic authentication for user: {Username}", username);
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
        else if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("ntfy authentication incomplete - username or password is missing");
        }
        else
        {
            logger.LogDebug("No ntfy authentication configured (using public server)");
        }
    }
}
