namespace MDB.Launcher.Services;

/// <summary>
/// Result of an injection operation.
/// </summary>
public record InjectionResult(bool Success, string Message);

/// <summary>
/// Service for injecting MDB_Bridge.dll into game processes.
/// </summary>
public interface IInjectorService
{
    /// <summary>
    /// Launch the game and inject MDB_Bridge.dll after GameAssembly.dll is loaded.
    /// </summary>
    /// <param name="gameExePath">Path to the game EXE.</param>
    /// <param name="bridgeDllPath">Path to MDB_Bridge.dll.</param>
    /// <param name="launchArgs">Optional launch arguments.</param>
    /// <param name="pollIntervalMs">Poll interval for GameAssembly.dll detection.</param>
    /// <param name="timeoutSeconds">Timeout for waiting for GameAssembly.dll.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<InjectionResult> LaunchAndInjectAsync(
        string gameExePath,
        string bridgeDllPath,
        string launchArgs = "",
        int pollIntervalMs = 500,
        int timeoutSeconds = 60,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inject MDB_Bridge.dll into an already-running process.
    /// </summary>
    /// <param name="processId">Target process ID.</param>
    /// <param name="bridgeDllPath">Path to MDB_Bridge.dll.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    Task<InjectionResult> InjectIntoProcessAsync(
        int processId,
        string bridgeDllPath,
        IProgress<string>? progress = null);

    /// <summary>
    /// Check if a module is loaded in a remote process.
    /// </summary>
    bool IsModuleLoaded(uint processId, string moduleName);
}
