using System.Text.Json;
using MDB.Launcher.Models;

namespace MDB.Launcher.Services;

/// <summary>
/// Service for managing game profiles and launcher configuration,
/// persisted as JSON in %AppData%/MDB_Launcher/.
/// </summary>
public class ProfileService : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ConfigFilePath { get; }

    public ProfileService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MDB_Launcher");
        Directory.CreateDirectory(appDataDir);
        ConfigFilePath = Path.Combine(appDataDir, "config.json");
    }

    public async Task<LauncherConfig> LoadConfigAsync()
    {
        if (!File.Exists(ConfigFilePath))
            return new LauncherConfig();

        try
        {
            var json = await File.ReadAllTextAsync(ConfigFilePath);
            return JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions)
                   ?? new LauncherConfig();
        }
        catch
        {
            return new LauncherConfig();
        }
    }

    public async Task SaveConfigAsync(LauncherConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(ConfigFilePath, json);
    }

    public string? DetectBridgeDll(string gameExePath)
    {
        var gameDir = Path.GetDirectoryName(gameExePath);
        if (string.IsNullOrEmpty(gameDir))
            return null;

        var bridgePath = Path.Combine(gameDir, "MDB_Bridge.dll");
        return File.Exists(bridgePath) ? bridgePath : null;
    }

    public bool IsIl2CppGame(string gameExePath)
    {
        var gameDir = Path.GetDirectoryName(gameExePath);
        if (string.IsNullOrEmpty(gameDir))
            return false;

        return File.Exists(Path.Combine(gameDir, "GameAssembly.dll"));
    }

    public GameProfile CreateProfile(string gameExePath)
    {
        var gameName = Path.GetFileNameWithoutExtension(gameExePath);
        var bridgePath = DetectBridgeDll(gameExePath);

        return new GameProfile
        {
            Name = gameName,
            GameExePath = gameExePath,
            BridgeDllPath = bridgePath ?? string.Empty,
            BridgePathOverridden = false,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
