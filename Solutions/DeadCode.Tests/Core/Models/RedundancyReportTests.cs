using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class RedundancyReportTests
{
    private RedundancyReport report = null!;

    [TestInitialize]
    public void Setup()
    {
        report = new RedundancyReport
        {
            AnalyzedAssemblies = ["Assembly1.dll", "Assembly2.dll"],
            TraceScenarios = ["scenario1", "scenario2"]
        };
    }

    [TestMethod]
    public void RedundancyReport_GeneratedAt_IsSetToCurrentTime()
    {
        // Arrange & Act
        RedundancyReport report = new();
        DateTime now = DateTime.UtcNow;

        // Assert
        report.GeneratedAt.ShouldBeLessThanOrEqualTo(now);
        report.GeneratedAt.ShouldBeGreaterThan(now.AddSeconds(-5));
    }

    [TestMethod]
    public void AddUnusedMethod_WithValidMethod_AddsToReport()
    {
        // Arrange
        UnusedMethod method = CreateUnusedMethod(SafetyClassification.HighConfidence);

        // Act
        report.AddUnusedMethod(method);

        // Assert
        report.UnusedMethods.Count.ShouldBe(1);
        report.UnusedMethods.First().ShouldBe(method);
    }

    [TestMethod]
    public void AddUnusedMethod_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => report.AddUnusedMethod(null!));
    }

    [TestMethod]
    public void AddUnusedMethods_WithMultipleMethods_AddsAllToReport()
    {
        // Arrange
        UnusedMethod[] methods = new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence)
        };

        // Act
        report.AddUnusedMethods(methods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(3);
    }

    [TestMethod]
    public void HighConfidenceMethods_ReturnsOnlyHighConfidenceMethods()
    {
        // Arrange
        report.AddUnusedMethods(new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence),
            CreateUnusedMethod(SafetyClassification.DoNotRemove)
        });

        // Act
        List<UnusedMethod> highConfidence = report.HighConfidenceMethods.ToList();

        // Assert
        highConfidence.Count.ShouldBe(2);
        highConfidence.All(m => m.Method.SafetyLevel == SafetyClassification.HighConfidence).ShouldBeTrue();
    }

    [TestMethod]
    public void MediumConfidenceMethods_ReturnsOnlyMediumConfidenceMethods()
    {
        // Arrange
        report.AddUnusedMethods(new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence)
        });

        // Act
        List<UnusedMethod> mediumConfidence = report.MediumConfidenceMethods.ToList();

        // Assert
        mediumConfidence.Count.ShouldBe(2);
        mediumConfidence.All(m => m.Method.SafetyLevel == SafetyClassification.MediumConfidence).ShouldBeTrue();
    }

    [TestMethod]
    public void LowConfidenceMethods_ReturnsOnlyLowConfidenceMethods()
    {
        // Arrange
        report.AddUnusedMethods(new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence)
        });

        // Act
        List<UnusedMethod> lowConfidence = report.LowConfidenceMethods.ToList();

        // Assert
        lowConfidence.Count.ShouldBe(3);
        lowConfidence.All(m => m.Method.SafetyLevel == SafetyClassification.LowConfidence).ShouldBeTrue();
    }

    [TestMethod]
    public void MethodsBySafety_GroupsMethodsCorrectly()
    {
        // Arrange
        report.AddUnusedMethods(new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence),
            CreateUnusedMethod(SafetyClassification.DoNotRemove)
        });

        // Act
        IReadOnlyDictionary<SafetyClassification, List<UnusedMethod>> methodsBySafety = report.MethodsBySafety;

        // Assert
        methodsBySafety.Count.ShouldBe(4);
        methodsBySafety[SafetyClassification.HighConfidence].Count.ShouldBe(2);
        methodsBySafety[SafetyClassification.MediumConfidence].Count.ShouldBe(1);
        methodsBySafety[SafetyClassification.LowConfidence].Count.ShouldBe(1);
        methodsBySafety[SafetyClassification.DoNotRemove].Count.ShouldBe(1);
    }

    [TestMethod]
    public void GetStatistics_ReturnsCorrectCounts()
    {
        // Arrange
        report.AddUnusedMethods(new[]
        {
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.HighConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.MediumConfidence),
            CreateUnusedMethod(SafetyClassification.LowConfidence),
            CreateUnusedMethod(SafetyClassification.DoNotRemove)
        });

        // Act
        ReportStatistics stats = report.GetStatistics();

        // Assert
        stats.TotalMethods.ShouldBe(7);
        stats.HighConfidence.ShouldBe(3);
        stats.MediumConfidence.ShouldBe(2);
        stats.LowConfidence.ShouldBe(1);
        stats.DoNotRemove.ShouldBe(1);
    }

    [TestMethod]
    public void UnusedMethod_FilePath_ReturnsSourceFileFromLocation()
    {
        // Arrange
        SourceLocation location = new("Test.cs", 10, 12, 20);
        MethodInfo method = new(
            AssemblyName: "Test",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );
        UnusedMethod unusedMethod = new(method, ["dep1"]);

        // Act & Assert
        unusedMethod.FilePath.ShouldBe("Test.cs");
        unusedMethod.LineNumber.ShouldBe(10);
    }

    [TestMethod]
    public void UnusedMethod_FilePath_ReturnsNullWhenNoLocation()
    {
        // Arrange
        MethodInfo method = new(
            AssemblyName: "Test",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: null
        );
        UnusedMethod unusedMethod = new(method, []);

        // Act & Assert
        unusedMethod.FilePath.ShouldBeNull();
        unusedMethod.LineNumber.ShouldBeNull();
    }

    // Helper method
    private static UnusedMethod CreateUnusedMethod(SafetyClassification safety)
    {
        MethodInfo method = new(
            AssemblyName: "Test",
            TypeName: "TestType",
            MethodName: $"Method_{safety}",
            Signature: "Method()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: safety,
            Location: null
        );

        return new UnusedMethod(method, []);
    }
}