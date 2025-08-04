using System.Diagnostics;

using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace DeadCode.Infrastructure.Profiling;

/// <summary>
/// Verifies and installs dotnet-trace dependency
/// </summary>
public class DotnetTraceVerifier : IDependencyVerifier
{
    private readonly ILogger<DotnetTraceVerifier> logger;

    public DotnetTraceVerifier(ILogger<DotnetTraceVerifier> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public async Task<bool> CheckDependenciesAsync()
    {
        try
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool list --global",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            bool isInstalled = output.Contains("dotnet-trace");

            if (isInstalled)
            {
                logger.LogInformation("dotnet-trace is installed");
            }
            else
            {
                logger.LogWarning("dotnet-trace is not installed");
            }

            return isInstalled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check for dotnet-trace");
            return false;
        }
    }

    public async Task<bool> InstallMissingDependenciesAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[yellow]Installing dotnet-trace...[/]");

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "tool install --global dotnet-trace",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]✓ dotnet-trace installed successfully[/]");
                logger.LogInformation("dotnet-trace installed successfully");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to install dotnet-trace: {error}[/]");
                logger.LogError("Failed to install dotnet-trace: {Error}", error);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install dotnet-trace");
            return false;
        }
    }
}