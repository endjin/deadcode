using DeadCode.CLI.Commands;
using DeadCode.Core.Services;
using DeadCode.Infrastructure.IO;
using DeadCode.Infrastructure.Profiling;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Spectre.Console;
using Spectre.Console.Cli;

ServiceCollection services = new();

// Configure logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Register core services
services.AddSingleton<IDependencyVerifier, DotnetTraceVerifier>();
services.AddSingleton<IMethodInventoryExtractor, ReflectionMethodExtractor>();
services.AddSingleton<ITraceRunner, DotnetTraceRunner>();
services.AddSingleton<ITraceParser, TraceParser>();
services.AddSingleton<IComparisonEngine, ComparisonEngine>();
services.AddSingleton<IReportGenerator, JsonReportGenerator>();
services.AddSingleton<ISafetyClassifier, RuleBasedSafetyClassifier>();
services.AddSingleton<IPdbReader, PdbReader>();

// Register Spectre.Console
services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

// Register commands
services.AddSingleton<ExtractCommand>();
services.AddSingleton<ProfileCommand>();
services.AddSingleton<AnalyzeCommand>();
services.AddSingleton<FullCommand>();

// Build service provider
ServiceProvider serviceProvider = services.BuildServiceProvider();

// Create type registrar for Spectre.Console.Cli
TypeRegistrar registrar = new(serviceProvider);

// Create and configure the CLI app
CommandApp app = new(registrar);
app.Configure(config =>
{
    config.SetApplicationName("deadcode");
    config.SetApplicationVersion("1.0.0");

    // Add commands
    config.AddCommand<ExtractCommand>("extract")
        .WithDescription("Extract method inventory from assemblies")
        .WithExample("extract", "bin/Release/net9.0/*.dll", "-o", "inventory.json");

    config.AddCommand<ProfileCommand>("profile")
        .WithDescription("Profile application execution")
        .WithExample("profile", "MyApp.exe", "--scenarios", "scenarios.json", "-o", "traces/");

    config.AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Analyze method usage")
        .WithExample("analyze", "-i", "inventory.json", "-t", "traces/", "-o", "report.json");

    config.AddCommand<FullCommand>("full")
        .WithDescription("Run complete analysis pipeline")
        .WithExample("full", "--assemblies", "bin/Release/net9.0/*.dll", "--executable", "MyApp.exe");

    // Configure help
    config.ValidateExamples();
});

// Run the app
return await app.RunAsync(args);

// Type registrar implementation for DI
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider serviceProvider;

    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this.serviceProvider = serviceProvider;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(serviceProvider);
    }

    public void Register(Type service, Type implementation)
    {
        // Not needed for our use case
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not needed for our use case
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not needed for our use case
    }
}

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider serviceProvider;

    public TypeResolver(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        this.serviceProvider = serviceProvider;
    }

    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return serviceProvider.GetService(type);
    }
}