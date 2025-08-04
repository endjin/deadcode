using System.ComponentModel;
using System.Text.Json;
using DeadCode.Core.Models;
using DeadCode.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DeadCode.CLI.Commands;

/// <summary>
/// Command to analyze method inventory against trace data
/// </summary>
[Description("Analyze method inventory against trace data to find unused code")]
public class AnalyzeCommand : AsyncCommand<AnalyzeCommand.Settings>
{
    private readonly IComparisonEngine comparisonEngine;
    private readonly ITraceParser traceParser;
    private readonly IReportGenerator reportGenerator;
    private readonly ILogger<AnalyzeCommand> logger;
    private readonly IAnsiConsole console;

    public AnalyzeCommand(
        IComparisonEngine comparisonEngine,
        ITraceParser traceParser,
        IReportGenerator reportGenerator,
        ILogger<AnalyzeCommand> logger,
        IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(comparisonEngine);
        ArgumentNullException.ThrowIfNull(traceParser);
        ArgumentNullException.ThrowIfNull(reportGenerator);
        ArgumentNullException.ThrowIfNull(logger);
        this.comparisonEngine = comparisonEngine;
        this.traceParser = traceParser;
        this.reportGenerator = reportGenerator;
        this.logger = logger;
        this.console = console ?? AnsiConsole.Console;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-i|--inventory")]
        [Description("Path to the method inventory JSON file")]
        [DefaultValue("inventory.json")]
        public string InventoryPath { get; init; } = "inventory.json";

        [CommandOption("-t|--traces")]
        [Description("Directory containing trace files or specific trace file paths")]
        public string[] TracePaths { get; init; } = [];

        [CommandOption("-o|--output")]
        [Description("Output path for the redundancy report")]
        [DefaultValue("report.json")]
        public string OutputPath { get; init; } = "report.json";

        [CommandOption("--min-confidence")]
        [Description("Minimum confidence level to include in report (high, medium, low)")]
        [DefaultValue("high")]
        public string MinConfidence { get; init; } = "high";

        public override ValidationResult Validate()
        {
            if (!File.Exists(InventoryPath))
            {
                return ValidationResult.Error($"Inventory file not found: {InventoryPath}");
            }

            if (TracePaths.Length == 0)
            {
                return ValidationResult.Error("At least one trace file or directory must be specified");
            }

            string[] validConfidenceLevels = new[] { "high", "medium", "low" };
            if (!validConfidenceLevels.Contains(MinConfidence.ToLower()))
            {
                return ValidationResult.Error("Min confidence must be one of: high, medium, low");
            }

            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        logger.LogInformation("Starting redundancy analysis");

        try
        {
            // Load method inventory
            console.Status()
                .Start("Loading method inventory...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("green"));
                });

            MethodInventory inventory = await LoadInventoryAsync(settings.InventoryPath);
            console.MarkupLine($"[green]✓[/] Loaded [blue]{inventory.Count}[/] methods from inventory");

            // Find and parse trace files
            List<string> traceFiles = GetTraceFiles(settings.TracePaths);
            if (traceFiles.Count == 0)
            {
                console.MarkupLine("[red]No trace files found![/]");
                return 1;
            }

            console.MarkupLine($"[green]✓[/] Found [blue]{traceFiles.Count}[/] trace files");

            // Parse traces and extract executed methods
            HashSet<string> executedMethods = [];

            await console.Progress()
                .AutoClear(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    ProgressTask task = ctx.AddTask("[green]Parsing trace files[/]", maxValue: traceFiles.Count);

                    foreach (string traceFile in traceFiles)
                    {
                        task.Description = $"[green]Parsing {Path.GetFileName(traceFile)}[/]";

                        HashSet<string> methods = await traceParser.ParseTraceAsync(traceFile);
                        executedMethods.UnionWith(methods);

                        task.Increment(1);
                    }

                    task.StopTask();
                });

            console.MarkupLine($"[green]✓[/] Found [blue]{executedMethods.Count}[/] unique executed methods");

            // Compare and generate report
            RedundancyReport report = null!;

            await console.Status()
                .StartAsync("Analyzing unused methods...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    ctx.SpinnerStyle(Style.Parse("yellow"));

                    report = await comparisonEngine.CompareAsync(inventory, executedMethods);
                });

