using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Interface for running profiling traces on executables
/// </summary>
public interface ITraceRunner
{
    /// <summary>
    /// Runs profiling on the specified executable
    /// </summary>
    /// <param name="executablePath">Path to the executable to profile</param>
    /// <param name="arguments">Command line arguments for the executable</param>
    /// <param name="options">Profiling options</param>
    /// <returns>Trace result containing the output file path and status</returns>
    Task<TraceResult> RunProfilingAsync(
        string executablePath,
        string[] arguments,
        ProfilingOptions options);
}

/// <summary>
/// Options for profiling execution
/// </summary>
public class ProfilingOptions
{
    /// <summary>
    /// Output directory for trace files
    /// </summary>
    public required string OutputDirectory { get; init; }

    /// <summary>
    /// Name of the scenario being profiled
    /// </summary>
    public required string ScenarioName { get; init; }

    /// <summary>
    /// Maximum duration to run profiling (null for run to completion)
    /// </summary>
    public int? Duration { get; init; }

    /// <summary>
    /// Whether failure is expected for this scenario
    /// </summary>
    public bool ExpectFailure { get; init; }
}