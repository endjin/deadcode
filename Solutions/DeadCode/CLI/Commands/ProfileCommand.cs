using System.ComponentModel;
using DeadCode.Core.Models;
using DeadCode.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DeadCode.CLI.Commands;

/// <summary>
/// Command to profile application execution and collect trace data
/// </summary>
[Description("Profile application execution to collect runtime trace data")]
public class ProfileCommand : AsyncCommand<ProfileCommand.Settings>
{
    private readonly ITraceRunner traceRunner;
    private readonly IDependencyVerifier dependencyVerifier;
    private readonly ILogger<ProfileCommand> logger;
    private readonly IAnsiConsole console;

    public ProfileCommand(
        ITraceRunner traceRunner,
        IDependencyVerifier dependencyVerifier,
        ILogger<ProfileCommand> logger,
        IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(traceRunner);
        ArgumentNullException.ThrowIfNull(dependencyVerifier);
        ArgumentNullException.ThrowIfNull(logger);
        this.traceRunner = traceRunner;
        this.dependencyVerifier = dependencyVerifier;
        this.logger = logger;
        this.console = console ?? AnsiConsole.Console;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<EXECUTABLE>")]
        [Description("Path to the executable to profile")]
        public string ExecutablePath { get; init; } = string.Empty;

        [CommandOption("--scenarios")]
        [Description("Path to scenarios JSON file")]
        public string? ScenariosPath { get; init; }

        [CommandOption("--args")]
        [Description("Arguments to pass to the executable")]
        public string[]? Arguments { get; init; }

        [CommandOption("-o|--output")]
        [Description("Output directory for trace files")]
        [DefaultValue("traces")]
        public string OutputDirectory { get; init; } = "traces";

        [CommandOption("--duration")]
        [Description("Duration to run the profiling in seconds (default: run to completion)")]
        public int? Duration { get; init; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(ExecutablePath))
            {
                return ValidationResult.Error("Executable path is required");
            }

            if (!File.Exists(ExecutablePath))
            {
                return ValidationResult.Error($"Executable not found: {ExecutablePath}");
            }

            if (ScenariosPath != null && !File.Exists(ScenariosPath))
            {
                return ValidationResult.Error($"Scenarios file not found: {ScenariosPath}");
            }

            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        logger.LogInformation("Starting profiling session");

        // Verify dependencies
        if (!await VerifyDependenciesAsync())
        {
            return 1;
        }

        // Load scenarios or create default
        List<ProfilingScenario> scenarios = await LoadScenariosAsync(settings);

        List<TraceResult> results = [];

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async ctx =>
            {
                ProgressTask task = ctx.AddTask("[green]Running profiling scenarios[/]", maxValue: scenarios.Count);

                foreach (ProfilingScenario scenario in scenarios)
                {
                    task.Description = $"[green]Profiling: {scenario.Name}[/]";

                    try
                    {
                        TraceResult result = await traceRunner.RunProfilingAsync(
                            settings.ExecutablePath,
                            scenario.Arguments,
                            new ProfilingOptions
                            {
                                OutputDirectory = settings.OutputDirectory,
                                ScenarioName = scenario.Name,
                                Duration = scenario.Duration ?? settings.Duration,
                                ExpectFailure = scenario.ExpectFailure
                            });

                        results.Add(result);

                        if (result.IsSuccessful)
                        {
                            console.MarkupLine($"[green]✓[/] Scenario [blue]{scenario.Name}[/] completed");
                        }
                        else
                        {
                            console.MarkupLine($"[yellow]![/] Scenario [blue]{scenario.Name}[/] failed: {result.ErrorMessage?.EscapeMarkup()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to run scenario {ScenarioName}", scenario.Name);
                        console.MarkupLine($"[red]✗[/] Scenario [blue]{scenario.Name}[/] error: {ex.Message}");

                        // Add a failed result for the exception case
                        results.Add(new TraceResult(
                            TraceFilePath: string.Empty,
                            ScenarioName: scenario.Name,
                            StartTime: DateTime.UtcNow,
                            EndTime: DateTime.UtcNow,
                            IsSuccessful: false,
                            ErrorMessage: ex.Message
                        ));
                    }

                    task.Increment(1);
                }

                task.StopTask();
            });

        // Display summary
        DisplaySummary(results);

        logger.LogInformation("Profiling session completed");

        return results.All(r => r.IsSuccessful || r.TraceFileExists) ? 0 : 1;
    }

    private async Task<bool> VerifyDependenciesAsync()
    {
        if (!await dependencyVerifier.CheckDependenciesAsync())
        {
            console.MarkupLine("[yellow]dotnet-trace is not installed.[/]");

            if (console.Confirm("Would you like to install it now?"))
            {
                return await dependencyVerifier.InstallMissingDependenciesAsync();
            }

            console.MarkupLine("[red]Cannot proceed without dotnet-trace.[/]");
            return false;
        }

        return true;
    }

    private async Task<List<ProfilingScenario>> LoadScenariosAsync(Settings settings)
    {
        if (settings.ScenariosPath != null)
        {
            string json = await File.ReadAllTextAsync(settings.ScenariosPath);
            System.Text.Json.JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            ScenariosConfiguration? scenariosConfig = System.Text.Json.JsonSerializer.Deserialize<ScenariosConfiguration>(json, options);
            return scenariosConfig?.Scenarios ?? [];
        }

        // Create default scenario
        return
        [
            new ProfilingScenario
            {
                Name = "default",
                Arguments = settings.Arguments ?? [],
                Duration = settings.Duration,
                Description = "Default profiling scenario"
            }
        ];
    }

    private void DisplaySummary(List<TraceResult> results)
    {
        Table table = new();
        table.AddColumn("Scenario");
        table.AddColumn("Duration");
        table.AddColumn("Status");
        table.AddColumn("Trace File");

        foreach (TraceResult result in results)
        {
            string status = result.IsSuccessful ? "[green]Success[/]" : "[red]Failed[/]";
            string fileName = Path.GetFileName(result.TraceFilePath);

            table.AddRow(
                result.ScenarioName,
                result.Duration.ToString(@"mm\:ss"),
                status,
                result.TraceFileExists ? $"[blue]{fileName}[/]" : "[grey]N/A[/]"
            );
        }

        console.Write(table);
    }
}

/// <summary>
/// Configuration for profiling scenarios
/// </summary>
public class ScenariosConfiguration
{
    public List<ProfilingScenario> Scenarios { get; init; } = [];
}

/// <summary>
/// A profiling scenario
/// </summary>
public class ProfilingScenario
{
    public required string Name { get; init; }
    public string[] Arguments { get; init; } = [];
    public int? Duration { get; init; }
    public string? Description { get; init; }
    public bool ExpectFailure { get; init; }
}