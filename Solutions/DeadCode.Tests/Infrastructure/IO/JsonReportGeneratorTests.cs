using System.Text.Json;

using DeadCode.Core.Models;
using DeadCode.Infrastructure.IO;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.IO;

[TestClass]
public class JsonReportGeneratorTests
{
    private JsonReportGenerator generator = null!;
    private ILogger<JsonReportGenerator> logger = null!;
    private string _tempFile = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<JsonReportGenerator>>();
        generator = new JsonReportGenerator(logger);
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [TestMethod]
    public async Task GenerateAsync_CreatesFileWithCorrectFormat()
    {
        // Arrange
        RedundancyReport report = CreateTestReport();

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert
        File.Exists(_tempFile).ShouldBeTrue();

        string json = await File.ReadAllTextAsync(_tempFile);
        json.ShouldNotBeNullOrWhiteSpace();

        // Verify it's valid JSON
        JsonDocument parsed = JsonDocument.Parse(json);
        parsed.RootElement.TryGetProperty("highConfidence", out _).ShouldBeTrue();
        parsed.RootElement.TryGetProperty("mediumConfidence", out _).ShouldBeTrue();
        parsed.RootElement.TryGetProperty("lowConfidence", out _).ShouldBeTrue();
    }

    [TestMethod]
    public async Task GenerateAsync_WritesHighConfidenceMethods()
    {
        // Arrange
        RedundancyReport report = new();
        SourceLocation location = new("Test.cs", 42, 44, 50);
        MethodInfo method = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "UnusedMethod",
            Signature: "UnusedMethod()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );
        UnusedMethod unusedMethod = new(method, ["registration:Program.cs:23"]);
        report.AddUnusedMethod(unusedMethod);

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert
        string json = await File.ReadAllTextAsync(_tempFile);
        JsonDocument parsed = JsonDocument.Parse(json);

        JsonElement highConfidence = parsed.RootElement.GetProperty("highConfidence");
        highConfidence.GetArrayLength().ShouldBe(1);

        JsonElement firstMethod = highConfidence[0];
        firstMethod.GetProperty("file").GetString().ShouldBe("Test.cs");
        firstMethod.GetProperty("line").GetInt32().ShouldBe(42);
        firstMethod.GetProperty("method").GetString().ShouldBe("UnusedMethod");

        JsonElement dependencies = firstMethod.GetProperty("dependencies");
        dependencies.GetArrayLength().ShouldBe(1);
        dependencies[0].GetString().ShouldBe("registration:Program.cs:23");
    }

    [TestMethod]
    public async Task GenerateAsync_HandlesMissingSourceLocation()
    {
        // Arrange
        RedundancyReport report = new();
        MethodInfo method = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "NoLocationMethod",
            Signature: "NoLocationMethod()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.LowConfidence,
            Location: null
        );
        UnusedMethod unusedMethod = new(method, []);
        report.AddUnusedMethod(unusedMethod);

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert
        string json = await File.ReadAllTextAsync(_tempFile);
        JsonDocument parsed = JsonDocument.Parse(json);

        JsonElement lowConfidence = parsed.RootElement.GetProperty("lowConfidence");
        lowConfidence.GetArrayLength().ShouldBe(1);

        JsonElement firstMethod = lowConfidence[0];
        firstMethod.GetProperty("file").ValueKind.ShouldBe(JsonValueKind.Null);
        firstMethod.GetProperty("line").ValueKind.ShouldBe(JsonValueKind.Null);
        firstMethod.GetProperty("method").GetString().ShouldBe("NoLocationMethod");
    }

    [TestMethod]
    public async Task GenerateAsync_GroupsMethodsByConfidenceLevel()
    {
        // Arrange
        RedundancyReport report = new();

        // Add methods of different confidence levels
        for (int i = 0; i < 3; i++)
        {
            report.AddUnusedMethod(CreateUnusedMethod($"HighMethod{i}", SafetyClassification.HighConfidence));
        }

        for (int i = 0; i < 2; i++)
        {
            report.AddUnusedMethod(CreateUnusedMethod($"MediumMethod{i}", SafetyClassification.MediumConfidence));
        }

        report.AddUnusedMethod(CreateUnusedMethod("LowMethod", SafetyClassification.LowConfidence));
        report.AddUnusedMethod(CreateUnusedMethod("DoNotRemoveMethod", SafetyClassification.DoNotRemove));

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert
        string json = await File.ReadAllTextAsync(_tempFile);
        JsonDocument parsed = JsonDocument.Parse(json);

        parsed.RootElement.GetProperty("highConfidence").GetArrayLength().ShouldBe(3);
        parsed.RootElement.GetProperty("mediumConfidence").GetArrayLength().ShouldBe(2);
        parsed.RootElement.GetProperty("lowConfidence").GetArrayLength().ShouldBe(1);

        // DoNotRemove methods should not be included
        parsed.RootElement.TryGetProperty("doNotRemove", out _).ShouldBeFalse();
    }

    [TestMethod]
    public async Task GenerateAsync_UsesIndentedFormatting()
    {
        // Arrange
        RedundancyReport report = new();
        report.AddUnusedMethod(CreateUnusedMethod("TestMethod", SafetyClassification.HighConfidence));

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert
        string json = await File.ReadAllTextAsync(_tempFile);

        // Check for indentation (should contain newlines and spaces)
        json.ShouldContain("\n");
        json.ShouldContain("  "); // Indentation
    }

    [TestMethod]
    public async Task GenerateAsync_LogsInformation()
    {
        // Arrange
        RedundancyReport report = CreateTestReport();

        // Act
        await generator.GenerateAsync(report, _tempFile);

        // Assert - check that logging was called at least twice (can't easily check exact messages with extension methods)
        logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log").ShouldBeGreaterThanOrEqualTo(2);
    }

    // Helper methods
    private static RedundancyReport CreateTestReport()
    {
        RedundancyReport report = new();
        report.AddUnusedMethod(CreateUnusedMethod("Method1", SafetyClassification.HighConfidence));
        report.AddUnusedMethod(CreateUnusedMethod("Method2", SafetyClassification.MediumConfidence));
        report.AddUnusedMethod(CreateUnusedMethod("Method3", SafetyClassification.LowConfidence));
        return report;
    }

    private static UnusedMethod CreateUnusedMethod(string name, SafetyClassification safety)
    {
        MethodInfo method = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: name,
            Signature: $"{name}()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: safety,
            Location: safety == SafetyClassification.HighConfidence
                ? new SourceLocation($"{name}.cs", 10, 12, 20)
                : null
        );

        return new UnusedMethod(method, ["dep1", "dep2"]);
    }
}