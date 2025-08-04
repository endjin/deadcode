namespace DeadCode.Core.Models;

public enum SafetyClassification
{
    /// <summary>
    /// Framework requirements, public APIs, security code - must not be removed
    /// </summary>
    DoNotRemove,

    /// <summary>
    /// Public methods, reflection possible, test methods - requires manual review
    /// </summary>
    LowConfidence,

    /// <summary>
    /// Internal/Protected, Virtual, DI Service, Event handlers - check carefully
    /// </summary>
    MediumConfidence,

    /// <summary>
    /// Private, no special attributes, not compiler-generated - safe to remove
    /// </summary>
    HighConfidence
}