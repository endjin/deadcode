using System.Reflection;

using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Reads debug information from PDB files
/// </summary>
public interface IPdbReader
{
    /// <summary>
    /// Gets the source location for a method from its PDB file
    /// </summary>
    Task<SourceLocation?> GetSourceLocationAsync(MethodBase method, string assemblyPath);
}