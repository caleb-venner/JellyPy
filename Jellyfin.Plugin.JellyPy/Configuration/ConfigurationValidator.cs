using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Validates plugin configuration settings.
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Validates Sonarr configuration settings.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>Validation result with any errors.</returns>
    public static ValidationResult ValidateSonarrConfiguration(PluginConfiguration config)
    {
        var result = new ValidationResult();

        if (!config.EnableNativeSonarrIntegration)
        {
            return result; // Not enabled, no validation needed
        }

        if (string.IsNullOrWhiteSpace(config.SonarrUrl))
        {
            result.AddError("Sonarr URL is required when native integration is enabled");
        }
        else if (!Uri.TryCreate(config.SonarrUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            result.AddError("Sonarr URL must be a valid HTTP or HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(config.SonarrApiKey))
        {
            result.AddError("Sonarr API key is required when native integration is enabled");
        }

        if (config.EpisodeDownloadBuffer < 1)
        {
            result.AddError("Episode download buffer must be at least 1");
        }

        if (config.EpisodeDownloadBuffer > 100)
        {
            result.AddWarning("Episode download buffer is unusually high (>100)");
        }

        return result;
    }

    /// <summary>
    /// Validates Radarr configuration settings.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>Validation result with any errors.</returns>
    public static ValidationResult ValidateRadarrConfiguration(PluginConfiguration config)
    {
        var result = new ValidationResult();

        if (!config.EnableNativeRadarrIntegration)
        {
            return result; // Not enabled, no validation needed
        }

        if (string.IsNullOrWhiteSpace(config.RadarrUrl))
        {
            result.AddError("Radarr URL is required when native integration is enabled");
        }
        else if (!Uri.TryCreate(config.RadarrUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            result.AddError("Radarr URL must be a valid HTTP or HTTPS URL");
        }

        if (string.IsNullOrWhiteSpace(config.RadarrApiKey))
        {
            result.AddError("Radarr API key is required when native integration is enabled");
        }

        if (config.UnmonitorOnlyIfWatched && config.MinimumWatchPercentage < 0)
        {
            result.AddError("Minimum watch percentage cannot be negative");
        }

        if (config.UnmonitorOnlyIfWatched && config.MinimumWatchPercentage > 100)
        {
            result.AddError("Minimum watch percentage cannot exceed 100");
        }

        return result;
    }

    /// <summary>
    /// Validates all configuration settings.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>Validation result with any errors.</returns>
    public static ValidationResult ValidateConfiguration(PluginConfiguration config)
    {
        var result = new ValidationResult();

        // Validate Sonarr settings
        var sonarrResult = ValidateSonarrConfiguration(config);
        result.Merge(sonarrResult);

        // Validate Radarr settings
        var radarrResult = ValidateRadarrConfiguration(config);
        result.Merge(radarrResult);

        return result;
    }
}

/// <summary>
/// Represents the result of configuration validation.
/// </summary>
public class ValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _errors.Add(error);
        }
    }

    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            _warnings.Add(warning);
        }
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The other validation result.</param>
    public void Merge(ValidationResult other)
    {
        if (other == null)
        {
            return;
        }

        _errors.AddRange(other.Errors);
        _warnings.AddRange(other.Warnings);
    }

    /// <summary>
    /// Gets a formatted error message combining all errors and warnings.
    /// </summary>
    /// <returns>Formatted error message.</returns>
    public string GetFormattedMessage()
    {
        var messages = new List<string>();

        if (_errors.Count > 0)
        {
            messages.Add($"Errors: {string.Join("; ", _errors)}");
        }

        if (_warnings.Count > 0)
        {
            messages.Add($"Warnings: {string.Join("; ", _warnings)}");
        }

        return messages.Count > 0 ? string.Join(" | ", messages) : "Configuration is valid";
    }
}
