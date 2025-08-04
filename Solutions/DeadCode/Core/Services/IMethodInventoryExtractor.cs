using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Interface for extracting method inventory from assemblies
/// </summary>
public interface IMethodInventoryExtractor
{
    /// <summary>
    /// Extracts all methods from the specified assemblies
    /// </summary>
    /// <param name="assemblyPaths">Paths to assemblies to analyze</param>
    /// <param name="options">Extraction options</param>
    /// <returns>Method inventory containing all discovered methods</returns>
    Task<MethodInventory> ExtractAsync(string[] assemblyPaths, ExtractionOptions options);
}

/// <summary>
/// Options for method extraction
/// </summary>
public class ExtractionOptions
{
    /// <summary>
    /// Whether to include compiler-generated methods
    /// </summary>
    public bool IncludeCompilerGenerated { get; init; }

    /// <summary>
    /// Progress reporter for extraction process
    /// </summary>
    public IProgress<ExtractionProgress>? Progress { get; init; }
}

/// <summary>
/// Progress information for extraction
/// </summary>
public record ExtractionProgress(
    int ProcessedAssemblies,
    int TotalAssemblies,
    string CurrentAssembly
);