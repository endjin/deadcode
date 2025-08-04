using System.ComponentModel;
using DeadCode.Core.Models;
using DeadCode.Core.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DeadCode.CLI.Commands;

/// <summary>
/// Command to extract method inventory from assemblies
/// </summary>
[Description("Extract method inventory from assemblies through static analysis")]
public class ExtractCommand : AsyncCommand<ExtractCommand.Settings>
{
    private readonly IMethodInventoryExtractor extractor;
    private readonly ILogger<ExtractCommand> logger;
    private readonly IAnsiConsole console;

    public ExtractCommand(IMethodInventoryExtractor extractor, ILogger<ExtractCommand> logger, IAnsiConsole? console = null)
    {
        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(logger);
        this.extractor = extractor;
        this.logger = logger;
        this.console = console ?? AnsiConsole.Console;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[ASSEMBLIES]")]
        [Description("Assembly file paths to analyze (supports wildcards)")]
        public string[] Assemblies { get; init; } = [];

        [CommandOption("-o|--output")]
        [Description("Output path for the inventory JSON file")]
        [DefaultValue("inventory.json")]
        public string OutputPath { get; init; } = "inventory.json";

        [CommandOption("--include-generated")]
        [Description("Include compiler-generated methods")]
        [DefaultValue(false)]
        public bool IncludeGenerated { get; init; }

        public override ValidationResult Validate()
        {
            if (Assemblies.Length == 0)
            {
                return ValidationResult.Error("At least one assembly path must be provided");
            }

            return ValidationResult.Success();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        logger.LogInformation("Starting method inventory extraction");

        // Resolve assembly paths from patterns
        string[] assemblyPaths = ResolveAssemblyPaths(settings.Assemblies);
        if (assemblyPaths.Length == 0)
        {
            console.MarkupLine("[red]No assembly files found matching the specified patterns[/]");
            return 1;
        }

        int result = 0;

        await console.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                ProgressTask task = ctx.AddTask("[green]Extracting methods[/]", maxValue: assemblyPaths.Length);

                try
                {
                    MethodInventory inventory = await extractor.ExtractAsync(assemblyPaths, new ExtractionOptions
                    {
                        IncludeCompilerGenerated = settings.IncludeGenerated,
                        Progress = new Progress<ExtractionProgress>(progress =>
                        {
                            task.Value = progress.ProcessedAssemblies;
                            task.Description = $"[green]Extracting methods from {progress.CurrentAssembly}[/]";
                        })
                    });

                    task.StopTask();

                    // Save inventory to JSON
                    await SaveInventoryAsync(inventory, settings.OutputPath);

                    // Display summary
                    DisplaySummary(inventory);

                    logger.LogInformation("Method inventory extraction completed successfully");
                    result = 0;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to extract method inventory");
                    console.WriteException(ex);
                    result = 1;
                }
            });

        return result;
    }

    private async Task SaveInventoryAsync(MethodInventory inventory, string outputPath)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(inventory, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });

        await File.WriteAllTextAsync(outputPath, json);

        console.MarkupLine($"[green]✓[/] Inventory saved to [blue]{outputPath}[/]");
    }

    private void DisplaySummary(MethodInventory inventory)
    {
        Table table = new();
        table.AddColumn("Metric");
        table.AddColumn("Value");

        table.AddRow("Total Methods", inventory.Count.ToString());
        table.AddRow("Assemblies", inventory.MethodsByAssembly.Count.ToString());
        table.AddRow("High Confidence", inventory.GetMethodsBySafety(SafetyClassification.HighConfidence).Count().ToString());
        table.AddRow("Medium Confidence", inventory.GetMethodsBySafety(SafetyClassification.MediumConfidence).Count().ToString());
        table.AddRow("Low Confidence", inventory.GetMethodsBySafety(SafetyClassification.LowConfidence).Count().ToString());
        table.AddRow("Do Not Remove", inventory.GetMethodsBySafety(SafetyClassification.DoNotRemove).Count().ToString());

        console.Write(table);
    }

    private string[] ResolveAssemblyPaths(string[] patterns)
    {
        List<string> resolvedPaths = [];

        foreach (string pattern in patterns)
        {
            // If it's a direct file path, add it
            if (File.Exists(pattern))
            {
                resolvedPaths.Add(Path.GetFullPath(pattern));
                continue;
            }

            // Try to resolve as a glob pattern
            string directory = Path.GetDirectoryName(pattern) ?? ".";
            string searchPattern = Path.GetFileName(pattern);

            if (Directory.Exists(directory) && !string.IsNullOrEmpty(searchPattern))
            {
                var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFullPath);
                resolvedPaths.AddRange(files);
            }
        }

        return resolvedPaths.Distinct().ToArray();
    }
}