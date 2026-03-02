using System.Text.Json.Serialization;

namespace MDB.Launcher.Models;

/// <summary>
/// Represents a game profile with paths to the game EXE, MDB Bridge DLL, and mods directory.
/// </summary>
public class GameProfile
{
    /// <summary>Unique identifier for this profile.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>User-friendly display name (defaults to game EXE name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the game executable.</summary>
    public string GameExePath { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path to MDB_Bridge.dll. Auto-detected beside the game EXE,
    /// or manually overridden by the user.
    /// </summary>
    public string BridgeDllPath { get; set; } = string.Empty;

    /// <summary>Whether the bridge path was manually overridden (not auto-detected).</summary>
    public bool BridgePathOverridden { get; set; }

    /// <summary>Launch arguments to pass to the game EXE.</summary>
    public string LaunchArguments { get; set; } = string.Empty;

    /// <summary>Date this profile was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date this profile was last used for injection/launch.</summary>
    public DateTime? LastUsedAt { get; set; }

    // ── Derived Paths ──

    /// <summary>Directory containing the game EXE.</summary>
    [JsonIgnore]
    public string GameDirectory => Path.GetDirectoryName(GameExePath) ?? string.Empty;

    /// <summary>Path to the MDB mods directory: &lt;GameDir&gt;/MDB/Mods/</summary>
    [JsonIgnore]
    public string ModsDirectory => Path.Combine(GameDirectory, "MDB", "Mods");

    /// <summary>Path to the MDB managed directory: &lt;GameDir&gt;/MDB/Managed/</summary>
    [JsonIgnore]
    public string ManagedDirectory => Path.Combine(GameDirectory, "MDB", "Managed");

    /// <summary>Path to MDB log directory: &lt;GameDir&gt;/MDB/Logs/</summary>
    [JsonIgnore]
    public string LogsDirectory => Path.Combine(GameDirectory, "MDB", "Logs");

    /// <summary>Path to GameAssembly.dll (indicates IL2CPP game).</summary>
    [JsonIgnore]
    public string GameAssemblyPath => Path.Combine(GameDirectory, "GameAssembly.dll");

    // ── Validation ──

    /// <summary>Whether the game EXE exists on disk.</summary>
    [JsonIgnore]
    public bool GameExeExists => File.Exists(GameExePath);

    /// <summary>Whether GameAssembly.dll exists (confirms IL2CPP game).</summary>
    [JsonIgnore]
    public bool IsIl2CppGame => File.Exists(GameAssemblyPath);

    /// <summary>Whether MDB_Bridge.dll exists at the configured path.</summary>
    [JsonIgnore]
    public bool BridgeExists => File.Exists(BridgeDllPath);

    /// <summary>Whether the mods directory exists.</summary>
    [JsonIgnore]
    public bool ModsDirectoryExists => Directory.Exists(ModsDirectory);
}
