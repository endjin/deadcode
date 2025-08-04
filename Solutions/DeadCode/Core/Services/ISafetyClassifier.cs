using System.Reflection;

using DeadCode.Core.Models;

namespace DeadCode.Core.Services;

/// <summary>
/// Interface for classifying method safety levels
/// </summary>
public interface ISafetyClassifier
{
    /// <summary>
    /// Classifies a method's safety level for removal
    /// </summary>
    /// <param name="method">The method to classify</param>
    /// <returns>Safety classification level</returns>
    SafetyClassification ClassifyMethod(MethodBase method);
}