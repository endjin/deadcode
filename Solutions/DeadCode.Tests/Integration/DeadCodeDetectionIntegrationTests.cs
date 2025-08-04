using DeadCode.Core.Models;
using DeadCode.Core.Services;
using DeadCode.Infrastructure.IO;
using DeadCode.Infrastructure.Profiling;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Spectre.Console.Testing;

namespace DeadCode.Tests.Integration;

[TestClass]
public class DeadCodeDetectionIntegrationTests : IDisposable
{
    private const string SampleAppPath = "Samples/SampleAppWithDeadCode/bin/Debug/net9.0/SampleAppWithDeadCode.dll";
    private readonly string testOutputDir;
    private readonly IServiceProvider serviceProvider;
    private readonly TestConsole console;

    public DeadCodeDetectionIntegrationTests()
    {
        testOutputDir = Path.Combine(Path.GetTempPath(), $"deadcode-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(testOutputDir);

        console = new TestConsole();

        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IMethodInventoryExtractor, ReflectionMethodExtractor>();
        services.AddSingleton<ISafetyClassifier, RuleBasedSafetyClassifier>();
        services.AddSingleton<IPdbReader, PdbReader>();
        services.AddSingleton<ITraceParser, TraceParser>();
        services.AddSingleton<IComparisonEngine, ComparisonEngine>();
        services.AddSingleton<IReportGenerator, JsonReportGenerator>();

        serviceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(testOutputDir))
        {
            Directory.Delete(testOutputDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task DefaultExecution_IdentifiesCorrectDeadCode()
    {
        // Arrange
        string traceFile = CreateTraceFile("default", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunDefault()",
            "SampleAppWithDeadCode.Services.Calculator..ctor()",
            "SampleAppWithDeadCode.Services.Calculator.Add(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Utilities.StringHelper.Reverse(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor..ctor()",
            "SampleAppWithDeadCode.Services.DataProcessor.ProcessData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.ValidateData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.NormalizeData(System.String)"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // These Calculator methods should be dead code
        AssertMethodIsUnused(report, "Calculator", "Subtract");
        AssertMethodIsUnused(report, "Calculator", "Multiply");
        AssertMethodIsUnused(report, "Calculator", "Divide");
        AssertMethodIsUnused(report, "Calculator", "CalculateSquareRoot");
        AssertMethodIsUnused(report, "Calculator", "CalculateLogarithm");

        // These DataProcessor methods should be dead code
        AssertMethodIsUnused(report, "DataProcessor", "ClearData");
        AssertMethodIsUnused(report, "DataProcessor", "GetProcessedData");
        AssertMethodIsUnused(report, "DataProcessor", "LogData");
        AssertMethodIsUnused(report, "DataProcessor", "ProcessBatchAsync");
        AssertMethodIsUnused(report, "DataProcessor", "OnDataProcessed");

        // These StringHelper methods should be dead code
        AssertMethodIsUnused(report, "StringHelper", "ToCamelCase");
        AssertMethodIsUnused(report, "StringHelper", "IsPalindrome");
        AssertMethodIsUnused(report, "StringHelper", "Truncate");
        AssertMethodIsUnused(report, "StringHelper", "JoinWithSeparator");

        // Verify that used methods are NOT in the unused list
        AssertMethodIsUsed(report, "Calculator", "Add");
        AssertMethodIsUsed(report, "StringHelper", "Reverse");
        AssertMethodIsUsed(report, "DataProcessor", "ProcessData");
    }

    [TestMethod]
    public async Task HelpCommand_OnlyUsesHelpMethods()
    {
        // Arrange
        string traceFile = CreateTraceFile("help", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.ShowHelp()"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // Almost everything should be dead code except Main and ShowHelp
        AssertMethodIsUnused(report, "Program", "RunDefault");
        AssertMethodIsUnused(report, "Program", "RunWithVerbose");
        AssertMethodIsUnused(report, "Program", "RunCalculatorOnly");
        AssertMethodIsUnused(report, "Program", "RunDataProcessing");
        AssertMethodIsUnused(report, "Program", "RunStressTest");

        // All Calculator methods should be dead
        AssertMethodIsUnused(report, "Calculator", "Add");
        AssertMethodIsUnused(report, "Calculator", "Subtract");
        AssertMethodIsUnused(report, "Calculator", "Multiply");

        // Verify ShowHelp is used
        AssertMethodIsUsed(report, "Program", "ShowHelp");
    }

    [TestMethod]
    public async Task VerboseMode_UsesAdditionalCalculatorMethods()
    {
        // Arrange
        string traceFile = CreateTraceFile("verbose", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunWithVerbose()",
            "SampleAppWithDeadCode.Program.RunDefault()",
            "SampleAppWithDeadCode.Services.Calculator..ctor()",
            "SampleAppWithDeadCode.Services.Calculator.Add(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Services.Calculator.Subtract(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Services.Calculator.Multiply(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Utilities.StringHelper.Reverse(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor..ctor()",
            "SampleAppWithDeadCode.Services.DataProcessor.ProcessData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.ValidateData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.NormalizeData(System.String)"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // Divide, CalculateSquareRoot and CalculateLogarithm should still be dead
        AssertMethodIsUnused(report, "Calculator", "Divide");
        AssertMethodIsUnused(report, "Calculator", "CalculateSquareRoot");
        AssertMethodIsUnused(report, "Calculator", "CalculateLogarithm");

        // But Add, Subtract, and Multiply should be used
        AssertMethodIsUsed(report, "Calculator", "Add");
        AssertMethodIsUsed(report, "Calculator", "Subtract");
        AssertMethodIsUsed(report, "Calculator", "Multiply");
    }

    [TestMethod]
    public async Task CalculatorOnly_UsesAllPublicCalculatorMethods()
    {
        // Arrange
        string traceFile = CreateTraceFile("calculator", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunCalculatorOnly()",
            "SampleAppWithDeadCode.Services.Calculator..ctor()",
            "SampleAppWithDeadCode.Services.Calculator.Add(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Services.Calculator.Subtract(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Services.Calculator.Multiply(System.Int32, System.Int32)",
            "SampleAppWithDeadCode.Services.Calculator.Divide(System.Double, System.Double)"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // Private/protected methods should still be dead
        AssertMethodIsUnused(report, "Calculator", "CalculateSquareRoot");
        AssertMethodIsUnused(report, "Calculator", "CalculateLogarithm");

        // All public methods should be used
        AssertMethodIsUsed(report, "Calculator", "Add");
        AssertMethodIsUsed(report, "Calculator", "Subtract");
        AssertMethodIsUsed(report, "Calculator", "Multiply");
        AssertMethodIsUsed(report, "Calculator", "Divide");

        // DataProcessor methods should all be dead
        AssertMethodIsUnused(report, "DataProcessor", "ProcessData");
        AssertMethodIsUnused(report, "DataProcessor", "GetProcessedData");
    }

    [TestMethod]
    public async Task DataProcessing_UsesGetProcessedData()
    {
        // Arrange
        string traceFile = CreateTraceFile("dataprocessing", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunDataProcessing(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor..ctor()",
            "SampleAppWithDeadCode.Services.DataProcessor.ProcessData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.ValidateData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.NormalizeData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.GetProcessedData()"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // GetProcessedData should now be used
        AssertMethodIsUsed(report, "DataProcessor", "GetProcessedData");

        // But these should still be dead
        AssertMethodIsUnused(report, "DataProcessor", "ClearData");
        AssertMethodIsUnused(report, "DataProcessor", "LogData");
        AssertMethodIsUnused(report, "DataProcessor", "ProcessBatchAsync");
    }

    [TestMethod]
    public async Task StressTest_UsesAsyncAndStringMethods()
    {
        // Arrange
        string traceFile = CreateTraceFile("stress", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunStressTest(System.Int32)",
            "SampleAppWithDeadCode.Services.DataProcessor..ctor()",
            "SampleAppWithDeadCode.Services.DataProcessor.ProcessBatchAsync(System.Collections.Generic.IEnumerable<System.String>)",
            "SampleAppWithDeadCode.Services.DataProcessor.ProcessData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.ValidateData(System.String)",
            "SampleAppWithDeadCode.Services.DataProcessor.NormalizeData(System.String)",
            "SampleAppWithDeadCode.Utilities.StringHelper.IsPalindrome(System.String)",
            "SampleAppWithDeadCode.Utilities.StringHelper.Reverse(System.String)"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // ProcessBatchAsync and IsPalindrome should now be used
        AssertMethodIsUsed(report, "DataProcessor", "ProcessBatchAsync");
        AssertMethodIsUsed(report, "StringHelper", "IsPalindrome");

        // But these should still be dead
        AssertMethodIsUnused(report, "StringHelper", "ToCamelCase");
        AssertMethodIsUnused(report, "StringHelper", "Truncate");
        AssertMethodIsUnused(report, "StringHelper", "JoinWithSeparator");
    }

    [TestMethod]
    public async Task UnusedModels_AllMethodsAreDead()
    {
        // Arrange - Use the default trace since UnusedModels are never used
        string traceFile = CreateTraceFile("unused-models", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])",
            "SampleAppWithDeadCode.Program.RunDefault()"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // All methods in UnusedModels should be dead
        AssertMethodIsUnused(report, "Customer", "GetAge");
        AssertMethodIsUnused(report, "Customer", "IsValidEmail");
        AssertMethodIsUnused(report, "BaseEntity", "Save");
        AssertMethodIsUnused(report, "ConfigurationManager", "GetSetting");
        AssertMethodIsUnused(report, "ConfigurationManager", "get_Instance");
    }

    [TestMethod]
    public async Task SafetyClassification_CorrectlyClassifiesMethods()
    {
        // Arrange
        string traceFile = CreateTraceFile("safety", new[]
        {
            "SampleAppWithDeadCode.Program.Main(System.String[])"
        });

        // Act
        RedundancyReport report = await RunAnalysis(traceFile);

        // Assert
        report.ShouldNotBeNull();

        // Private methods should be high confidence
        UnusedMethod? privateMethod = report.UnusedMethods.FirstOrDefault(m =>
            m.Method.MethodName.Contains("CalculateSquareRoot"));
        privateMethod.ShouldNotBeNull();
        privateMethod.Method.SafetyLevel.ShouldBe(SafetyClassification.HighConfidence);

        // Public methods should be lower confidence
        UnusedMethod? publicMethod = report.UnusedMethods.FirstOrDefault(m =>
            m.Method.TypeName?.Contains("Calculator") == true &&
            m.Method.MethodName.Contains("Add"));
        publicMethod.ShouldNotBeNull();
        publicMethod.Method.SafetyLevel.ShouldNotBe(SafetyClassification.HighConfidence);
    }

    private string CreateTraceFile(string scenario, string[] methodCalls)
    {
        string traceDir = Path.Combine(testOutputDir, "traces");
        Directory.CreateDirectory(traceDir);

        string traceFile = Path.Combine(traceDir, $"trace-{scenario}.txt");
        string content = string.Join(Environment.NewLine,
            methodCalls.Select(m => $"Method Enter: {m}"));

        File.WriteAllText(traceFile, content);
        return traceFile;
    }

    private async Task<RedundancyReport> RunAnalysis(string traceFile)
    {
        // First, extract method inventory from the sample app
        IMethodInventoryExtractor extractor = serviceProvider.GetRequiredService<IMethodInventoryExtractor>();

        // Build the sample app if needed
        await EnsureSampleAppBuilt();

        // Navigate from test assembly location to project root
        string testAssemblyDir = Path.GetDirectoryName(typeof(DeadCodeDetectionIntegrationTests).Assembly.Location)!;
        string projectRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "../../../../"));
        string assemblyPath = Path.Combine(projectRoot, SampleAppPath);

        ExtractionOptions extractOptions = new()
        {
            IncludeCompilerGenerated = false
        };

        MethodInventory inventory = await extractor.ExtractAsync([assemblyPath], extractOptions);

        // Parse the trace file
        ITraceParser parser = serviceProvider.GetRequiredService<ITraceParser>();
        HashSet<string> executedMethods = await parser.ParseTraceAsync(traceFile);

        // Compare and generate report
        IComparisonEngine comparisonEngine = serviceProvider.GetRequiredService<IComparisonEngine>();
        RedundancyReport report = await comparisonEngine.CompareAsync(inventory, executedMethods);

        return report;
    }

    private async Task EnsureSampleAppBuilt()
    {
        // Navigate from test assembly location to project root
        string testAssemblyDir = Path.GetDirectoryName(typeof(DeadCodeDetectionIntegrationTests).Assembly.Location)!;
        string projectRoot = Path.GetFullPath(Path.Combine(testAssemblyDir, "../../../../"));
        string projectPath = Path.Combine(projectRoot, "Samples/SampleAppWithDeadCode/SampleAppWithDeadCode.csproj");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Sample project not found at: {projectPath}");
        }

        // Check if the dll exists
        string dllPath = Path.Combine(projectRoot, SampleAppPath);

        if (!File.Exists(dllPath))
        {
            // Build the project
            System.Diagnostics.ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\" -c Debug",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
            await process!.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Failed to build sample app");
            }
        }
    }

    private void AssertMethodIsUnused(RedundancyReport report, string className, string methodName)
    {
        bool unused = report.UnusedMethods.Any(m =>
            m.Method.TypeName?.Contains(className) == true &&
            m.Method.MethodName.Contains(methodName));

        unused.ShouldBeTrue($"Expected {className}.{methodName} to be unused, but it was not found in unused methods");
    }

    private void AssertMethodIsUsed(RedundancyReport report, string className, string methodName)
    {
        bool unused = report.UnusedMethods.Any(m =>
            m.Method.TypeName?.Contains(className) == true &&
            m.Method.MethodName.Contains(methodName));

        unused.ShouldBeFalse($"Expected {className}.{methodName} to be used, but it was found in unused methods");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}