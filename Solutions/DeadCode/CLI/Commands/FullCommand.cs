using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DeadCode.CLI.Commands;

/// <summary>
/// Command to run the complete deadcode analysis pipeline
/// </summary>
[Description("Run complete analysis pipeline: extract, profile, and analyze")]
public class FullCommand : AsyncCommand<FullCommand.Settings>
{
    private readonly ExtractCommand extractCommand;
    private readonly ProfileCommand profileCommand;
    private readonly AnalyzeCommand analyzeCommand;
    private readonly ILogger<FullCommand> logger;
    private readonly IAnsiConsole console;

    public FullCommand(
        ExtractCommand extractCommand,
        ProfileCommand profileCommand,
        AnalyzeCommand analyzeCommand,
        ILogger<FullCommand> logger,
        IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(extractCommand);
        ArgumentNullException.ThrowIfNull(profileCommand);
        ArgumentNullException.ThrowIfNull(analyzeCommand);
        ArgumentNullException.ThrowIfNull(logger);
        this.extractCommand = extractCommand;
        this.profileCommand = profileCommand;
        this.analyzeCommand = analyzeCommand;
        this.logger = logger;
        this.console = console ?? AnsiConsole.Console;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("--assemblies")]
        [Description("Assembly file paths to analyze (supports wildcards)")]
        public string[] Assemblies { get; init; } = [];

        [CommandOption("--executable")]
        [Description("Path to the executable to profile")]
        public string? ExecutablePath { get; init; }

        [CommandOption("--scenarios")]
        [Description("Path to scenarios JSON file")]
        public string? ScenariosPath { get; init; }

        [CommandOption("--output")]
        [Description("Output directory for all artifacts")]
        [DefaultValue("analysis")]
        public string OutputDirectory { get; init; } = "analysis";

        [CommandOption("--min-confidence")]
        [Description("Minimum confidence level to include in report (high, medium, low)")]
        [DefaultValue("high")]
        public string MinConfidence { get; init; } = "high";

        public override ValidationResult Validate()
        {
            if (Assemblies.Length == 0)
            {
                return ValidationResult.Error("At least one assembly path must be provided");
            }

            if (string.IsNullOrWhiteSpace(ExecutablePath))
            {
                return ValidationResult.Error("Executable path is required");
            }

            if (!File.Exists(ExecutablePath))
            {
                return ValidationResult.Error($"Executable not found: {ExecutablePath}");
            }

            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        logger.LogInformation("Starting full deadcode analysis pipeline");

        // Create output directory
        Directory.CreateDirectory(settings.OutputDirectory);

        Rule rule = new($"[bold blue]DeadCode Analysis Pipeline[/]")
        {
            Style = Style.Parse("blue")
        };
        console.Write(rule);

        try
        {
            // Step 1: Extract method inventory
            console.MarkupLine("\n[bold]Step 1:[/] Extracting method inventory");

            string inventoryPath = Path.Combine(settings.OutputDirectory, "inventory.json");
            ExtractCommand.Settings extractSettings = new()
            {
                Assemblies = settings.Assemblies,
                OutputPath = inventoryPath,
                IncludeGenerated = false
            };

            int extractResult = await extractCommand.ExecuteAsync(context, extractSettings);
            if (extractResult != 0)
            {
                console.MarkupLine("[red]Failed to extract method inventory[/]");
                return extractResult;
            }

            // Step 2: Profile application
            console.MarkupLine("\n[bold]Step 2:[/] Profiling application execution");

            string tracesDirectory = Path.Combine(settings.OutputDirectory, "traces");
            ProfileCommand.Settings profileSettings = new()
            {
                ExecutablePath = settings.ExecutablePath!,
                ScenariosPath = settings.ScenariosPath,
                OutputDirectory = tracesDirectory
            };

            int profileResult = await profileCommand.ExecuteAsync(context, profileSettings);
            if (profileResult != 0)
            {
                console.MarkupLine("[yellow]Warning: Some profiling scenarios failed[/]");
            }

            // Step 3: Analyze results
            console.MarkupLine("\n[bold]Step 3:[/] Analyzing for unused code");

            string reportPath = Path.Combine(settings.OutputDirectory, "report.json");
            AnalyzeCommand.Settings analyzeSettings = new()
            {
                InventoryPath = inventoryPath,
                TracePaths = [tracesDirectory],
                OutputPath = reportPath,
                MinConfidence = settings.MinConfidence
            };

            int analyzeResult = await analyzeCommand.ExecuteAsync(context, analyzeSettings);
            if (analyzeResult != 0)
            {
                console.MarkupLine("[red]Failed to analyze redundancy[/]");
                return analyzeResult;
            }

            // Display final summary
            DisplayFinalSummary(settings.OutputDirectory);

            logger.LogInformation("Full analysis pipeline completed successfully");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete analysis pipeline");
            console.WriteException(ex);
            return 1;
        }
    }

    private void DisplayFinalSummary(string outputDirectory)
    {
        Rule rule = new($"[bold green]Analysis Complete[/]")
        {
            Style = Style.Parse("green")
        };
        console.Write(rule);

        Grid grid = new();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            new Text("Output Directory:", new Style(Color.Grey)),
            new Text(outputDirectory, new Style(Color.Blue))
        );

        grid.AddRow(
            new Text("Inventory:", new Style(Color.Grey)),
            new Text("inventory.json", new Style(Color.Blue))
        );

        grid.AddRow(
            new Text("Traces:", new Style(Color.Grey)),
            new Text("traces/", new Style(Color.Blue))
        );

        grid.AddRow(
            new Text("Report:", new Style(Color.Grey)),
            new Text("report.json", new Style(Color.Blue))
        );

        console.Write(new Panel(grid)
        {
            Header = new PanelHeader("Output Files"),
            Padding = new Padding(1)
        });

        console.MarkupLine("\n[green]✓[/] Run [blue]deadcode analyze --help[/] to customize the analysis");
        console.MarkupLine("[green]✓[/] Use the report.json with an LLM to generate cleanup tasks");
    }
}