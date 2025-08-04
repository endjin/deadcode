using DeadCode.CLI.Commands;
using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

using NSubstitute.ExceptionExtensions;

using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace DeadCode.Tests.CLI.Commands;

[TestClass]
public class ProfileCommandTests : IDisposable
{
    private readonly ITraceRunner mockTraceRunner;
    private readonly IDependencyVerifier mockDependencyVerifier;
    private readonly ILogger<ProfileCommand> mockLogger;
    private readonly TestConsole testConsole;
    private readonly ProfileCommand command;
    private readonly CommandContext context;

    public ProfileCommandTests()
    {
        mockTraceRunner = Substitute.For<ITraceRunner>();
        mockDependencyVerifier = Substitute.For<IDependencyVerifier>();
        mockLogger = Substitute.For<ILogger<ProfileCommand>>();
        testConsole = new TestConsole();
        command = new ProfileCommand(mockTraceRunner, mockDependencyVerifier, mockLogger, testConsole);
        context = null!; // CommandContext is sealed, can't mock
    }

    [TestMethod]
    public void Constructor_WithNullTraceRunner_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ProfileCommand(null!, mockDependencyVerifier, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullDependencyVerifier_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ProfileCommand(mockTraceRunner, null!, mockLogger, testConsole));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ProfileCommand(mockTraceRunner, mockDependencyVerifier, null!, testConsole));
    }

    [TestMethod]
    public void Settings_Validation_RequiresExecutablePath()
    {
        // Arrange
        ProfileCommand.Settings settings = new()
        {
            ExecutablePath = ""
        };

        // Act
        Spectre.Console.ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Executable path is required");
    }

    [TestMethod]
    public void Settings_Validation_RequiresExistingExecutable()
    {
        // Arrange
        ProfileCommand.Settings settings = new()
        {
            ExecutablePath = "nonexistent.exe"
        };

        // Act
        Spectre.Console.ValidationResult result = settings.Validate();

        // Assert
        result.Successful.ShouldBeFalse();
        result.Message!.ShouldContain("Executable not found: nonexistent.exe");
    }

    [TestMethod]
    public void Settings_Validation_RequiresExistingScenariosFile()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe,
                ScenariosPath = "nonexistent.json"
            };

            // Act
            Spectre.Console.ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeFalse();
            result.Message!.ShouldContain("Scenarios file not found: nonexistent.json");
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public void Settings_Validation_SucceedsWithValidExecutable()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe
            };

            // Act
            Spectre.Console.ValidationResult result = settings.Validate();

            // Assert
            result.Successful.ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithMissingDependencies_Returns1()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(false);
            // User responds No to installation prompt
            testConsole.Input.PushTextWithEnter("n");

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
            string output = testConsole.Output;
            output.ShouldContain("dotnet-trace is not installed");
            output.ShouldContain("Would you like to install it now?");
            output.ShouldContain("Cannot proceed without dotnet-trace");
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSuccessfulProfiling_Returns0()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe,
                Arguments = ["--help"]
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateSuccessfulTraceResult());

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithFailedProfiling_Returns1()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateFailedTraceResult());

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithScenariosFile_LoadsScenarios()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        string tempScenarios = Path.GetTempFileName();
        try
        {
            string scenariosJson = """
                {
                    "scenarios": [
                        {
                            "name": "test-scenario",
                            "arguments": ["--test"],
                            "duration": 30,
                            "description": "Test scenario"
                        }
                    ]
                }
                """;
            await File.WriteAllTextAsync(tempScenarios, scenariosJson);

            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe,
                ScenariosPath = tempScenarios
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateSuccessfulTraceResult());

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockTraceRunner.Received(1).RunProfilingAsync(
                tempExe,
                Arg.Is<string[]>(args => args.SequenceEqual(new[] { "--test" })),
                Arg.Is<ProfilingOptions>(o => o.ScenarioName == "test-scenario"));
        }
        finally
        {
            File.Delete(tempExe);
            File.Delete(tempScenarios);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithoutScenariosFile_CreatesDefaultScenario()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe,
                Arguments = ["--help", "--verbose"]
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateSuccessfulTraceResult());

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(0);
            await mockTraceRunner.Received(1).RunProfilingAsync(
                tempExe,
                Arg.Is<string[]>(args => args.SequenceEqual(new[] { "--help", "--verbose" })),
                Arg.Is<ProfilingOptions>(o => o.ScenarioName == "default"));
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WithProfilingException_LogsError()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .ThrowsAsync(new InvalidOperationException("Test error"));

            // Act
            int result = await command.ExecuteAsync(context, settings);

            // Assert
            result.ShouldBe(1);

            // Verify that LogError was called with the expected exception
            mockLogger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Is<Exception>(ex => ex.Message == "Test error"),
                Arg.Any<Func<object, Exception?, string>>()
            );
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_PassesCorrectProfilingOptions()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe,
                OutputDirectory = "custom-output",
                Duration = 60
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateSuccessfulTraceResult());

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            await mockTraceRunner.Received(1).RunProfilingAsync(
                tempExe,
                Arg.Any<string[]>(),
                Arg.Is<ProfilingOptions>(o =>
                    o.OutputDirectory == "custom-output" &&
                    o.Duration == 60 &&
                    o.ScenarioName == "default"));
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task ExecuteAsync_WritesExpectedConsoleOutput()
    {
        // Arrange
        string tempExe = Path.GetTempFileName();
        try
        {
            ProfileCommand.Settings settings = new()
            {
                ExecutablePath = tempExe
            };

            mockDependencyVerifier.CheckDependenciesAsync().Returns(true);
            mockTraceRunner.RunProfilingAsync(
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<ProfilingOptions>())
                .Returns(CreateSuccessfulTraceResult());

            // Act
            await command.ExecuteAsync(context, settings);

            // Assert
            string output = testConsole.Output;
            output.ShouldContain("✓");
            output.ShouldContain("Scenario");
            output.ShouldContain("Duration");
            output.ShouldContain("Status");
        }
        finally
        {
            File.Delete(tempExe);
        }
    }

    private static TraceResult CreateSuccessfulTraceResult()
    {
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "dummy trace data");

        return new TraceResult(
            TraceFilePath: tempFile,
            ScenarioName: "test",
            StartTime: DateTime.UtcNow.AddMinutes(-1),
            EndTime: DateTime.UtcNow,
            IsSuccessful: true
        );
    }

    private static TraceResult CreateFailedTraceResult()
    {
        return new TraceResult(
            TraceFilePath: "/nonexistent/trace.nettrace",
            ScenarioName: "test",
            StartTime: DateTime.UtcNow.AddMinutes(-1),
            EndTime: DateTime.UtcNow,
            IsSuccessful: false,
            ErrorMessage: "Test error"
        );
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}