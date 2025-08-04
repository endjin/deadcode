using DeadCode.CLI.Commands;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DeadCode.Tests.CLI.Commands;

[TestClass]
public class FullCommandTests : IDisposable
{
    private readonly ExtractCommand mockExtractCommand;
    private readonly ProfileCommand mockProfileCommand;
    private readonly AnalyzeCommand mockAnalyzeCommand;
    private readonly ILogger<FullCommand> mockLogger;
    private readonly TestConsole testConsole;
    private readonly FullCommand command;
    private readonly CommandContext context;

    public FullCommandTests()
    {
        testConsole = new TestConsole();

        mockExtractCommand = Substitute.For<ExtractCommand>(
            Substitute.For<DeadCode.Core.Services.IMethodInventoryExtractor>(),
            Substitute.For<ILogger<ExtractCommand>>(),
            testConsole);

        mockProfileCommand = Substitute.For<ProfileCommand>(
            Substitute.For<DeadCode.Core.Services.ITraceRunner>(),
            Substitute.For<DeadCode.Core.Services.IDependencyVerifier>(),
            Substitute.For<ILogger<ProfileCommand>>(),
            testConsole);

        mockAnalyzeCommand = Substitute.For<AnalyzeCommand>(
            Substitute.For<DeadCode.Core.Services.IComparisonEngine>(),
            Substitute.For<DeadCode.Core.Services.ITraceParser>(),
            Substitute.For<DeadCode.Core.Services.IReportGenerator>(),
            Substitute.For<ILogger<AnalyzeCommand>>(),
            testConsole);

        mockLogger = Substitute.For<ILogger<FullCommand>>();
        command = new FullCommand(mockExtractCommand, mockProfileCommand, mockAnalyzeCommand, mockLogger, testConsole);
        context = null!; // CommandContext is sealed, can't mock
    }

    [TestMethod]
    public void Constructor_WithNullExtractCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FullCommand(null!, mockProfileCommand, mockAnalyzeCommand, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullProfileCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FullCommand(mockExtractCommand, null!, mockAnalyzeCommand, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullAnalyzeCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FullCommand(mockExtractCommand, mockProfileCommand, null!, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new FullCommand(mockExtractCommand, mockProfileCommand, mockAnalyzeCommand, null!, testConsole));
    }

    [TestMethod]
    public void Settings_Validation_RequiresAtLeastOneAssembly()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = [],
                ExecutablePath = tempExe
            };

            // Act
            ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeFalse();
            result.Message!.ShouldContain("At least one assembly path must be provided");
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public void Settings_Validation_RequiresExecutablePath()
    {
        // Arrange
        FullCommand.Settings settings = new()
        {
            Assemblies = ["test.dll"],
            ExecutablePath = ""
        };

        // Act
        ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Executable path is required");
    }

    [TestMethod]
    public void Settings_Validation_RequiresExistingExecutable()
    {
        // Arrange
        FullCommand.Settings settings = new()
        {
            Assemblies = ["test.dll"],
            ExecutablePath = "nonexistent.exe"
        };

        // Act
        ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Executable not found: nonexistent.exe");
    }

    [TestMethod]
    public void Settings_Validation_SucceedsWithValidSettings()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe
            };

            // Act
            ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSuccessfulPipeline_Returns0()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName,
                MinConfidence = "high"
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFailedExtraction_ReturnsExitCode()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(1); // Failed extraction

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
            await mockProfileCommand.DidNotReceive().ExecuteAsync(Arg.Any<CommandContext>(), Arg.Any<ProfileCommand.Settings>());
            await mockAnalyzeCommand.DidNotReceive().ExecuteAsync(Arg.Any<CommandContext>(), Arg.Any<AnalyzeCommand.Settings>());
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFailedProfiling_ContinuesToAnalysis()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(1); // Failed profiling
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockAnalyzeCommand.Received(1).ExecuteAsync(Arg.Any<CommandContext>(), Arg.Any<AnalyzeCommand.Settings>());
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFailedAnalysis_ReturnsExitCode()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(1); // Failed analysis

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_PassesCorrectSettingsToSubCommands()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test1.dll", "test2.dll"],
                ExecutablePath = tempExe,
                ScenariosPath = "scenarios.json",
                OutputDirectory = tempDir.FullName,
                MinConfidence = "medium"
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            await mockExtractCommand.Received(1).ExecuteAsync(context,
                Arg.Is<ExtractCommand.Settings>(s =>
                    s.Assemblies.SequenceEqual(settings.Assemblies) &&
                    s.OutputPath == Path.Combine(settings.OutputDirectory, "inventory.json") &&
                    s.IncludeGenerated == false));

            await mockProfileCommand.Received(1).ExecuteAsync(context,
                Arg.Is<ProfileCommand.Settings>(s =>
                    s.ExecutablePath == settings.ExecutablePath &&
                    s.ScenariosPath == settings.ScenariosPath &&
                    s.OutputDirectory == Path.Combine(settings.OutputDirectory, "traces")));

            await mockAnalyzeCommand.Received(1).ExecuteAsync(context,
                Arg.Is<AnalyzeCommand.Settings>(s =>
                    s.InventoryPath == Path.Combine(settings.OutputDirectory, "inventory.json") &&
                    s.TracePaths.SequenceEqual(new[] { Path.Combine(settings.OutputDirectory, "traces") }) &&
                    s.OutputPath == Path.Combine(settings.OutputDirectory, "report.json") &&
                    s.MinConfidence == settings.MinConfidence));
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_CreatesOutputDirectory()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            Directory.Exists(tempDir).ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempExe);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithException_LogsErrorAndReturns1()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            InvalidOperationException expectedException = new("Test error");
            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .ThrowsAsync(expectedException);

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
            mockLogger.Received().LogError(expectedException, "Failed to complete analysis pipeline");
            string output = testConsole.Output;
            output.ShouldContain("InvalidOperationException");
            output.ShouldContain("Test error");
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_LogsInformationMessages()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            mockLogger.Received().LogInformation("Starting full deadcode analysis pipeline");
            mockLogger.Received().LogInformation("Full analysis pipeline completed successfully");
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WritesExpectedConsoleOutput()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            FullCommand.Settings settings = new()
            {
                Assemblies = ["test.dll"],
                ExecutablePath = tempExe,
                OutputDirectory = tempDir.FullName
            };

            mockExtractCommand.ExecuteAsync(context, Arg.Any<ExtractCommand.Settings>())
                .Returns(0);
            mockProfileCommand.ExecuteAsync(context, Arg.Any<ProfileCommand.Settings>())
                .Returns(0);
            mockAnalyzeCommand.ExecuteAsync(context, Arg.Any<AnalyzeCommand.Settings>())
                .Returns(0);

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            string output = testConsole.Output;
            output.ShouldContain("DeadCode Analysis Pipeline");
            output.ShouldContain("Step 1:");
            output.ShouldContain("Extracting method inventory");
            output.ShouldContain("Step 2:");
            output.ShouldContain("Profiling application execution");
            output.ShouldContain("Step 3:");
            output.ShouldContain("Analyzing for unused code");
            output.ShouldContain("Analysis Complete");
            output.ShouldContain("inventory.json");
            output.ShouldContain("traces/");
            output.ShouldContain("report.json");
            output.ShouldContain("deadcode analyze --help");
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}