using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.IO;

/// <summary>
/// Compares static inventory against dynamic traces
/// </summary>
public class ComparisonEngine : IComparisonEngine
{
    private readonly ILogger<ComparisonEngine> logger;

    public ComparisonEngine(ILogger<ComparisonEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public Task<RedundancyReport> CompareAsync(MethodInventory inventory, HashSet<string> executedMethods)
    {
        logger.LogInformation(
            "Comparing {InventoryCount} methods against {ExecutedCount} executed methods",
            inventory.Count, executedMethods.Count);

        RedundancyReport report = IdentifyUnusedMethods(inventory, executedMethods);

        return Task.FromResult(report);
    }

    public RedundancyReport IdentifyUnusedMethods(MethodInventory inventory, HashSet<string> executedMethods)
    {
        RedundancyReport report = new()
        {
            AnalyzedAssemblies = inventory.MethodsByAssembly.Keys.ToList(),
            TraceScenarios = ["default"]
        };

        // Create a case-insensitive set for comparison
        HashSet<string> executedMethodsLower = new(
            executedMethods.Select(m => m.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase
        );


        foreach (MethodInfo method in inventory.Methods)
        {
            // Skip methods marked as DoNotRemove
            if (method.SafetyLevel == SafetyClassification.DoNotRemove)
            {
                logger.LogDebug("Skipping DoNotRemove method: {Method}", method.FullyQualifiedName);
                continue;
            }

            // Check if the method was executed
            string fullyQualifiedName = method.FullyQualifiedName.ToLowerInvariant();

            if (!executedMethodsLower.Contains(fullyQualifiedName))
            {
                logger.LogDebug("Method not found in execution trace: {Method}", method.FullyQualifiedName);

                // Note: Dependency analysis could be enhanced to track registration points,
                // initialization code, and cross-assembly references for more accurate reporting
                List<string> dependencies = [];

                UnusedMethod unusedMethod = new(method, dependencies);
                report.AddUnusedMethod(unusedMethod);
            }
            else
            {
                logger.LogDebug("Method was executed: {Method}", method.FullyQualifiedName);
            }
        }

        logger.LogInformation(
            "Identified {UnusedCount} unused methods out of {TotalCount} total methods",
            report.UnusedMethods.Count,
            inventory.Count
        );

        ReportStatistics stats = report.GetStatistics();
        logger.LogInformation(
            "Unused methods by confidence: High={High}, Medium={Medium}, Low={Low}",
            stats.HighConfidence,
            stats.MediumConfidence,
            stats.LowConfidence
        );

        return report;
    }
}