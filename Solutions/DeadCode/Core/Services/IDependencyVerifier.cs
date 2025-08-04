namespace DeadCode.Core.Services;

/// <summary>
/// Interface for verifying and installing required dependencies
/// </summary>
public interface IDependencyVerifier
{
    /// <summary>
    /// Checks if all required dependencies are installed
    /// </summary>
    /// <returns>True if all dependencies are available</returns>
    Task<bool> CheckDependenciesAsync();

    /// <summary>
    /// Installs missing dependencies
    /// </summary>
    /// <returns>True if installation succeeded</returns>
    Task<bool> InstallMissingDependenciesAsync();
}