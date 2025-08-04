namespace DeadCode.Core.Models;

/// <summary>
/// Represents the result of a profiling trace session
/// </summary>
public record TraceResult(
    string TraceFilePath,
    string ScenarioName,
    DateTime StartTime,
    DateTime EndTime,
    bool IsSuccessful,
    string? ErrorMessage = null
)
{
    /// <summary>
    /// Gets the duration of the trace session
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets whether the trace file exists
    /// </summary>
    public bool TraceFileExists => File.Exists(TraceFilePath);
}