            // Filter by confidence level
            SafetyClassification minConfidenceLevel = ParseConfidenceLevel(settings.MinConfidence);
            report = FilterByConfidence(report, minConfidenceLevel);

            // Generate output
            await reportGenerator.GenerateAsync(report, settings.OutputPath);

            // Display summary
            DisplaySummary(report, settings.OutputPath);

            logger.LogInformation("Redundancy analysis completed successfully");

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze redundancy");
            console.WriteException(ex);
            return 1;
        }
    }

    private async Task<MethodInventory> LoadInventoryAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        return JsonSerializer.Deserialize<MethodInventory>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize inventory");
    }

    private List<string> GetTraceFiles(string[] paths)
    {
        List<string> traceFiles = [];

        foreach (string path in paths)
        {
            string ext = Path.GetExtension(path);
            if (File.Exists(path) && (ext == ".nettrace" || ext == ".txt"))
            {
                traceFiles.Add(path);
            }
            else if (Directory.Exists(path))
            {
                traceFiles.AddRange(Directory.GetFiles(path, "*.nettrace", SearchOption.AllDirectories));
                traceFiles.AddRange(Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories));
            }
        }

        return traceFiles;
    }

    private SafetyClassification ParseConfidenceLevel(string confidence)
    {
        return confidence.ToLower() switch
        {
            "high" => SafetyClassification.HighConfidence,
            "medium" => SafetyClassification.MediumConfidence,
            "low" => SafetyClassification.LowConfidence,
            _ => SafetyClassification.HighConfidence
        };
    }

    private RedundancyReport FilterByConfidence(RedundancyReport report, SafetyClassification minLevel)
    {
        RedundancyReport filtered = new()
        {
            AnalyzedAssemblies = report.AnalyzedAssemblies ?? [],
            TraceScenarios = report.TraceScenarios ?? []
        };

        IEnumerable<UnusedMethod> methods = minLevel switch
        {
            SafetyClassification.HighConfidence => report.HighConfidenceMethods,
            SafetyClassification.MediumConfidence => [..report.HighConfidenceMethods, ..report.MediumConfidenceMethods],
            SafetyClassification.LowConfidence => report.UnusedMethods.AsEnumerable().Where(m => m.Method.SafetyLevel != SafetyClassification.DoNotRemove),
            _ => report.HighConfidenceMethods
        };

        filtered.AddUnusedMethods(methods);

        return filtered;
    }

    private void DisplaySummary(RedundancyReport report, string outputPath)
    {
        ReportStatistics stats = report.GetStatistics();

        Table table = new();
        table.AddColumn("Category");
        table.AddColumn("Count");
        table.AddColumn("Action");

        table.AddRow(
            "[green]High Confidence[/]",
            stats.HighConfidence.ToString(),
            "Safe to remove"
        );

        table.AddRow(
            "[yellow]Medium Confidence[/]",
            stats.MediumConfidence.ToString(),
            "Review carefully"
        );

        table.AddRow(
            "[orange3]Low Confidence[/]",
            stats.LowConfidence.ToString(),
            "Likely false positives"
        );

        table.AddRow(
            "[red]Do Not Remove[/]",
            stats.DoNotRemove.ToString(),
            "Framework/Security code"
        );

        console.Write(new Panel(table)
        {
            Header = new PanelHeader("Redundancy Analysis Summary"),
            Padding = new Padding(1),
            BorderStyle = new Style(Color.Blue)
        });

        console.MarkupLine($"\n[green]✓[/] Report saved to [blue]{outputPath}[/]");
    }
}