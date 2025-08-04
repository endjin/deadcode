using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Interface for generating redundancy reports
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a report file from the redundancy analysis
    /// </summary>
    /// <param name="report">The redundancy report to save</param>
    /// <param name="outputPath">Path where the report should be saved</param>
    Task GenerateAsync(RedundancyReport report, string outputPath);
}