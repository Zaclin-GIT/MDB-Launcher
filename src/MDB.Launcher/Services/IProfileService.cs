using MDB.Launcher.Models;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for managing game profiles and launcher configuration.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Load the launcher configuration from disk.
    /// </summary>
    Task<LauncherConfig> LoadConfigAsync();

    /// <summary>
    /// Save the launcher configuration to disk.
    /// </summary>
    Task SaveConfigAsync(LauncherConfig config);

    /// <summary>
    /// Get the configuration file path.
    /// </summary>
    string ConfigFilePath { get; }

    /// <summary>
    /// Auto-detect the MDB_Bridge.dll path for a given game EXE.
    /// Returns the path if found beside the EXE, or null.
    /// </summary>
    string? DetectBridgeDll(string gameExePath);

    /// <summary>
    /// Check if a game EXE is an IL2CPP game (has GameAssembly.dll beside it).
    /// </summary>
    bool IsIl2CppGame(string gameExePath);

    /// <summary>
    /// Create a new profile from a game EXE path with auto-detection.
    /// </summary>
    GameProfile CreateProfile(string gameExePath);
}
