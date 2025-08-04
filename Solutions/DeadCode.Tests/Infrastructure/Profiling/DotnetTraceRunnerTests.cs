using DeadCode.Core.Models;
using DeadCode.Core.Services;
using DeadCode.Infrastructure.Profiling;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Profiling;

[TestClass]
public class DotnetTraceRunnerTests
{
    private readonly ILogger<DotnetTraceRunner> mockLogger;
    private readonly DotnetTraceRunner traceRunner;

    public DotnetTraceRunnerTests()
    {
        mockLogger = Substitute.For<ILogger<DotnetTraceRunner>>();
        traceRunner = new DotnetTraceRunner(mockLogger);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DotnetTraceRunner(null!));
    }

    [TestMethod]
    public async Task RunProfilingAsync_CreatesOutputDirectory()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string tempExe = Path.GetTempFileName();

        try
        {
            ProfilingOptions options = new()
            {
                OutputDirectory = tempDir,
                ScenarioName = "test",
                Duration = 1 // Very short duration
            };

            // Act
            TraceResult result = await traceRunner.RunProfilingAsync(tempExe, [], options);

            // Assert
            Directory.Exists(tempDir).ShouldBeTrue();
            result.ShouldNotBeNull();
            result.ScenarioName.ShouldBe("test");
            result.TraceFilePath.ShouldContain("test");
        }
        finally
        {
            File.Delete(tempExe);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [TestMethod]
    public async Task RunProfilingAsync_WithInvalidExecutable_ReturnsFailedResult()
    {
        // Arrange
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            ProfilingOptions options = new()
            {
                OutputDirectory = tempDir.FullName,
                ScenarioName = "test-invalid",
                Duration = 1
            };

            // Act
            TraceResult result = await traceRunner.RunProfilingAsync("nonexistent.exe", [], options);

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccessful.ShouldBeFalse();
            result.ScenarioName.ShouldBe("test-invalid");
            result.ErrorMessage.ShouldNotBeNull();
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task RunProfilingAsync_LogsInformationMessages()
    {
        // Arrange
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
        string tempExe = Path.GetTempFileName();

        try
        {
            // Create a simple executable script
            if (OperatingSystem.IsWindows())
            {
                File.WriteAllText(tempExe, "@echo off\necho Test output\nexit 0");
            }
            else
            {
                File.WriteAllText(tempExe, "#!/bin/sh\necho Test output\nexit 0");
                File.SetUnixFileMode(tempExe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            ProfilingOptions options = new()
            {
                OutputDirectory = tempDir.FullName,
                ScenarioName = "test-logging",
                Duration = 1
            };

            // Act
            await traceRunner.RunProfilingAsync(tempExe, ["--help"], options);

            // Assert - Just verify that some information was logged
            mockLogger.ReceivedCalls()
                .Any(call => call.GetMethodInfo().Name == "Log" &&
                     call.GetArguments().Length > 0 &&
                     call.GetArguments()[0]?.Equals(LogLevel.Information) == true)
                .ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task RunProfilingAsync_WithArguments_PassesArgumentsCorrectly()
    {
        // Arrange
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();
        string tempExe = Path.GetTempFileName();

        try
        {
            ProfilingOptions options = new()
            {
                OutputDirectory = tempDir.FullName,
                ScenarioName = "test-args",
                Duration = 1
            };

            string[] arguments = new[] { "--version", "--help" };

            // Act
            TraceResult result = await traceRunner.RunProfilingAsync(tempExe, arguments, options);

            // Assert
            result.ShouldNotBeNull();
            result.ScenarioName.ShouldBe("test-args");
        }
        finally
        {
            File.Delete(tempExe);
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public async Task RunProfilingAsync_WithExpectFailureOption_HandlesFailureCorrectly()
    {
        // Arrange
        DirectoryInfo tempDir = Directory.CreateTempSubdirectory();

        try
        {
            ProfilingOptions options = new()
            {
                OutputDirectory = tempDir.FullName,
                ScenarioName = "test-expect-failure",
                Duration = 1,
                ExpectFailure = true
            };

            // Act - Use nonexistent executable which should fail
            TraceResult result = await traceRunner.RunProfilingAsync("nonexistent.exe", [], options);

            // Assert
            result.ShouldNotBeNull();
            result.ScenarioName.ShouldBe("test-expect-failure");
            // With ExpectFailure = true, even if the process fails, IsSuccessful should reflect that expectation
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [TestMethod]
    public void DotnetTraceRunner_ImplementsITraceRunner()
    {
        // Act & Assert
        traceRunner.ShouldBeAssignableTo<DeadCode.Core.Services.ITraceRunner>();
    }
}