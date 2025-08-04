using System.Diagnostics;
using System.Text;

using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.Profiling;

/// <summary>
/// Runs profiling using dotnet-trace
/// </summary>
public class DotnetTraceRunner : ITraceRunner
{
    private readonly ILogger<DotnetTraceRunner> logger;

    public DotnetTraceRunner(ILogger<DotnetTraceRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public async Task<TraceResult> RunProfilingAsync(string executablePath, string[] arguments, ProfilingOptions options)
    {
        logger.LogInformation(
            "Running profiling on {Executable} with scenario {ScenarioName}",
            executablePath,
            options.ScenarioName);

        // Ensure output directory exists
        Directory.CreateDirectory(options.OutputDirectory);

        string traceFilePath = Path.Combine(options.OutputDirectory, $"trace-{options.ScenarioName}.nettrace");
        DateTime startTime = DateTime.UtcNow;

        try
        {
            // Build dotnet-trace arguments
            List<string> traceArgs = BuildTraceArguments(traceFilePath, executablePath, arguments, options);

            logger.LogDebug("Starting dotnet-trace with arguments: {Args}", string.Join(" ", traceArgs));

            ProcessStartInfo processInfo = new()
            {
                FileName = "dotnet-trace",
                Arguments = string.Join(" ", traceArgs),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = new() { StartInfo = processInfo };

            StringBuilder outputBuilder = new();
            StringBuilder errorBuilder = new();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                    logger.LogTrace("dotnet-trace output: {Output}", e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                    logger.LogWarning("dotnet-trace error: {Error}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            TimeSpan timeout = options.Duration.HasValue
                ? TimeSpan.FromSeconds(options.Duration.Value + 30) // Add buffer time
                : TimeSpan.FromMinutes(10); // Default max timeout

            using CancellationTokenSource cts = new(timeout);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Process timed out after {Timeout}", timeout);
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(); // Wait for the kill to complete
            }

            DateTime endTime = DateTime.UtcNow;
            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();

            // Determine if the trace was successful
            bool isSuccessful = process.ExitCode == 0 || (options.ExpectFailure && process.ExitCode != 0);
            bool traceFileExists = File.Exists(traceFilePath);

            if (!isSuccessful)
            {
                logger.LogError("dotnet-trace failed with exit code {ExitCode}. Error: {Error}",
                    process.ExitCode, error);
            }

            if (!traceFileExists)
            {
                logger.LogWarning("Trace file was not created: {TraceFilePath}", traceFilePath);
            }
            else
            {
                FileInfo fileInfo = new(traceFilePath);
                logger.LogInformation(
                    "Trace file created: {TraceFilePath} ({Size} bytes)",
                    traceFilePath, fileInfo.Length);
            }

            return new TraceResult(
                TraceFilePath: traceFilePath,
                ScenarioName: options.ScenarioName,
                StartTime: startTime,
                EndTime: endTime,
                IsSuccessful: isSuccessful,
                ErrorMessage: isSuccessful ? null : error?.Trim()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run dotnet-trace for scenario {ScenarioName}", options.ScenarioName);

            return new TraceResult(
                TraceFilePath: traceFilePath,
                ScenarioName: options.ScenarioName,
                StartTime: startTime,
                EndTime: DateTime.UtcNow,
                IsSuccessful: false,
                ErrorMessage: ex.Message
            );
        }
    }

    private static List<string> BuildTraceArguments(string traceFilePath, string executablePath, string[] arguments, ProfilingOptions options)
    {
        List<string> args =
        [
            "collect",
            // Use JIT compilation event providers for deterministic method tracking
            // This captures ALL executed methods as they are JIT-compiled
            "--providers", "Microsoft-Windows-DotNETRuntime:0x4C14FCCBD:5",
            "--buffersize", "512", // Larger buffer for method-heavy applications
            "--output", $"\"{traceFilePath}\"", // Quote to handle spaces
            "--"
        ];

        // Check if the executable is a .dll file
        if (executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            // For .dll files, we need to use 'dotnet' to run them
            args.Add("dotnet");
            args.Add($"\"{executablePath}\""); // Quote DLL path
        }
        else
        {
            // For .exe files or other executables, run directly
            args.Add($"\"{executablePath}\""); // Quote executable path
        }

        args.AddRange(arguments.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));

        return args;
    }
}