using System.Reflection;

using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.Reflection;

/// <summary>
/// Rule-based safety classifier for methods
/// </summary>
public class RuleBasedSafetyClassifier : ISafetyClassifier
{
    private readonly ILogger<RuleBasedSafetyClassifier> logger;

    public RuleBasedSafetyClassifier(ILogger<RuleBasedSafetyClassifier> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public SafetyClassification ClassifyMethod(MethodBase method)
    {
        ArgumentNullException.ThrowIfNull(method);

        // Check for special methods first (property getters/setters, event add/remove)
        if (method.IsSpecialName && !method.Name.StartsWith("op_")) // Exclude operators
        {
            return SafetyClassification.MediumConfidence;
        }

        // Check for DoNotRemove conditions
        if (HasFrameworkAttributes(method) || HasFrameworkAttributesOnType(method))
        {
            return SafetyClassification.DoNotRemove;
        }

        if (IsSecurityCritical(method))
        {
            return SafetyClassification.DoNotRemove;
        }

        if (IsEventHandler(method))
        {
            return SafetyClassification.MediumConfidence;
        }

        if (method.IsVirtual || method.IsAbstract)
        {
            return SafetyClassification.MediumConfidence;
        }

        if (method.IsFamily || method.IsFamilyOrAssembly) // protected or protected internal
        {
            return SafetyClassification.MediumConfidence;
        }

        // Check for LowConfidence conditions
        if (method.IsPublic)
        {
            return SafetyClassification.LowConfidence;
        }

        if (IsTestMethod(method))
        {
            return SafetyClassification.LowConfidence;
        }

        // HighConfidence - private methods with no special attributes
        if (method.IsPrivate)
        {
            return SafetyClassification.HighConfidence;
        }

        // Default to medium confidence
        return SafetyClassification.MediumConfidence;
    }

    private bool HasFrameworkAttributes(MethodBase method)
    {
        object[] attributes = method.GetCustomAttributes(false);

        // Check for specific framework attributes that indicate the method shouldn't be removed
        return attributes.Any(attr =>
        {
            Type type = attr.GetType();
            string fullName = type.FullName ?? "";

            // Check for specific framework attributes
            return type == typeof(System.Runtime.InteropServices.DllImportAttribute) ||
                   type == typeof(System.Runtime.InteropServices.ComVisibleAttribute) ||
                   fullName.Contains("System.CodeDom.Compiler.GeneratedCode");
        });
    }

    private bool IsSecurityCritical(MethodBase method)
    {
        // In modern .NET, many methods are marked as SecurityCritical by default
        // We only care about methods explicitly marked with security attributes
        object[] attributes = method.GetCustomAttributes(false);
        return attributes.Any(attr =>
        {
            string typeName = attr.GetType().FullName ?? "";
            return typeName.Contains("System.Security") &&
                   (typeName.Contains("SecurityCritical") ||
                    typeName.Contains("SecuritySafeCritical"));
        });
    }

    private bool IsTestMethod(MethodBase method)
    {
        object[] attributes = method.GetCustomAttributes(false);
        return attributes.Any(attr =>
        {
            string typeName = attr.GetType().Name;
            return typeName.Contains("Test") ||
                   typeName.Contains("Fact") ||
                   typeName.Contains("Theory");
        });
    }

    private bool IsEventHandler(MethodBase method)
    {
        ParameterInfo[] parameters = method.GetParameters();

        // Check for event handler signature (object sender, EventArgs e)
        if (parameters.Length == 2)
        {
            return parameters[0].ParameterType == typeof(object) &&
                   typeof(EventArgs).IsAssignableFrom(parameters[1].ParameterType);
        }

        return false;
    }

    private bool HasFrameworkAttributesOnType(MethodBase method)
    {
        if (method.DeclaringType == null)
        {
            return false;
        }

        object[] attributes = method.DeclaringType.GetCustomAttributes(false);

        // Check for type-level attributes that affect all methods
        return attributes.Any(attr =>
        {
            Type type = attr.GetType();
            return type == typeof(SerializableAttribute) ||
                   attr.GetType().FullName?.Contains("System.Runtime.Serialization") == true;
        });
    }
}