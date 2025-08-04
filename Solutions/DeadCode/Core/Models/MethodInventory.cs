using System.Text.Json.Serialization;

namespace DeadCode.Core.Models;

/// <summary>
/// Represents the complete inventory of methods extracted from assemblies
/// </summary>
public class MethodInventory
{
    private readonly List<MethodInfo> methods = [];

    /// <summary>
    /// Gets or sets all methods in the inventory (for JSON serialization)
    /// </summary>
    [JsonPropertyName("methods")]
    public List<MethodInfo> Methods
    {
        get => methods;
        init => methods = value ?? [];
    }

    /// <summary>
    /// Gets the count of methods in the inventory
    /// </summary>
    public int Count => methods.Count;

    /// <summary>
    /// Gets methods grouped by assembly name
    /// </summary>
    public IReadOnlyDictionary<string, List<MethodInfo>> MethodsByAssembly =>
        methods.GroupBy(m => m.AssemblyName).ToDictionary(g => g.Key, g => g.ToList());

    /// <summary>
    /// Adds a method to the inventory
    /// </summary>
    public void AddMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        methods.Add(method);
    }

    /// <summary>
    /// Adds multiple methods to the inventory
    /// </summary>
    public void AddMethods(IEnumerable<MethodInfo> methods)
    {
        ArgumentNullException.ThrowIfNull(methods);
        this.methods.AddRange(methods);
    }

    /// <summary>
    /// Gets methods filtered by safety classification
    /// </summary>
    public IEnumerable<MethodInfo> GetMethodsBySafety(SafetyClassification safety) =>
        methods.Where(m => m.SafetyLevel == safety);
}