namespace Jellyfin.Plugin.JellyPy.Configuration;

/// <summary>
/// API and configuration constants.
/// </summary>
internal static class ApiConstants
{
    /// <summary>
    /// Maximum number of directories to return when browsing.
    /// </summary>
    public const int MaxDirectoryListingResults = 100;

    /// <summary>
    /// HTTP request timeout in seconds for external API calls.
    /// </summary>
    public const int HttpRequestTimeoutSeconds = 30;
}

/// <summary>
/// File size formatting constants.
/// </summary>
internal static class FileSizeConstants
{
    /// <summary>
    /// Number of bytes in a kilobyte.
    /// </summary>
    public const int BytesPerKilobyte = 1024;

    /// <summary>
    /// File size unit names.
    /// </summary>
    public static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];
}

/// <summary>
/// Logging and debugging constants.
/// </summary>
internal static class LoggingConstants
{
    /// <summary>
    /// Maximum number of PATH directories to log for debugging.
    /// </summary>
    public const int MaxPathDirectoriesToLog = 10;

    /// <summary>
    /// Maximum number of error lines to include in log output.
    /// </summary>
    public const int MaxErrorLinesToLog = 5;
}
