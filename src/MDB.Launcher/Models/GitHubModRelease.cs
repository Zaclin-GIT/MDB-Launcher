namespace MDB.Launcher.Models;

/// <summary>
/// Represents a GitHub release asset (mod DLL) available for download.
/// </summary>
public class GitHubModRelease
{
    /// <summary>Repository owner.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Repository name.</summary>
    public string RepoName { get; set; } = string.Empty;

    /// <summary>Release tag name (e.g., "v1.0.0").</summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>Release title.</summary>
    public string ReleaseName { get; set; } = string.Empty;

    /// <summary>Release description / body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Release publish date.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Whether this is a pre-release.</summary>
    public bool IsPreRelease { get; set; }

    /// <summary>Available DLL assets in this release.</summary>
    public List<GitHubModAsset> Assets { get; set; } = [];
}

/// <summary>
/// Represents a downloadable asset from a GitHub release.
/// </summary>
public class GitHubModAsset
{
    /// <summary>Asset filename.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Download URL.</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Formatted file size for display.</summary>
    public string SizeFormatted => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        _ => $"{Size / (1024.0 * 1024.0):F1} MB"
    };

    /// <summary>Content type (MIME).</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Download count.</summary>
    public int DownloadCount { get; set; }
}
