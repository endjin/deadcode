using DeadCode.Infrastructure.Profiling;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Profiling;

[TestClass]
public class TraceParserTests
{
    private TraceParser parser = null!;
    private ILogger<TraceParser> logger = null!;
    private string testTraceFile = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<TraceParser>>();
        parser = new TraceParser(logger);
        testTraceFile = Path.Combine(Path.GetTempPath(), $"test_trace_{Guid.NewGuid()}.txt");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(testTraceFile))
        {
            File.Delete(testTraceFile);
        }
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithEmptyFile_ReturnsEmptySet()
    {
        // Arrange
        await File.WriteAllTextAsync(testTraceFile, string.Empty);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithValidMethodCalls_ExtractsMethodNames()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.235): Method Exit: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.236): Method Enter: Assembly.Type.Method2(System.String)
Process(1234).Thread(5678)/(1.237): Method Exit: Assembly.Type.Method2(System.String)
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Assembly.Type.Method1");
        result.ShouldContain("Assembly.Type.Method2");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithDuplicateMethods_ReturnsUniqueSet()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.235): Method Exit: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.236): Method Enter: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.237): Method Exit: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.238): Method Enter: Assembly.Type.Method1()
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain("Assembly.Type.Method1");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithNestedNamespaces_HandlesCorrectly()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Company.Product.Module.Type.Method()
Process(1234).Thread(5678)/(1.235): Method Enter: System.Collections.Generic.List`1.Add(T)
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Company.Product.Module.Type.Method");
        result.ShouldContain("System.Collections.Generic.List`1.Add");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithGenericMethods_HandlesCorrectly()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type.GenericMethod`1(System.String)
Process(1234).Thread(5678)/(1.235): Method Enter: Assembly.OtherType.GenericMethod`2(System.String, System.Int32)
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Assembly.Type.GenericMethod`1");
        result.ShouldContain("Assembly.OtherType.GenericMethod`2");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithConstructors_HandlesCorrectly()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type..ctor()
Process(1234).Thread(5678)/(1.235): Method Enter: Assembly.Type..ctor(System.String)
Process(1234).Thread(5678)/(1.236): Method Enter: Assembly.Type..cctor()
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(2); // .cctor has only two parts after removing params
        result.ShouldContain("Assembly.Type..ctor");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithPropertyAccessors_HandlesCorrectly()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type.get_Property()
Process(1234).Thread(5678)/(1.235): Method Enter: Assembly.Type.set_Property(System.String)
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain("Assembly.Type.get_Property");
        result.ShouldContain("Assembly.Type.set_Property");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithNonMethodLines_IgnoresThem()
    {
        // Arrange
        string traceContent = @"
This is a header line
Process(1234).Thread(5678)/(1.234): Some other event
Process(1234).Thread(5678)/(1.235): Method Enter: Assembly.Type.ValidMethod()
Process(1234).Thread(5678)/(1.236): Exception thrown
Process(1234).Thread(5678)/(1.237): Method Exit: Assembly.Type.ValidMethod()
Footer information
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        HashSet<string> result = await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContain("Assembly.Type.ValidMethod");
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonExistentFile = Path.Combine(Path.GetTempPath(), "non_existent_trace.txt");

        // Act & Assert
        await Should.ThrowAsync<FileNotFoundException>(
            async () => await parser.ParseExecutedMethodsAsync(nonExistentFile)
        );
    }

    [TestMethod]
    public async Task ParseExecutedMethodsAsync_LogsInformation()
    {
        // Arrange
        string traceContent = @"
Process(1234).Thread(5678)/(1.234): Method Enter: Assembly.Type.Method1()
Process(1234).Thread(5678)/(1.235): Method Enter: Assembly.Type.Method2()
";
        await File.WriteAllTextAsync(testTraceFile, traceContent);

        // Act
        await parser.ParseExecutedMethodsAsync(testTraceFile);

        // Assert
        logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "Log").ShouldBeGreaterThanOrEqualTo(1);
    }
}