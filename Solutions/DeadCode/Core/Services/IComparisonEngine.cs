using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Interface for comparing static method inventory against dynamic execution traces
/// </summary>
public interface IComparisonEngine
{
    /// <summary>
    /// Compares method inventory against executed methods to find unused code
    /// </summary>
    /// <param name="inventory">Static method inventory from assemblies</param>
    /// <param name="executedMethods">Set of method signatures found in traces</param>
    /// <returns>Redundancy report containing unused methods</returns>
    Task<RedundancyReport> CompareAsync(MethodInventory inventory, HashSet<string> executedMethods);
}