#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Jellypy.Configuration;
using Jellyfin.Plugin.Jellypy.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellypy.Services;

/// <summary>
/// Service for evaluating execution conditions against event data.
/// </summary>
public class ConditionEvaluator
{
    private readonly ILogger<ConditionEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionEvaluator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ConditionEvaluator(ILogger<ConditionEvaluator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluates whether all conditions are met for the given event data.
    /// </summary>
    /// <param name="conditions">The conditions to evaluate.</param>
    /// <param name="eventData">The event data to evaluate against.</param>
    /// <returns>True if all conditions are met, false otherwise.</returns>
    public bool EvaluateConditions(System.Collections.Generic.IEnumerable<ExecutionCondition> conditions, EventData eventData)
    {
        var conditionsList = conditions.ToList();
        if (conditionsList.Count == 0)
        {
            return true; // No conditions means always execute
        }

        foreach (var condition in conditionsList)
        {
            if (!EvaluateCondition(condition, eventData))
            {
                return false; // All conditions must be true
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates a single condition against event data.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="eventData">The event data to evaluate against.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    public bool EvaluateCondition(ExecutionCondition condition, EventData eventData)
    {
        try
        {
            var actualValue = GetFieldValue(condition.Field, eventData);
            return EvaluateValue(actualValue, condition.Value, condition.Operator, condition.CaseSensitive);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating condition {Field} {Operator} {Value}",
                condition.Field,
                condition.Operator,
                condition.Value);
            return false;
        }
    }

    private string GetFieldValue(string field, EventData eventData)
    {
        return field.ToLowerInvariant() switch
        {
            "eventtype" => eventData.EventType.ToString(),
            "userid" => eventData.UserId?.ToString() ?? string.Empty,
            "username" => eventData.UserName ?? string.Empty,
            "itemid" => eventData.ItemId?.ToString() ?? string.Empty,
            "itemname" => eventData.ItemName ?? string.Empty,
            "itemtype" => eventData.ItemType ?? string.Empty,
            "itempath" => eventData.ItemPath ?? string.Empty,
            "libraryid" => eventData.LibraryId?.ToString() ?? string.Empty,
            "libraryname" => eventData.LibraryName ?? string.Empty,
            "clientname" => eventData.ClientName ?? string.Empty,
            "devicename" => eventData.DeviceName ?? string.Empty,
            "deviceid" => eventData.DeviceId ?? string.Empty,
            "seriesname" => eventData.SeriesName ?? string.Empty,
            "seasonnumber" => eventData.SeasonNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "episodenumber" => eventData.EpisodeNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "year" => eventData.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "contentrating" => eventData.ContentRating ?? string.Empty,
            "ispaused" => eventData.IsPaused?.ToString() ?? string.Empty,
            "genres" => string.Join(",", eventData.Genres),
            _ => GetAdditionalDataValue(field, eventData)
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

    private bool EvaluateValue(string actualValue, string expectedValue, ConditionOperator operatorType, bool caseSensitive)
    {
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        return operatorType switch
        {
            ConditionOperator.Equals => string.Equals(actualValue, expectedValue, comparison),
            ConditionOperator.NotEquals => !string.Equals(actualValue, expectedValue, comparison),
            ConditionOperator.Contains => actualValue.Contains(expectedValue, comparison),
            ConditionOperator.NotContains => !actualValue.Contains(expectedValue, comparison),
            ConditionOperator.StartsWith => actualValue.StartsWith(expectedValue, comparison),
            ConditionOperator.EndsWith => actualValue.EndsWith(expectedValue, comparison),
            ConditionOperator.Regex => EvaluateRegex(actualValue, expectedValue),
            ConditionOperator.GreaterThan => EvaluateNumeric(actualValue, expectedValue, (a, b) => a > b),
            ConditionOperator.LessThan => EvaluateNumeric(actualValue, expectedValue, (a, b) => a < b),
            ConditionOperator.In => EvaluateInList(actualValue, expectedValue, comparison),
            ConditionOperator.NotIn => !EvaluateInList(actualValue, expectedValue, comparison),
            _ => false
        };
    }

    private bool EvaluateRegex(string actualValue, string pattern)
    {
        try
        {
            return Regex.IsMatch(actualValue, pattern);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            return false;
        }
    }

    private bool EvaluateNumeric(string actualValue, string expectedValue, Func<decimal, decimal, bool> comparison)
    {
        if (decimal.TryParse(actualValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var actual) &&
            decimal.TryParse(expectedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var expected))
        {
            return comparison(actual, expected);
        }

        return false;
    }

    private bool EvaluateInList(string actualValue, string listValue, StringComparison comparison)
    {
        var items = listValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim());

        return items.Any(item => string.Equals(actualValue, item, comparison));
    }
}
