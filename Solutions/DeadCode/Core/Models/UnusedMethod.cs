namespace DeadCode.Core.Models;

/// <summary>
/// Represents a method that was not found in any execution traces
/// </summary>
public record UnusedMethod(
    MethodInfo Method,
    List<string> Dependencies
)
{
    /// <summary>
    /// Gets the file path if source location is available
    /// </summary>
    public string? FilePath => Method.Location?.SourceFile;

    /// <summary>
    /// Gets the line number where the method is declared
    /// </summary>
    public int? LineNumber => Method.Location?.DeclarationLine;
}