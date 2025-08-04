using System.Text.Json;

using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.IO;

/// <summary>
/// Generates JSON format redundancy reports
/// </summary>
public class JsonReportGenerator : IReportGenerator
{
    private readonly ILogger<JsonReportGenerator> logger;

    public JsonReportGenerator(ILogger<JsonReportGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public async Task GenerateAsync(RedundancyReport report, string outputPath)
    {
        logger.LogInformation("Generating JSON report to {Path}", outputPath);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Create minimal LLM-ready output - only include methods with source locations
        var output = new
        {
            highConfidence = report.HighConfidenceMethods
                .Where(m => m.FilePath != null && m.LineNumber != null)
                .Select(m => new
                {
                    file = m.FilePath,
                    line = m.LineNumber,
                    method = m.Method.MethodName,
                    dependencies = m.Dependencies
                })
                .ToList(), // Force evaluation
            mediumConfidence = report.MediumConfidenceMethods
                .Where(m => m.FilePath != null && m.LineNumber != null)
                .Select(m => new
                {
                    file = m.FilePath,
                    line = m.LineNumber,
                    method = m.Method.MethodName,
                    dependencies = m.Dependencies
                })
                .ToList(), // Force evaluation
            lowConfidence = report.LowConfidenceMethods
                .Where(m => m.FilePath != null && m.LineNumber != null)
                .Select(m => new
                {
                    file = m.FilePath,
                    line = m.LineNumber,
                    method = m.Method.MethodName,
                    dependencies = m.Dependencies
                })
                .ToList() // Force evaluation
        };

        string json = JsonSerializer.Serialize(output, options);
        await File.WriteAllTextAsync(outputPath, json);

        logger.LogInformation("Report generated successfully");
    }
}