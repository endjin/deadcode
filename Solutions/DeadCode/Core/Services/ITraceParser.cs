namespace DeadCode.Core.Services;

/// <summary>
/// Interface for parsing trace files to extract executed methods
/// </summary>
public interface ITraceParser
{
    /// <summary>
    /// Parses a trace file and extracts all executed method signatures
    /// </summary>
    /// <param name="traceFilePath">Path to the .nettrace file</param>
    /// <returns>Set of executed method signatures</returns>
    Task<HashSet<string>> ParseTraceAsync(string traceFilePath);
}