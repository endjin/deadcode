using DeadCode.CLI.Commands;
using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

using NSubstitute.ExceptionExtensions;

using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DeadCode.Tests.CLI.Commands;

[TestClass]
public class ExtractCommandTests : IDisposable
{
    private readonly IMethodInventoryExtractor mockExtractor;
    private readonly ILogger<ExtractCommand> mockLogger;
    private readonly TestConsole testConsole;
    private readonly ExtractCommand command;
    private readonly CommandContext context;
    private readonly string testDirectory;
    private readonly List<string> createdFiles = [];

    public ExtractCommandTests()
    {
        mockExtractor = Substitute.For<IMethodInventoryExtractor>();
        mockLogger = Substitute.For<ILogger<ExtractCommand>>();
        testConsole = new TestConsole();
        command = new ExtractCommand(mockExtractor, mockLogger, testConsole);
        context = null!; // CommandContext is sealed, can't mock
        testDirectory = Path.Combine(Path.GetTempPath(), $"DeadCodeTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test files
        foreach (string file in createdFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);
    }

    private string CreateTestFile(string fileName)
    {
        string fullPath = Path.Combine(testDirectory, fileName);
        File.WriteAllText(fullPath, "test");
        createdFiles.Add(fullPath);
        return fullPath;
    }

    private string[] CreateTestAssemblies(params string[] fileNames)
    {
        return fileNames.Select(CreateTestFile).ToArray();
    }

    [TestMethod]
    public void Constructor_WithNullExtractor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ExtractCommand(null!, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ExtractCommand(mockExtractor, null!, testConsole));
    }

    [TestMethod]
    public void Settings_Validation_RequiresAtLeastOneAssembly()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = []
        };

        // Act
        Spectre.Console.ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("At least one assembly path must be provided");
    }

    [TestMethod]
    public void Settings_Validation_SucceedsWithValidAssemblies()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "output.json")
        };

        // Act
        Spectre.Console.ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_WithValidInput_SuccessfulExecution()
    {
        // Arrange
        string testFile1 = CreateTestFile("test.dll");
        string testFile2 = CreateTestFile("another.dll");

        ExtractCommand.Settings settings = new()
        {
            Assemblies = [testFile1, testFile2],
            OutputPath = Path.Combine(testDirectory, "output.json"),
            IncludeGenerated = true
        };

        MethodInventory testInventory = CreateTestInventory();
        mockExtractor.ExtractAsync(
            Arg.Any<string[]>(),
            Arg.Is<ExtractionOptions>(o => o.IncludeCompilerGenerated == true))
            .Returns(testInventory);

        // Act
        int result = await command.ExecuteAsync(context, settings);

        // Assert
        result.ShouldBe(0);
        await mockExtractor.Received(1).ExtractAsync(
            Arg.Any<string[]>(),
            Arg.Any<ExtractionOptions>());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithException_LogsErrorAndReturns1()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "output.json")
        };

        InvalidOperationException expectedException = new("Test error");
        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .ThrowsAsync(expectedException);

        // Act
        int result = await command.ExecuteAsync(context, settings);

        // Assert
        result.ShouldBe(1); // The command returns 1 on exception
        mockLogger.Received().LogError(expectedException, "Failed to extract method inventory");
        string output = testConsole.Output;
        output.ShouldContain("InvalidOperationException");
        output.ShouldContain("Test error");
    }

    [TestMethod]
    public async Task ExecuteAsync_PassesCorrectExtractionOptions()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "output.json"),
            IncludeGenerated = true
        };

        MethodInventory testInventory = CreateTestInventory();
        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .Returns(testInventory);

        // Act
        await command.ExecuteAsync(context, settings);

        // Assert
        await mockExtractor.Received(1).ExtractAsync(
            Arg.Any<string[]>(),
            Arg.Is<ExtractionOptions>(o =>
                o.IncludeCompilerGenerated == true &&
                o.Progress != null));
    }

    [TestMethod]
    public async Task ExecuteAsync_WithDefaultSettings_UsesDefaults()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll")
            // OutputPath and IncludeGenerated use defaults
        };

        MethodInventory testInventory = CreateTestInventory();
        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .Returns(testInventory);

        // Act
        int result = await command.ExecuteAsync(context, settings);

        // Assert
        result.ShouldBe(0);
        settings.OutputPath.ShouldBe("inventory.json");
        settings.IncludeGenerated.ShouldBeFalse();
    }

    [TestMethod]
    public async Task ExecuteAsync_CallsProgressCallback()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "output.json")
        };

        MethodInventory testInventory = CreateTestInventory();

        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .Returns(callInfo =>
            {
                ExtractionOptions options = callInfo.Arg<ExtractionOptions>();
                // Simulate progress callback
                options.Progress?.Report(new ExtractionProgress(
                    ProcessedAssemblies: 1,
                    TotalAssemblies: 1,
                    CurrentAssembly: "test.dll"
                ));
                return testInventory;
            });

        // Act
        int result = await command.ExecuteAsync(context, settings);

        // Assert
        result.ShouldBe(0);
    }

    [TestMethod]
    public async Task ExecuteAsync_LogsInformationMessages()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "output.json")
        };

        MethodInventory testInventory = CreateTestInventory();
        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .Returns(testInventory);

        // Act
        await command.ExecuteAsync(context, settings);

        // Assert
        mockLogger.Received().LogInformation("Starting method inventory extraction");
        mockLogger.Received().LogInformation("Method inventory extraction completed successfully");
    }

    [TestMethod]
    public async Task ExecuteAsync_WritesExpectedConsoleOutput()
    {
        // Arrange
        ExtractCommand.Settings settings = new()
        {
            Assemblies = CreateTestAssemblies("test.dll"),
            OutputPath = Path.Combine(testDirectory, "test-output.json")
        };

        MethodInventory testInventory = CreateTestInventory();
        mockExtractor.ExtractAsync(Arg.Any<string[]>(), Arg.Any<ExtractionOptions>())
            .Returns(testInventory);

        // Act
        await command.ExecuteAsync(context, settings);

        // Assert
        string output = testConsole.Output;
        output.ShouldContain("✓");
        output.ShouldContain("Inventory saved to");
        output.ShouldContain("test-output.json");
        output.ShouldContain("Total Methods");
        output.ShouldContain("2"); // From CreateTestInventory
    }

    private static MethodInventory CreateTestInventory()
    {
        MethodInfo[] methods = new[]
        {
            new MethodInfo(
                AssemblyName: "TestAssembly",
                TypeName: "TestType",
                MethodName: "TestMethod1",
                Signature: "TestMethod1()",
                Visibility: MethodVisibility.Public,
                SafetyLevel: SafetyClassification.HighConfidence
            ),
            new MethodInfo(
                AssemblyName: "TestAssembly",
                TypeName: "TestType",
                MethodName: "TestMethod2",
                Signature: "TestMethod2(string)",
                Visibility: MethodVisibility.Private,
                SafetyLevel: SafetyClassification.MediumConfidence
            )
        };

        MethodInventory inventory = new();
        inventory.AddMethods(methods);
        return inventory;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}