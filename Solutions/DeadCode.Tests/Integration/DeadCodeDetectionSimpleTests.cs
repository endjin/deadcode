using DeadCode.Core.Models;
using DeadCode.Core.Services;
using DeadCode.Infrastructure.IO;
using DeadCode.Infrastructure.Profiling;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Integration;

[TestClass]
public class DeadCodeDetectionSimpleTests
{
    private readonly IServiceProvider serviceProvider;

    public DeadCodeDetectionSimpleTests()
    {
        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IMethodInventoryExtractor, ReflectionMethodExtractor>();
        services.AddSingleton<ISafetyClassifier, RuleBasedSafetyClassifier>();
        services.AddSingleton<IPdbReader, PdbReader>();
        services.AddSingleton<ITraceParser, TraceParser>();
        services.AddSingleton<IComparisonEngine, ComparisonEngine>();
        services.AddSingleton<IReportGenerator, JsonReportGenerator>();

        serviceProvider = services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task SimpleDeadCodeDetection_WorksCorrectly()
    {
        // Arrange - Create a simple method inventory
        MethodInventory inventory = new();
        inventory.Methods.AddRange(new[]
        {
            new MethodInfo("TestAssembly", "TestNamespace.Calculator", "Add", "Add(int,int)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Calculator", "Subtract", "Subtract(int,int)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Calculator", "Multiply", "Multiply(int,int)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Calculator", "Divide", "Divide(int,int)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Calculator", "CalculateSquareRoot", "CalculateSquareRoot(double)",
                MethodVisibility.Private, SafetyClassification.HighConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.StringHelper", "Reverse", "Reverse(string)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.StringHelper", "ToUpperCase", "ToUpperCase(string)",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
        });

        // Create executed methods set (simulating trace results)
        // These should match the FullyQualifiedName from the MethodInfo records
        HashSet<string> executedMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "TestNamespace.Calculator.Add",
            "TestNamespace.StringHelper.Reverse"
        };

        // Act
        IComparisonEngine comparisonEngine = serviceProvider.GetRequiredService<IComparisonEngine>();
        RedundancyReport report = await comparisonEngine.CompareAsync(inventory, executedMethods);

        // Assert
        report.ShouldNotBeNull();
        report.UnusedMethods.Count.ShouldBe(5); // 7 total - 2 used = 5 unused

        // Verify specific methods are marked as unused
        report.UnusedMethods.Any(m => m.Method.MethodName == "Subtract").ShouldBeTrue();
        report.UnusedMethods.Any(m => m.Method.MethodName == "Multiply").ShouldBeTrue();
        report.UnusedMethods.Any(m => m.Method.MethodName == "Divide").ShouldBeTrue();
        report.UnusedMethods.Any(m => m.Method.MethodName == "CalculateSquareRoot").ShouldBeTrue();
        report.UnusedMethods.Any(m => m.Method.MethodName == "ToUpperCase").ShouldBeTrue();

        // Verify used methods are NOT in unused list
        report.UnusedMethods.Any(m => m.Method.MethodName == "Add").ShouldBeFalse();
        report.UnusedMethods.Any(m => m.Method.MethodName == "Reverse").ShouldBeFalse();
    }

    [TestMethod]
    public async Task SafetyClassification_IsPreserved()
    {
        // Arrange
        MethodInventory inventory = new();
        inventory.Methods.AddRange(new[]
        {
            new MethodInfo("TestAssembly", "TestNamespace.Service", "PublicMethod", "PublicMethod()",
                MethodVisibility.Public, SafetyClassification.LowConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Service", "PrivateMethod", "PrivateMethod()",
                MethodVisibility.Private, SafetyClassification.HighConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Service", "ProtectedMethod", "ProtectedMethod()",
                MethodVisibility.Protected, SafetyClassification.MediumConfidence),
        });

        HashSet<string> executedMethods = []; // No methods executed

        // Act
        IComparisonEngine comparisonEngine = serviceProvider.GetRequiredService<IComparisonEngine>();
        RedundancyReport report = await comparisonEngine.CompareAsync(inventory, executedMethods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(3);

        UnusedMethod publicMethod = report.UnusedMethods.First(m => m.Method.MethodName == "PublicMethod");
        publicMethod.Method.SafetyLevel.ShouldBe(SafetyClassification.LowConfidence);

        UnusedMethod privateMethod = report.UnusedMethods.First(m => m.Method.MethodName == "PrivateMethod");
        privateMethod.Method.SafetyLevel.ShouldBe(SafetyClassification.HighConfidence);

        UnusedMethod protectedMethod = report.UnusedMethods.First(m => m.Method.MethodName == "ProtectedMethod");
        protectedMethod.Method.SafetyLevel.ShouldBe(SafetyClassification.MediumConfidence);
    }

    [TestMethod]
    public async Task TraceParser_ParsesTextFiles()
    {
        // Arrange
        string tempFile = Path.GetTempFileName() + ".txt";
        try
        {
            File.WriteAllText(tempFile, @"
Method Enter: TestNamespace.Calculator.Add(System.Int32, System.Int32)
Method Enter: TestNamespace.Calculator.Multiply(System.Int32, System.Int32)
Method Enter: TestNamespace.StringHelper.Reverse(System.String)
Method Enter: TestNamespace.Calculator.Add(System.Int32, System.Int32)
");

            // Act
            ITraceParser parser = serviceProvider.GetRequiredService<ITraceParser>();
            HashSet<string> executedMethods = await parser.ParseTraceAsync(tempFile);

            // Assert
            executedMethods.ShouldNotBeNull();
            executedMethods.Count.ShouldBe(3); // 3 unique methods (Add appears twice but counted once)
            // The signature normalizer should extract just the method name part
            executedMethods.Any(m => m.Contains("Add")).ShouldBeTrue();
            executedMethods.Any(m => m.Contains("Multiply")).ShouldBeTrue();
            executedMethods.Any(m => m.Contains("Reverse")).ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task EmptyTraceFile_ReturnsAllMethodsAsUnused()
    {
        // Arrange
        MethodInventory inventory = new();
        inventory.Methods.AddRange(new[]
        {
            new MethodInfo("TestAssembly", "TestNamespace.Service", "Method1", "Method1()",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
            new MethodInfo("TestAssembly", "TestNamespace.Service", "Method2", "Method2()",
                MethodVisibility.Public, SafetyClassification.MediumConfidence),
        });

        HashSet<string> executedMethods = []; // Empty - no methods executed

        // Act
        IComparisonEngine comparisonEngine = serviceProvider.GetRequiredService<IComparisonEngine>();
        RedundancyReport report = await comparisonEngine.CompareAsync(inventory, executedMethods);

        // Assert
        report.UnusedMethods.Count.ShouldBe(2);
        report.GetStatistics().TotalMethods.ShouldBe(2);
    }
}