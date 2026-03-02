namespace MDB.Launcher.Models;

/// <summary>
/// Top-level launcher configuration persisted to JSON.
/// </summary>
public class LauncherConfig
{
    /// <summary>All saved game profiles.</summary>
    public List<GameProfile> Profiles { get; set; } = [];

    /// <summary>ID of the last active profile.</summary>
    public string? ActiveProfileId { get; set; }

    /// <summary>Injection delay in milliseconds (poll interval for GameAssembly.dll).</summary>
    public int InjectionPollIntervalMs { get; set; } = 500;

    /// <summary>Maximum time to wait for GameAssembly.dll before timing out (seconds).</summary>
    public int InjectionTimeoutSeconds { get; set; } = 60;

    /// <summary>Whether to automatically close the launcher after successful injection.</summary>
    public bool CloseAfterInjection { get; set; } = false;

    /// <summary>GitHub personal access token (optional, for higher API rate limits).</summary>
    public string? GitHubToken { get; set; }
}
