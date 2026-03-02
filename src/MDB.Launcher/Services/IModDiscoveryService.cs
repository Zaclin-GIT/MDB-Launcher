using MDB.Launcher.Models;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for discovering and managing mods in a game's MDB/Mods/ directory.
/// </summary>
public interface IModDiscoveryService
{
    /// <summary>
    /// Scan the mods directory and extract metadata from all mod DLLs.
    /// </summary>
    /// <param name="modsDirectory">Path to the MDB/Mods/ directory.</param>
    /// <param name="managedDirectory">Path to the MDB/Managed/ directory containing GameSDK.ModHost.dll.</param>
    /// <returns>List of discovered mods with their metadata.</returns>
    Task<List<ModEntry>> DiscoverModsAsync(string modsDirectory, string? managedDirectory = null);

    /// <summary>
    /// Enable a mod by renaming .dll.disabled back to .dll.
    /// </summary>
    bool EnableMod(ModEntry mod);

    /// <summary>
    /// Disable a mod by renaming .dll to .dll.disabled.
    /// </summary>
    bool DisableMod(ModEntry mod);
}
