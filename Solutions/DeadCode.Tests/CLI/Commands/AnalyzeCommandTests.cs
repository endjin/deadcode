using DeadCode.CLI.Commands;
using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

using NSubstitute.ExceptionExtensions;

using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DeadCode.Tests.CLI.Commands;

[TestClass]
public class AnalyzeCommandTests : IDisposable
{
    private readonly IComparisonEngine mockComparisonEngine;
    private readonly ITraceParser mockTraceParser;
    private readonly IReportGenerator mockReportGenerator;
    private readonly ILogger<AnalyzeCommand> mockLogger;
    private readonly TestConsole testConsole;
    private readonly AnalyzeCommand command;
    private readonly CommandContext context;

    public AnalyzeCommandTests()
    {
        mockComparisonEngine = Substitute.For<IComparisonEngine>();
        mockTraceParser = Substitute.For<ITraceParser>();
        mockReportGenerator = Substitute.For<IReportGenerator>();
        mockLogger = Substitute.For<ILogger<AnalyzeCommand>>();
        testConsole = new TestConsole();
        command = new AnalyzeCommand(mockComparisonEngine, mockTraceParser, mockReportGenerator, mockLogger, testConsole);
        context = null!; // CommandContext is sealed, can't mock
    }

    [TestMethod]
    public void Constructor_WithNullComparisonEngine_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AnalyzeCommand(null!, mockTraceParser, mockReportGenerator, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullTraceParser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AnalyzeCommand(mockComparisonEngine, null!, mockReportGenerator, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullReportGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AnalyzeCommand(mockComparisonEngine, mockTraceParser, null!, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AnalyzeCommand(mockComparisonEngine, mockTraceParser, mockReportGenerator, null!, testConsole));
    }

    [TestMethod]
    public void Settings_Validation_RequiresExistingInventoryFile()
    {
        // Arrange
        AnalyzeCommand.Settings settings = new()
        {
            InventoryPath = "nonexistent.json",
            TracePaths = ["trace.nettrace"]
        };

        // Act
        Spectre.Console.ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Inventory file not found: nonexistent.json");
    }

    [TestMethod]
    public void Settings_Validation_RequiresAtLeastOneTracePath()
    {
        // Arrange
        string tempInventory = Path.GetTempFileName();
        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = []
            };

            // Act
            Spectre.Console.ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeFalse();
            result.Message!.ShouldContain("At least one trace file or directory must be specified");
        }
        finally
        {
            File.Delete(tempInventory);
        }
    }

    [TestMethod]
    public void Settings_Validation_RequiresValidConfidenceLevel()
    {
        // Arrange
        string tempInventory = Path.GetTempFileName();
        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = ["trace.nettrace"],
                MinConfidence = "invalid"
            };

            // Act
            Spectre.Console.ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeFalse();
            result.Message!.ShouldContain("Min confidence must be one of: high, medium, low");
        }
        finally
        {
            File.Delete(tempInventory);
        }
    }

    [TestMethod]
    public void Settings_Validation_SucceedsWithValidSettings()
    {
        // Arrange
        string tempInventory = Path.GetTempFileName();
        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = ["trace.nettrace"],
                MinConfidence = "high"
            };

            // Act
            Spectre.Console.ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempInventory);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidInput_SuccessfulExecution()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        string tempTrace = CreateTempTraceFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempTrace],
                OutputPath = "report.json",
                MinConfidence = "high"
            };

            HashSet<string> executedMethods = ["TestMethod1"];
            mockTraceParser.ParseTraceAsync(tempTrace).Returns(executedMethods);

            RedundancyReport report = CreateTestReport();
            mockComparisonEngine.CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>())
                .Returns(report);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockTraceParser.Received(1).ParseTraceAsync(tempTrace);
            await mockComparisonEngine.Received(1).CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>());
            await mockReportGenerator.Received(1).GenerateAsync(Arg.Any<RedundancyReport>(), "report.json");
        }
        finally
        {
            File.Delete(tempInventory);
            File.Delete(tempTrace);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDirectoryTracePath_FindsTraceFiles()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
        string tempTrace1 = Path.Combine(tempDir.FullName, "trace1.nettrace");
        string tempTrace2 = Path.Combine(tempDir.FullName, "trace2.nettrace");

        try
        {
            File.WriteAllText(tempTrace1, "dummy trace 1");
            File.WriteAllText(tempTrace2, "dummy trace 2");

            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempDir.FullName]
            };

            HashSet<string> executedMethods = ["TestMethod1"];
            mockTraceParser.ParseTraceAsync(Arg.Any<string>()).Returns(executedMethods);

            RedundancyReport report = CreateTestReport();
            mockComparisonEngine.CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>())
                .Returns(report);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockTraceParser.Received(2).ParseTraceAsync(Arg.Any<string>());
        }
        finally
        {
            File.Delete(tempInventory);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoTraceFiles_Returns1()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = ["nonexistent-directory"]
            };

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
            string output = testConsole.Output;
            output.ShouldContain("No trace files found!");
        }
        finally
        {
            File.Delete(tempInventory);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithException_LogsErrorAndReturns1()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        string tempTrace = CreateTempTraceFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempTrace]
            };

            InvalidOperationException expectedException = new("Test error");
            mockTraceParser.ParseTraceAsync(tempTrace)
                .ThrowsAsync(expectedException);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
            mockLogger.Received().LogError(expectedException, "Failed to analyze redundancy");
        }
        finally
        {
            File.Delete(tempInventory);
            File.Delete(tempTrace);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_FiltersReportByConfidenceLevel()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        string tempTrace = CreateTempTraceFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempTrace],
                MinConfidence = "medium"
            };

            HashSet<string> executedMethods = ["TestMethod1"];
            mockTraceParser.ParseTraceAsync(tempTrace).Returns(executedMethods);

            RedundancyReport report = CreateTestReport();
            mockComparisonEngine.CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>())
                .Returns(report);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            // The filtering logic should include both high and medium confidence methods
            await mockReportGenerator.Received(1).GenerateAsync(
                Arg.Is<RedundancyReport>(r =>
                    r.MediumConfidenceMethods.Any() ||
                    r.HighConfidenceMethods.Any()),
                Arg.Any<string>());
        }
        finally
        {
            File.Delete(tempInventory);
            File.Delete(tempTrace);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_AggregatesMultipleTraceFiles()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        string tempTrace1 = CreateTempTraceFile();
        string tempTrace2 = CreateTempTraceFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempTrace1, tempTrace2]
            };

            HashSet<string> executedMethods1 = ["TestMethod1"];
            HashSet<string> executedMethods2 = ["TestMethod2"];

            mockTraceParser.ParseTraceAsync(tempTrace1).Returns(executedMethods1);
            mockTraceParser.ParseTraceAsync(tempTrace2).Returns(executedMethods2);

            RedundancyReport report = CreateTestReport();
            mockComparisonEngine.CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>())
                .Returns(report);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockComparisonEngine.Received(1).CompareAsync(
                Arg.Any<MethodInventory>(),
                Arg.Is<HashSet<string>>(methods =>
                    methods.Contains("TestMethod1") &&
                    methods.Contains("TestMethod2")));
        }
        finally
        {
            File.Delete(tempInventory);
            File.Delete(tempTrace1);
            File.Delete(tempTrace2);
        }
    }

    private static string CreateTempInventoryFile()
    {
        string tempFile = Path.GetTempFileName();
        MethodInventory inventory = new();
        inventory.AddMethod(new MethodInfo(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod1",
            Signature: "TestMethod1()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence
        ));

        string json = System.Text.Json.JsonSerializer.Serialize(inventory, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(tempFile, json);
        return tempFile;
    }

    private static string CreateTempTraceFile()
    {
        string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".nettrace");
        File.WriteAllText(tempFile, "dummy trace data");
        return tempFile;
    }

    [TestMethod]
    public async Task ExecuteAsync_WritesExpectedConsoleOutput()
    {
        // Arrange
        string tempInventory = CreateTempInventoryFile();
        string tempTrace = CreateTempTraceFile();

        try
        {
            AnalyzeCommand.Settings settings = new()
            {
                InventoryPath = tempInventory,
                TracePaths = [tempTrace],
                OutputPath = "test-report.json"
            };

            HashSet<string> executedMethods = ["TestMethod1"];
            mockTraceParser.ParseTraceAsync(tempTrace).Returns(executedMethods);

            RedundancyReport report = CreateTestReport();
            mockComparisonEngine.CompareAsync(Arg.Any<MethodInventory>(), Arg.Any<HashSet<string>>())
                .Returns(report);

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            string output = testConsole.Output;
            output.ShouldContain("✓");
            output.ShouldContain("Loaded");
            output.ShouldContain("Found");
            output.ShouldContain("trace files");
            output.ShouldContain("unique executed methods");
            output.ShouldContain("Redundancy Analysis Summary");
            output.ShouldContain("High Confidence");
            output.ShouldContain("Report saved to");
            output.ShouldContain("test-report.json");
        }
        finally
        {
            File.Delete(tempInventory);
            File.Delete(tempTrace);
        }
    }

    private static RedundancyReport CreateTestReport()
    {
        RedundancyReport report = new()
        {
            AnalyzedAssemblies = ["TestAssembly"],
            TraceScenarios = ["default"]
        };

        UnusedMethod unusedMethod = new(
            new MethodInfo(
                AssemblyName: "TestAssembly",
                TypeName: "TestType",
                MethodName: "UnusedMethod",
                Signature: "UnusedMethod()",
                Visibility: MethodVisibility.Private,
                SafetyLevel: SafetyClassification.HighConfidence
            ),
            []
        );

        report.AddUnusedMethod(unusedMethod);
        return report;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}