namespace DeadCode.Core.Models;

/// <summary>
/// Represents comprehensive information about a method in the codebase
/// </summary>
public record MethodInfo(
    string AssemblyName,
    string TypeName,
    string MethodName,
    string Signature,
    MethodVisibility Visibility,
    SafetyClassification SafetyLevel,
    SourceLocation? Location = null
)
{
    /// <summary>
    /// Gets the fully qualified method name for comparison
    /// </summary>
    public string FullyQualifiedName => $"{TypeName}.{MethodName}";

    /// <summary>
    /// Gets whether this method has source location information
    /// </summary>
    public bool HasSourceLocation => Location is not null;
}