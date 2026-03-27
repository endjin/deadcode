namespace DeadCode.Core.Models;

/// <summary>
/// Represents the final redundancy analysis report
/// </summary>
public class RedundancyReport
{
    private readonly List<UnusedMethod> unusedMethods = [];
    private IReadOnlyDictionary<SafetyClassification, List<UnusedMethod>>? cachedMethodsBySafety;

    /// <summary>
    /// Gets the timestamp when the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the assemblies that were analyzed
    /// </summary>
    public List<string> AnalyzedAssemblies { get; init; } = [];

    /// <summary>
    /// Gets the trace scenarios that were used
    /// </summary>
    public List<string> TraceScenarios { get; init; } = [];

    /// <summary>
    /// Gets all unused methods
    /// </summary>
    public IReadOnlyList<UnusedMethod> UnusedMethods => unusedMethods.AsReadOnly();

    /// <summary>
    /// Gets unused methods grouped by safety classification
    /// </summary>
    public IReadOnlyDictionary<SafetyClassification, List<UnusedMethod>> MethodsBySafety => GetOrBuildSafetyCache();

    /// <summary>
    /// Gets high confidence unused methods (safe to remove)
    /// </summary>
    public IEnumerable<UnusedMethod> HighConfidenceMethods =>
        GetOrBuildSafetyCache().TryGetValue(SafetyClassification.HighConfidence, out var list) ? list : [];

    /// <summary>
    /// Gets medium confidence unused methods (requires review)
    /// </summary>
    public IEnumerable<UnusedMethod> MediumConfidenceMethods =>
        GetOrBuildSafetyCache().TryGetValue(SafetyClassification.MediumConfidence, out var list) ? list : [];

    /// <summary>
    /// Gets low confidence unused methods (likely false positives)
    /// </summary>
    public IEnumerable<UnusedMethod> LowConfidenceMethods =>
        GetOrBuildSafetyCache().TryGetValue(SafetyClassification.LowConfidence, out var list) ? list : [];

    /// <summary>
    /// Adds an unused method to the report
    /// </summary>
    public void AddUnusedMethod(UnusedMethod method)
    {
        ArgumentNullException.ThrowIfNull(method);
        unusedMethods.Add(method);
        cachedMethodsBySafety = null;
    }

    /// <summary>
    /// Adds multiple unused methods to the report
    /// </summary>
    public void AddUnusedMethods(IEnumerable<UnusedMethod> methods)
    {
        ArgumentNullException.ThrowIfNull(methods);
        unusedMethods.AddRange(methods);
        cachedMethodsBySafety = null;
    }

    private IReadOnlyDictionary<SafetyClassification, List<UnusedMethod>> GetOrBuildSafetyCache() =>
        cachedMethodsBySafety ??= unusedMethods.GroupBy(um => um.Method.SafetyLevel)
                                                .ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// Gets summary statistics for the report
    /// </summary>
    public ReportStatistics GetStatistics() => new(
        TotalMethods: unusedMethods.Count,
        HighConfidence: HighConfidenceMethods.Count(),
        MediumConfidence: MediumConfidenceMethods.Count(),
        LowConfidence: LowConfidenceMethods.Count(),
        DoNotRemove: unusedMethods.Count(um => um.Method.SafetyLevel == SafetyClassification.DoNotRemove)
    );
}

/// <summary>
/// Summary statistics for a redundancy report
/// </summary>
public record ReportStatistics(
    int TotalMethods,
    int HighConfidence,
    int MediumConfidence,
    int LowConfidence,
    int DoNotRemove
);