namespace MDB.Launcher.Models;

/// <summary>
/// Represents a discovered mod DLL with its metadata extracted from the [Mod] attribute.
/// </summary>
public class ModEntry
{
    /// <summary>Mod ID in reverse-domain notation (e.g., "Author.ModName").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the mod.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Semantic version string (e.g., "1.0.0").</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Mod author name.</summary>
    public string Author { get; set; } = "Unknown";

    /// <summary>Mod description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Absolute path to the mod DLL file.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Just the filename (e.g., "MyMod.dll").</summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Whether the mod is currently enabled.
    /// Enabled = .dll extension, Disabled = .dll.disabled extension.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Formatted file size for display.</summary>
    public string FileSizeFormatted => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / (1024.0 * 1024.0):F1} MB"
    };

    /// <summary>Whether metadata was successfully read from the DLL.</summary>
    public bool HasMetadata { get; set; }

    /// <summary>Error message if metadata reading failed.</summary>
    public string? MetadataError { get; set; }
}
