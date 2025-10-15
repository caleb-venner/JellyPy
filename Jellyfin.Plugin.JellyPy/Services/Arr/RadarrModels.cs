using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JellyPy.Services.Arr;

/// <summary>
/// Represents a Radarr movie.
/// </summary>
public class RadarrMovie
{
    /// <summary>
    /// Gets or sets the Radarr movie ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the movie is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the movie has a file.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }
}

/// <summary>
/// Request to update movies.
/// </summary>
public class UpdateMoviesRequest
{
    /// <summary>
    /// Gets or sets the movie IDs to update.
    /// </summary>
    [JsonPropertyName("movieIds")]
    public int[] MovieIds { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Gets or sets a value indicating whether movies should be monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }
}

/// <summary>
/// Represents detailed information about a Radarr movie including quality profile.
/// </summary>
public class RadarrMovieDetails
{
    /// <summary>
    /// Gets or sets the Radarr movie ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the movie is monitored.
    /// </summary>
    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID.
    /// </summary>
    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the movie has a file.
    /// </summary>
    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    /// <summary>
    /// Gets or sets the movie file information.
    /// </summary>
    [JsonPropertyName("movieFile")]
    public RadarrMovieFile? MovieFile { get; set; }

    /// <summary>
    /// Gets a value indicating whether the movie has reached its quality cutoff.
    /// This is determined by comparing the current file quality to the cutoff of the quality profile.
    /// </summary>
    public bool HasReachedQualityCutoff => MovieFile?.QualityCutoffNotMet == false;
}

/// <summary>
/// Represents a movie file in Radarr.
/// </summary>
public class RadarrMovieFile
{
    /// <summary>
    /// Gets or sets the file ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the quality information.
    /// </summary>
    [JsonPropertyName("quality")]
    public RadarrQuality? Quality { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the quality cutoff has not been met.
    /// When false, the movie has reached its quality cutoff and won't be upgraded.
    /// </summary>
    [JsonPropertyName("qualityCutoffNotMet")]
    public bool QualityCutoffNotMet { get; set; }
}

/// <summary>
/// Represents quality information for a movie file.
/// </summary>
public class RadarrQuality
{
    /// <summary>
    /// Gets or sets the quality definition.
    /// </summary>
    [JsonPropertyName("quality")]
    public RadarrQualityDefinition? QualityDefinition { get; set; }
}

/// <summary>
/// Represents a quality definition.
/// </summary>
public class RadarrQualityDefinition
{
    /// <summary>
    /// Gets or sets the quality ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the quality name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
