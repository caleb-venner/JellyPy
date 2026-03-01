using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // Private backing fields for encrypted API keys
    private string _sonarrApiKeyEncrypted = string.Empty;
    private string _radarrApiKeyEncrypted = string.Empty;

    // Private backing fields for encrypted ntfy credentials
    private string _ntfyAccessTokenEncrypted = string.Empty;
    private string _ntfyPasswordEncrypted = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ScriptSettings = new Collection<ScriptSetting>();
        GlobalSettings = new GlobalScriptSettings();

        // Native integration settings - enabled by default
        EnableNativeSonarrIntegration = true;
        EnableNativeRadarrIntegration = true;
        EpisodeDownloadBuffer = 6;
        AutoSearchEpisodes = true;
        MonitorFutureEpisodes = true;
        UnmonitorWatchedMovies = true;
        SkipEpisodesWithFiles = true;
        UnmonitorWatchedEpisodes = true;
        MonitorOnlyCurrentSeason = false;
        UnmonitorOnlyIfWatched = false;
        MinimumWatchPercentage = 90;
        UnmonitorAfterUpgrade = false;

        // Item grouping settings
        EnableItemGrouping = true;
        ItemGroupingDelaySeconds = 2;

        // ntfy notification settings
        EnableNtfyNotifications = false;
        NtfyUrl = "https://ntfy.sh";
        NtfyTopic = string.Empty;
        NtfyDefaultPriority = 3;
        NtfyIncludeMediaImage = true;
        NtfyNotificationDelaySeconds = 30;
        NtfyNotifyOnEpisodes = true;
        NtfyNotifyOnMovies = true;
        NtfyNotifyOnSeasons = false;
        NtfyNotifyOnSeries = false;
    }

    /// <summary>
    /// Gets or sets the directory where user scripts are stored.
    /// This resolves to {AppContext.BaseDirectory}/scripts by default.
    ///
    /// On different platforms:
    /// • Docker: /app/scripts
    /// • Windows: {Jellyfin Install}/scripts
    /// • macOS/Linux: {Jellyfin Install}/scripts.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public static string ScriptsDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted Sonarr API key used when invoking the script.
    /// </summary>
    public string SonarrApiKeyEncrypted
    {
        get => _sonarrApiKeyEncrypted;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _sonarrApiKeyEncrypted = string.Empty;
                return;
            }

            // If EncryptionHelper not initialized yet (during deserialization), store as-is
            if (!EncryptionHelper.IsInitialized())
            {
                _sonarrApiKeyEncrypted = value;
                return;
            }

            // If the value appears to be plaintext, encrypt it
            if (!IsLikelyEncrypted(value))
            {
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                _sonarrApiKeyEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
            }
            else
            {
                _sonarrApiKeyEncrypted = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Sonarr base URL used when invoking the script.
    /// </summary>
    public string SonarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted Radarr API key used when invoking the script.
    /// </summary>
    public string RadarrApiKeyEncrypted
    {
        get => _radarrApiKeyEncrypted;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _radarrApiKeyEncrypted = string.Empty;
                return;
            }

            // If EncryptionHelper not initialized yet (during deserialization), store as-is
            if (!EncryptionHelper.IsInitialized())
            {
                _radarrApiKeyEncrypted = value;
                return;
            }

            // If the value appears to be plaintext, encrypt it
            if (!IsLikelyEncrypted(value))
            {
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                _radarrApiKeyEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
            }
            else
            {
                _radarrApiKeyEncrypted = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the Sonarr API key using automatic encryption on set and decryption on get.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public string SonarrApiKey
    {
        get
        {
            if (!EncryptionHelper.IsInitialized())
            {
                return string.Empty;
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            return string.IsNullOrEmpty(SonarrApiKeyEncrypted)
                ? string.Empty
                : EncryptionHelper.Decrypt(SonarrApiKeyEncrypted, encryptionKey);
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                SonarrApiKeyEncrypted = string.Empty;
                return;
            }

            if (!EncryptionHelper.IsInitialized())
            {
                return; // Cannot encrypt without initialization
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            SonarrApiKeyEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
        }
    }

    /// <summary>
    /// Gets or sets the Radarr API key using automatic encryption on set and decryption on get.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public string RadarrApiKey
    {
        get
        {
            if (!EncryptionHelper.IsInitialized())
            {
                return string.Empty;
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            return string.IsNullOrEmpty(RadarrApiKeyEncrypted)
                ? string.Empty
                : EncryptionHelper.Decrypt(RadarrApiKeyEncrypted, encryptionKey);
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                RadarrApiKeyEncrypted = string.Empty;
                return;
            }

            if (!EncryptionHelper.IsInitialized())
            {
                return; // Cannot encrypt without initialization
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            RadarrApiKeyEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
        }
    }

    /// <summary>
    /// Gets or sets the Radarr base URL used when invoking the script.
    /// </summary>
    public string RadarrUrl { get; set; } = string.Empty;

    // Native Sonarr/Radarr Integration Settings

    /// <summary>
    /// Gets or sets a value indicating whether native Sonarr integration is enabled.
    /// When enabled, episodes will be automatically downloaded without requiring Python scripts.
    /// </summary>
    public bool EnableNativeSonarrIntegration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether native Radarr integration is enabled.
    /// When enabled, movies will be automatically unmonitored after watching without requiring Python scripts.
    /// </summary>
    public bool EnableNativeRadarrIntegration { get; set; }

    /// <summary>
    /// Gets or sets the number of next episodes to monitor and download when watching a TV show.
    /// This matches the episode buffer used by the previous download script implementation.
    /// Default is 6 episodes.
    /// </summary>
    public int EpisodeDownloadBuffer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically search for next episodes.
    /// When true, triggers Sonarr to search for the next episodes immediately.
    /// </summary>
    public bool AutoSearchEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to monitor future episodes automatically.
    /// When true and remaining episodes are less than the buffer, all future episodes will be monitored.
    /// </summary>
    public bool MonitorFutureEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to unmonitor movies after they are watched.
    /// When true, sets movies to unmonitored in Radarr when playback starts.
    /// </summary>
    public bool UnmonitorWatchedMovies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip monitoring episodes that already have files.
    /// When true, only monitors episodes that are missing files (default behavior).
    /// When false, monitors all next episodes regardless of file existence (useful for upgrades).
    /// </summary>
    public bool SkipEpisodesWithFiles { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to unmonitor episodes after watching them.
    /// When false, episodes remain monitored after watching (useful for quality upgrades).
    /// Default is true.
    /// </summary>
    public bool UnmonitorWatchedEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only monitor episodes in the current season being watched.
    /// When true, only monitors episodes in the same season as the episode you just watched.
    /// Episodes in future seasons will not be monitored until you start watching that season.
    /// Default is false (monitors episodes across all future seasons).
    /// </summary>
    public bool MonitorOnlyCurrentSeason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only unmonitor movies after they've been watched.
    /// When true, checks playback progress and only unmonitors if watch percentage exceeds threshold.
    /// When false, unmonitors immediately on playback start.
    /// Default is false.
    /// </summary>
    public bool UnmonitorOnlyIfWatched { get; set; }

    /// <summary>
    /// Gets or sets the minimum watch percentage required to unmonitor a movie.
    /// Only used when UnmonitorOnlyIfWatched is true.
    /// Default is 90 percent.
    /// </summary>
    public int MinimumWatchPercentage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only unmonitor movies after they reach quality cutoff.
    /// When true, keeps movies monitored until they meet the quality profile cutoff.
    /// Default is false.
    /// </summary>
    public bool UnmonitorAfterUpgrade { get; set; }

    // New enhanced configuration properties

    /// <summary>
    /// Gets the list of script settings for enhanced event handling.
    /// </summary>
    public Collection<ScriptSetting> ScriptSettings { get; }

    /// <summary>
    /// Gets or sets global settings for script execution.
    /// </summary>
    public GlobalScriptSettings GlobalSettings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable item grouping.
    /// When enabled, items added in quick succession will be grouped into a single event.
    /// </summary>
    public bool EnableItemGrouping { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds to wait for more items before processing a group.
    /// </summary>
    public int ItemGroupingDelaySeconds { get; set; }

    // ntfy Notification Settings

    /// <summary>
    /// Gets or sets a value indicating whether ntfy notifications are enabled.
    /// </summary>
    public bool EnableNtfyNotifications { get; set; }

    /// <summary>
    /// Gets or sets the ntfy server URL.
    /// Default is https://ntfy.sh for the public server.
    /// </summary>
    public string NtfyUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ntfy topic to publish notifications to.
    /// </summary>
    public string NtfyTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted ntfy access token for authentication.
    /// </summary>
    public string NtfyAccessTokenEncrypted
    {
        get => _ntfyAccessTokenEncrypted;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _ntfyAccessTokenEncrypted = string.Empty;
                return;
            }

            // If EncryptionHelper not initialized yet (during deserialization), store as-is
            if (!EncryptionHelper.IsInitialized())
            {
                _ntfyAccessTokenEncrypted = value;
                return;
            }

            // If the value appears to be plaintext, encrypt it
            if (!IsLikelyEncrypted(value))
            {
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                _ntfyAccessTokenEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
            }
            else
            {
                _ntfyAccessTokenEncrypted = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ntfy access token using automatic encryption on set and decryption on get.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public string NtfyAccessToken
    {
        get
        {
            if (!EncryptionHelper.IsInitialized())
            {
                return string.Empty;
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            return string.IsNullOrEmpty(NtfyAccessTokenEncrypted)
                ? string.Empty
                : EncryptionHelper.Decrypt(NtfyAccessTokenEncrypted, encryptionKey);
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                NtfyAccessTokenEncrypted = string.Empty;
                return;
            }

            if (!EncryptionHelper.IsInitialized())
            {
                return; // Cannot encrypt without initialization
            }

            string encryptionKey = EncryptionHelper.GenerateMachineKey();
            NtfyAccessTokenEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
        }
    }

    /// <summary>
    /// Gets or sets the ntfy username for basic authentication.
    /// Used when access token is not provided.
    /// </summary>
    public string NtfyUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted ntfy password for basic authentication.
    /// </summary>
    public string NtfyPasswordEncrypted
    {
        get => _ntfyPasswordEncrypted;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                System.Diagnostics.Debug.WriteLine("NtfyPasswordEncrypted setter: Setting to empty");
                _ntfyPasswordEncrypted = string.Empty;
                return;
            }

            // If EncryptionHelper not initialized yet (during deserialization), store as-is
            if (!EncryptionHelper.IsInitialized())
            {
                System.Diagnostics.Debug.WriteLine($"NtfyPasswordEncrypted setter: EncryptionHelper not initialized, storing as-is (length: {value.Length})");
                _ntfyPasswordEncrypted = value;
                return;
            }

            // If the value appears to be plaintext, encrypt it
            bool isEncrypted = IsLikelyEncrypted(value);
            System.Diagnostics.Debug.WriteLine($"NtfyPasswordEncrypted setter: IsLikelyEncrypted={isEncrypted}, value length={value.Length}");

            if (!isEncrypted)
            {
                System.Diagnostics.Debug.WriteLine("NtfyPasswordEncrypted setter: Value appears to be plaintext, encrypting it");
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                _ntfyPasswordEncrypted = EncryptionHelper.Encrypt(value, encryptionKey);
                System.Diagnostics.Debug.WriteLine($"NtfyPasswordEncrypted setter: Encrypted result length={_ntfyPasswordEncrypted?.Length ?? 0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("NtfyPasswordEncrypted setter: Value appears to be encrypted, storing as-is");
                _ntfyPasswordEncrypted = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the ntfy password using automatic encryption on set and decryption on get.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public string NtfyPassword
    {
        get
        {
            if (!EncryptionHelper.IsInitialized())
            {
                System.Diagnostics.Debug.WriteLine("NtfyPassword getter: EncryptionHelper not initialized");
                return string.Empty;
            }

            if (string.IsNullOrEmpty(NtfyPasswordEncrypted))
            {
                System.Diagnostics.Debug.WriteLine("NtfyPassword getter: NtfyPasswordEncrypted is empty");
                return string.Empty;
            }

            try
            {
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                System.Diagnostics.Debug.WriteLine($"NtfyPassword getter: Attempting to decrypt (encrypted length: {NtfyPasswordEncrypted.Length})");
                var decrypted = EncryptionHelper.Decrypt(NtfyPasswordEncrypted, encryptionKey);
                System.Diagnostics.Debug.WriteLine($"NtfyPassword getter: Decryption result length: {decrypted?.Length ?? 0}");
                return decrypted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NtfyPassword getter: Exception during decryption: {ex.Message}");
                return string.Empty;
            }
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                System.Diagnostics.Debug.WriteLine("NtfyPassword setter: Setting to empty");
                NtfyPasswordEncrypted = string.Empty;
                return;
            }

            if (!EncryptionHelper.IsInitialized())
            {
                System.Diagnostics.Debug.WriteLine("NtfyPassword setter: EncryptionHelper not initialized, cannot encrypt");
                return; // Cannot encrypt without initialization
            }

            try
            {
                string encryptionKey = EncryptionHelper.GenerateMachineKey();
                System.Diagnostics.Debug.WriteLine($"NtfyPassword setter: Attempting to encrypt (plaintext length: {value.Length})");
                var encrypted = EncryptionHelper.Encrypt(value, encryptionKey);
                System.Diagnostics.Debug.WriteLine($"NtfyPassword setter: Encryption result length: {encrypted?.Length ?? 0}");
                NtfyPasswordEncrypted = encrypted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NtfyPassword setter: Exception during encryption: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets or sets the default notification priority (1-5).
    /// 1=min, 2=low, 3=default, 4=high, 5=max.
    /// </summary>
    public int NtfyDefaultPriority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include media images in notifications.
    /// Requires JellyfinExternalUrl to be configured.
    /// </summary>
    public bool NtfyIncludeMediaImage { get; set; }

    /// <summary>
    /// Gets or sets the external URL for Jellyfin server.
    /// Used for media image URLs in ntfy notifications.
    /// Example: https://jellyfin.example.com.
    /// </summary>
    public string JellyfinExternalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delay in seconds to wait before sending ntfy notifications.
    /// This allows batching multiple episodes from the same series into a single notification.
    /// Default is 30 seconds.
    /// </summary>
    public int NtfyNotificationDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications for new episodes.
    /// </summary>
    public bool NtfyNotifyOnEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications for new movies.
    /// </summary>
    public bool NtfyNotifyOnMovies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications for new seasons.
    /// </summary>
    public bool NtfyNotifyOnSeasons { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to send notifications for new series.
    /// </summary>
    public bool NtfyNotifyOnSeries { get; set; }

    /// <summary>
    /// Ensures API keys are properly encrypted before saving.
    /// This method should be called before serialization to handle any plaintext API keys.
    /// </summary>
    public void EnsureApiKeysEncrypted()
    {
        string encryptionKey = EncryptionHelper.GenerateMachineKey();

        // Check if SonarrApiKeyEncrypted contains plaintext that should be encrypted
        if (!string.IsNullOrEmpty(SonarrApiKeyEncrypted) && !IsLikelyEncrypted(SonarrApiKeyEncrypted))
        {
            // Appears to be plaintext - encrypt it
            string plaintext = SonarrApiKeyEncrypted;
            SonarrApiKeyEncrypted = EncryptionHelper.Encrypt(plaintext, encryptionKey);
        }

        // Check if RadarrApiKeyEncrypted contains plaintext that should be encrypted
        if (!string.IsNullOrEmpty(RadarrApiKeyEncrypted) && !IsLikelyEncrypted(RadarrApiKeyEncrypted))
        {
            // Appears to be plaintext - encrypt it
            string plaintext = RadarrApiKeyEncrypted;
            RadarrApiKeyEncrypted = EncryptionHelper.Encrypt(plaintext, encryptionKey);
        }

        // Check if NtfyAccessTokenEncrypted contains plaintext that should be encrypted
        if (!string.IsNullOrEmpty(NtfyAccessTokenEncrypted) && !IsLikelyEncrypted(NtfyAccessTokenEncrypted))
        {
            // Appears to be plaintext - encrypt it
            string plaintext = NtfyAccessTokenEncrypted;
            NtfyAccessTokenEncrypted = EncryptionHelper.Encrypt(plaintext, encryptionKey);
        }

        // Check if NtfyPasswordEncrypted contains plaintext that should be encrypted
        if (!string.IsNullOrEmpty(NtfyPasswordEncrypted) && !IsLikelyEncrypted(NtfyPasswordEncrypted))
        {
            // Appears to be plaintext - encrypt it
            string plaintext = NtfyPasswordEncrypted;
            NtfyPasswordEncrypted = EncryptionHelper.Encrypt(plaintext, encryptionKey);
        }
    }

    /// <summary>
    /// Checks if a string appears to be encrypted (base64 encoded with appropriate length).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value appears to be encrypted, false otherwise.</returns>
    private static bool IsLikelyEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        // Encrypted values should be base64 strings with reasonable length
        // Minimum encrypted size: 16-byte IV + 16-byte cipher block = 32 bytes = ~43 base64 chars
        // Use 40 as threshold to account for very short passwords
        if (value.Length < 40)
        {
            return false; // Too short to be encrypted
        }

        // Check if it looks like base64
        try
        {
            byte[] data = Convert.FromBase64String(value);
            return data.Length >= 32; // Minimum: 16-byte IV + 16-byte ciphertext
        }
        catch (FormatException)
        {
            return false; // Not valid base64, likely plaintext
        }
    }
}
