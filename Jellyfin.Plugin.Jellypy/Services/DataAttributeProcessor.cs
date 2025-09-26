using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Jellypy.Configuration;
using Jellyfin.Plugin.Jellypy.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Services;

/// <summary>
/// Service for processing data attributes and converting them to script arguments and environment variables.
/// </summary>
public class DataAttributeProcessor
{
    private readonly ILogger<DataAttributeProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAttributeProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DataAttributeProcessor(ILogger<DataAttributeProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes data attributes and returns script arguments and environment variables.
    /// </summary>
    /// <param name="dataAttributes">The data attributes to process.</param>
    /// <param name="eventData">The event data to extract values from.</param>
    /// <returns>A tuple containing arguments and environment variables.</returns>
    public (List<string> Arguments, Dictionary<string, string> EnvironmentVariables) ProcessDataAttributes(
        IEnumerable<DataAttribute> dataAttributes, EventData eventData)
    {
        var arguments = new List<string>();
        var environmentVariables = new Dictionary<string, string>();

        foreach (var attribute in dataAttributes)
        {
            try
            {
                var value = GetAttributeValue(attribute, eventData);

                if (string.IsNullOrEmpty(value) && attribute.Required)
                {
                    _logger.LogWarning("Required attribute {Name} is missing or empty", attribute.Name);
                    continue;
                }

                if (string.IsNullOrEmpty(value))
                {
                    value = attribute.DefaultValue;
                }

                ProcessAttribute(attribute, value, arguments, environmentVariables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data attribute {Name}", attribute.Name);
            }
        }

        return (arguments, environmentVariables);
    }

    private void ProcessAttribute(DataAttribute attribute, string value, List<string> arguments, Dictionary<string, string> environmentVariables)
    {
        switch (attribute.Format)
        {
            case DataAttributeFormat.Argument:
                arguments.Add($"--{attribute.Name}");
                arguments.Add(value);
                break;

            case DataAttributeFormat.Environment:
                var envVarName = attribute.Name.ToUpperInvariant().Replace('-', '_');
                environmentVariables[envVarName] = value;
                break;

            case DataAttributeFormat.Json:
                var jsonValue = FormatAsJson(attribute, value);
                arguments.Add($"--{attribute.Name}");
                arguments.Add(jsonValue);
                break;

            case DataAttributeFormat.String:
            default:
                arguments.Add($"--{attribute.Name}");
                arguments.Add(value);
                break;
        }
    }

    private string GetAttributeValue(DataAttribute attribute, EventData eventData)
    {
        var sourceField = attribute.SourceField.ToLowerInvariant();

        return sourceField switch
        {
            "eventtype" => eventData.EventType.ToString(),
            "timestamp" => eventData.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            "userid" => eventData.UserId?.ToString() ?? string.Empty,
            "username" => eventData.UserName ?? string.Empty,
            "sessionid" => eventData.SessionId ?? string.Empty,
            "itemid" => eventData.ItemId?.ToString() ?? string.Empty,
            "itemname" => eventData.ItemName ?? string.Empty,
            "itemtype" => eventData.ItemType ?? string.Empty,
            "itempath" => eventData.ItemPath ?? string.Empty,
            "libraryid" => eventData.LibraryId?.ToString() ?? string.Empty,
            "libraryname" => eventData.LibraryName ?? string.Empty,
            "playbackpositionticks" => eventData.PlaybackPositionTicks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "ispaused" => eventData.IsPaused?.ToString() ?? string.Empty,
            "clientname" => eventData.ClientName ?? string.Empty,
            "devicename" => eventData.DeviceName ?? string.Empty,
            "deviceid" => eventData.DeviceId ?? string.Empty,
            "seriesname" => eventData.SeriesName ?? string.Empty,
            "seasonnumber" => eventData.SeasonNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "episodenumber" => eventData.EpisodeNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "year" => eventData.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "genres" => string.Join(",", eventData.Genres),
            "contentrating" => eventData.ContentRating ?? string.Empty,
            "additionaldata" => JsonSerializer.Serialize(eventData.AdditionalData),
            _ => GetAdditionalDataValue(attribute.SourceField, eventData)
        };
    }

    private string GetAdditionalDataValue(string field, EventData eventData)
    {
        if (eventData.AdditionalData.TryGetValue(field, out var value))
        {
            return value?.ToString() ?? string.Empty;
        }

        // Try with different casing
        var key = eventData.AdditionalData.Keys.FirstOrDefault(k =>
            string.Equals(k, field, StringComparison.OrdinalIgnoreCase));

        return key != null ? eventData.AdditionalData[key]?.ToString() ?? string.Empty : string.Empty;
    }

    private string FormatAsJson(DataAttribute attribute, string value)
    {
        try
        {
            // Try to parse as JSON first to validate
            if (value.StartsWith('{') || value.StartsWith('['))
            {
                var parsed = JsonSerializer.Deserialize<object>(value);
                return JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = false });
            }
            else
            {
                // Wrap simple values in JSON
                return JsonSerializer.Serialize(value);
            }
        }
        catch (JsonException)
        {
            // If not valid JSON, wrap as string
            return JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Creates default data attributes for common use cases.
    /// </summary>
    /// <param name="eventType">The event type to create attributes for.</param>
    /// <returns>A list of default data attributes.</returns>
    public static List<DataAttribute> CreateDefaultDataAttributes(EventType eventType)
    {
        var attributes = new List<DataAttribute>
        {
            new() { Name = "event-type", SourceField = "EventType", Format = DataAttributeFormat.Argument },
            new() { Name = "timestamp", SourceField = "Timestamp", Format = DataAttributeFormat.Environment },
            new() { Name = "user-name", SourceField = "UserName", Format = DataAttributeFormat.Environment },
            new() { Name = "item-name", SourceField = "ItemName", Format = DataAttributeFormat.Argument },
            new() { Name = "item-type", SourceField = "ItemType", Format = DataAttributeFormat.Environment }
        };

        // Add event-specific attributes
        switch (eventType)
        {
            case EventType.PlaybackStart:
            case EventType.PlaybackStop:
                attributes.AddRange(new[]
                {
                    new DataAttribute { Name = "client-name", SourceField = "ClientName", Format = DataAttributeFormat.Environment },
                    new DataAttribute { Name = "device-name", SourceField = "DeviceName", Format = DataAttributeFormat.Environment },
                    new DataAttribute { Name = "playback-position", SourceField = "PlaybackPositionTicks", Format = DataAttributeFormat.Argument }
                });
                break;

            case EventType.ItemAdded:
            case EventType.ItemUpdated:
                attributes.AddRange(new[]
                {
                    new DataAttribute { Name = "item-path", SourceField = "ItemPath", Format = DataAttributeFormat.Argument },
                    new DataAttribute { Name = "library-name", SourceField = "LibraryName", Format = DataAttributeFormat.Environment }
                });
                break;
        }

        // Add media-specific attributes
        attributes.AddRange(new[]
        {
            new DataAttribute { Name = "series-name", SourceField = "SeriesName", Format = DataAttributeFormat.Argument },
            new DataAttribute { Name = "season-number", SourceField = "SeasonNumber", Format = DataAttributeFormat.Argument },
            new DataAttribute { Name = "episode-number", SourceField = "EpisodeNumber", Format = DataAttributeFormat.Argument },
            new DataAttribute { Name = "year", SourceField = "Year", Format = DataAttributeFormat.Argument },
            new DataAttribute { Name = "genres", SourceField = "Genres", Format = DataAttributeFormat.Environment }
        });

        return attributes;
    }
}
