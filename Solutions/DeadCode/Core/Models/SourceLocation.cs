namespace DeadCode.Core.Models;

/// <summary>
/// Represents the source code location of a method
/// </summary>
public record SourceLocation(
    string SourceFile,
    int DeclarationLine,
    int BodyStartLine,
    int BodyEndLine
);