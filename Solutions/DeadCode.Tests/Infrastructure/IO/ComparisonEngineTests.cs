using DeadCode.Core.Models;
using DeadCode.Infrastructure.IO;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.IO;

[TestClass]
public class ComparisonEngineTests
{
    private ComparisonEngine engine = null!;
    private ILogger<ComparisonEngine> logger = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<ComparisonEngine>>();
        engine = new ComparisonEngine(logger);
    }

    [TestMethod]
    public void IdentifyUnusedMethods_WithEmptyInventory_ReturnsEmptyReport()
    {
        // Arrange
        MethodInventory inventory = new();
        HashSet<string> executedMethods = ["Method1", "Method2"];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.ShouldNotBeNull();
        report.UnusedMethods.ShouldBeEmpty();
    }

    [TestMethod]
    public void IdentifyUnusedMethods_WithNoExecutedMethods_ReturnsAllMethodsAsUnused()
    {
        // Arrange
        MethodInventory inventory = new();
        MethodInfo method1 = CreateMethodInfo("Method1", SafetyClassification.HighConfidence);
        MethodInfo method2 = CreateMethodInfo("Method2", SafetyClassification.LowConfidence);
        inventory.AddMethod(method1);
        inventory.AddMethod(method2);

        HashSet<string> executedMethods = [];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(2);
        report.UnusedMethods.ShouldContain(m => m.Method.MethodName == "Method1");
        report.UnusedMethods.ShouldContain(m => m.Method.MethodName == "Method2");
    }

    [TestMethod]
    public void IdentifyUnusedMethods_WithSomeExecutedMethods_ReturnsOnlyUnusedMethods()
    {
        // Arrange
        MethodInventory inventory = new();
        MethodInfo method1 = CreateMethodInfo("ExecutedMethod", SafetyClassification.HighConfidence);
        MethodInfo method2 = CreateMethodInfo("UnusedMethod", SafetyClassification.HighConfidence);
        MethodInfo method3 = CreateMethodInfo("AnotherExecutedMethod", SafetyClassification.LowConfidence);

        inventory.AddMethod(method1);
        inventory.AddMethod(method2);
        inventory.AddMethod(method3);

        HashSet<string> executedMethods =
        [
            "TestType.ExecutedMethod",
            "TestType.AnotherExecutedMethod"
        ];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(1);
        report.UnusedMethods.First().Method.MethodName.ShouldBe("UnusedMethod");
    }

    [TestMethod]
    public void IdentifyUnusedMethods_ExcludesDoNotRemoveMethods()
    {
        // Arrange
        MethodInventory inventory = new();
        MethodInfo safeMethod = CreateMethodInfo("SafeMethod", SafetyClassification.HighConfidence);
        MethodInfo doNotRemoveMethod = CreateMethodInfo("CriticalMethod", SafetyClassification.DoNotRemove);

        inventory.AddMethod(safeMethod);
        inventory.AddMethod(doNotRemoveMethod);

        HashSet<string> executedMethods = [];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(1);
        report.UnusedMethods.First().Method.MethodName.ShouldBe("SafeMethod");
        report.UnusedMethods.ShouldNotContain(m => m.Method.MethodName == "CriticalMethod");
    }

    [TestMethod]
    public void IdentifyUnusedMethods_CaseInsensitiveMatching()
    {
        // Arrange
        MethodInventory inventory = new();
        MethodInfo method = CreateMethodInfo("TestMethod", SafetyClassification.HighConfidence);
        inventory.AddMethod(method);

        // Executed methods with different casing
        HashSet<string> executedMethods =
        [
            "TESTTYPE.TESTMETHOD" // All uppercase
        ];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.UnusedMethods.ShouldBeEmpty(); // Method should be considered as executed
    }

    [TestMethod]
    public void IdentifyUnusedMethods_HandlesMethodOverloads()
    {
        // Arrange
        MethodInventory inventory = new();
        MethodInfo method1 = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "OverloadedMethod",
            Signature: "OverloadedMethod()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence
        );
        MethodInfo method2 = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "OverloadedMethod",
            Signature: "OverloadedMethod(int)",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence
        );

        inventory.AddMethod(method1);
        inventory.AddMethod(method2);

        // Only one overload is executed
        HashSet<string> executedMethods =
        [
            "TestType.OverloadedMethod" // This would match both methods with same name
        ];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        // Both methods are matched by the single executed method name (overload ambiguity)
        report.UnusedMethods.ShouldBeEmpty();
    }

    [TestMethod]
    public void IdentifyUnusedMethods_PopulatesReportMetadata()
    {
        // Arrange
        MethodInventory inventory = new();
        inventory.AddMethod(CreateMethodInfo("Method1", SafetyClassification.HighConfidence));

        HashSet<string> executedMethods = [];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.AnalyzedAssemblies.Count.ShouldBe(1); // Set by IdentifyUnusedMethods
        report.AnalyzedAssemblies.First().ShouldBe("TestAssembly");
        report.TraceScenarios.Count.ShouldBe(1);
        report.TraceScenarios.First().ShouldBe("default");
        report.GeneratedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        report.GeneratedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [TestMethod]
    public void IdentifyUnusedMethods_GroupsByConfidenceLevel()
    {
        // Arrange
        MethodInventory inventory = new();
        inventory.AddMethod(CreateMethodInfo("HighConf1", SafetyClassification.HighConfidence));
        inventory.AddMethod(CreateMethodInfo("HighConf2", SafetyClassification.HighConfidence));
        inventory.AddMethod(CreateMethodInfo("MediumConf", SafetyClassification.MediumConfidence));
        inventory.AddMethod(CreateMethodInfo("LowConf", SafetyClassification.LowConfidence));

        HashSet<string> executedMethods = [];

        // Act
        RedundancyReport report = engine.IdentifyUnusedMethods(inventory, executedMethods);

        // Assert
        report.HighConfidenceMethods.Count().ShouldBe(2);
        report.MediumConfidenceMethods.Count().ShouldBe(1);
        report.LowConfidenceMethods.Count().ShouldBe(1);
    }

    // Helper method
    private static MethodInfo CreateMethodInfo(string name, SafetyClassification safety)
    {
        return new MethodInfo(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: name,
            Signature: $"{name}()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: safety
        );
    }
}