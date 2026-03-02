using System.Reflection;
using MDB.Launcher.Models;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for discovering mods by scanning DLLs in the MDB/Mods/ directory
/// and reading [Mod] attribute metadata via MetadataLoadContext.
/// </summary>
public class ModDiscoveryService : IModDiscoveryService
{
    public Task<List<ModEntry>> DiscoverModsAsync(string modsDirectory, string? managedDirectory = null)
    {
        return Task.Run(() =>
        {
            var mods = new List<ModEntry>();

            if (!Directory.Exists(modsDirectory))
                return mods;

            // Scan for both enabled (.dll) and disabled (.dll.disabled) mods
            var dllFiles = Directory.GetFiles(modsDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            var disabledFiles = Directory.GetFiles(modsDirectory, "*.dll.disabled", SearchOption.TopDirectoryOnly);

            foreach (var file in dllFiles)
                mods.Add(ReadModEntry(file, isEnabled: true, managedDirectory));

            foreach (var file in disabledFiles)
                mods.Add(ReadModEntry(file, isEnabled: false, managedDirectory));

            return mods.OrderBy(m => m.Name).ToList();
        });
    }

    private ModEntry ReadModEntry(string filePath, bool isEnabled, string? managedDirectory)
    {
        var fileInfo = new FileInfo(filePath);
        var entry = new ModEntry
        {
            FilePath = filePath,
            IsEnabled = isEnabled,
            FileSize = fileInfo.Length,
            Name = Path.GetFileNameWithoutExtension(filePath)
                       .Replace(".dll", "", StringComparison.OrdinalIgnoreCase),
        };

        try
        {
            ReadModAttribute(entry, filePath, managedDirectory);
        }
        catch (Exception ex)
        {
            entry.HasMetadata = false;
            entry.MetadataError = ex.Message;
        }

        return entry;
    }

    private void ReadModAttribute(ModEntry entry, string filePath, string? managedDirectory)
    {
        // Build assembly resolver paths
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        var resolver = new PathAssemblyResolver(GetResolverPaths(runtimeDir, filePath, managedDirectory));

        using var mlc = new MetadataLoadContext(resolver, coreAssemblyName: "System.Runtime");

        try
        {
            var assembly = mlc.LoadFromAssemblyPath(filePath);

            foreach (var type in assembly.GetTypes())
            {
                // Look for [Mod] attribute (GameSDK.ModHost.ModAttribute)
                var attrData = type.CustomAttributes.FirstOrDefault(a =>
                    a.AttributeType.Name == "ModAttribute" ||
                    a.AttributeType.FullName == "GameSDK.ModHost.ModAttribute");

                if (attrData == null)
                    continue;

                entry.HasMetadata = true;

                // Read constructor arguments: (string id, string name, string version)
                var ctorArgs = attrData.ConstructorArguments;
                if (ctorArgs.Count >= 3)
                {
                    entry.Id = ctorArgs[0].Value?.ToString() ?? string.Empty;
                    entry.Name = ctorArgs[1].Value?.ToString() ?? entry.Name;
                    entry.Version = ctorArgs[2].Value?.ToString() ?? "1.0.0";
                }

                // Read named arguments: Author, Description
                foreach (var namedArg in attrData.NamedArguments)
                {
                    switch (namedArg.MemberName)
                    {
                        case "Author":
                            entry.Author = namedArg.TypedValue.Value?.ToString() ?? "Unknown";
                            break;
                        case "Description":
                            entry.Description = namedArg.TypedValue.Value?.ToString() ?? string.Empty;
                            break;
                    }
                }

                return; // Found the mod class, stop scanning
            }

            // No [Mod] attribute found — still a valid DLL, just no metadata
            entry.HasMetadata = false;
            entry.MetadataError = "No [Mod] attribute found in assembly.";
        }
        catch (Exception ex)
        {
            entry.HasMetadata = false;
            entry.MetadataError = $"Failed to read assembly: {ex.Message}";
        }
    }

    private IEnumerable<string> GetResolverPaths(string runtimeDir, string targetDll, string? managedDirectory)
    {
        // Core runtime assemblies
        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            yield return dll;

        // MDB Managed directory (contains GameSDK.ModHost.dll and other framework assemblies)
        if (!string.IsNullOrEmpty(managedDirectory) && Directory.Exists(managedDirectory))
        {
            foreach (var dll in Directory.GetFiles(managedDirectory, "*.dll"))
                yield return dll;
        }

        // The target mod DLL itself
        yield return targetDll;

        // Other DLLs in the same directory as the mod (dependencies)
        var modDir = Path.GetDirectoryName(targetDll);
        if (modDir != null)
        {
            foreach (var dll in Directory.GetFiles(modDir, "*.dll"))
            {
                if (!string.Equals(dll, targetDll, StringComparison.OrdinalIgnoreCase))
                    yield return dll;
            }
        }
    }

    public bool EnableMod(ModEntry mod)
    {
        try
        {
            if (!mod.FilePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                return true; // Already enabled

            var newPath = mod.FilePath[..^".disabled".Length];
            File.Move(mod.FilePath, newPath);
            mod.FilePath = newPath;
            mod.IsEnabled = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DisableMod(ModEntry mod)
    {
        try
        {
            if (mod.FilePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                return true; // Already disabled

            var newPath = mod.FilePath + ".disabled";
            File.Move(mod.FilePath, newPath);
            mod.FilePath = newPath;
            mod.IsEnabled = false;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
