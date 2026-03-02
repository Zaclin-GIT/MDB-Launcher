namespace MDB.Launcher.Services;

/// <summary>
/// Service for managing process elevation (UAC) for DLL injection operations.
/// </summary>
public interface IProcessElevationService
{
    /// <summary>
    /// Check if the current process is running with administrator privileges.
    /// </summary>
    bool IsElevated { get; }

    /// <summary>
    /// Re-launch the current process with administrator elevation.
    /// Passes injection arguments so the elevated instance can perform the injection
    /// headlessly and exit.
    /// </summary>
    /// <param name="arguments">Command-line arguments for the elevated instance.</param>
    /// <returns>True if elevation was initiated, false if user cancelled UAC.</returns>
    bool RelaunchElevated(string arguments);
}
