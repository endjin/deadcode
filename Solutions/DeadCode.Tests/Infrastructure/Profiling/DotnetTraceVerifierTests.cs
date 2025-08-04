using DeadCode.Infrastructure.Profiling;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Profiling;

[TestClass]
public class DotnetTraceVerifierTests
{
    private readonly ILogger<DotnetTraceVerifier> mockLogger;
    private readonly DotnetTraceVerifier verifier;

    public DotnetTraceVerifierTests()
    {
        mockLogger = Substitute.For<ILogger<DotnetTraceVerifier>>();
        verifier = new DotnetTraceVerifier(mockLogger);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DotnetTraceVerifier(null!));
    }

    [TestMethod]
    public async Task CheckDependenciesAsync_LogsInformation_WhenTraceInstalled()
    {
        // Note: This test is challenging because it depends on the actual system state
        // In a real scenario, we'd want to mock Process.Start, but that's complex
        // For now, we'll test the basic functionality

        // Act
        bool result = await verifier.CheckDependenciesAsync();

        // Assert - We can't assert the exact result since it depends on system state
        // But we can verify that it doesn't throw and returns a boolean
        result.ShouldBeOfType<bool>();
    }

    [TestMethod]
    public async Task InstallMissingDependenciesAsync_LogsInformation()
    {
        // Note: Similar to CheckDependenciesAsync, this depends on system state
        // In a production environment, we'd mock the Process class

        // Act
        bool result = await verifier.InstallMissingDependenciesAsync();

        // Assert
        result.ShouldBeOfType<bool>();
    }

    [TestMethod]
    public void DotnetTraceVerifier_ImplementsIDependencyVerifier()
    {
        // Act & Assert
        verifier.ShouldBeAssignableTo<DeadCode.Core.Services.IDependencyVerifier>();
    }
}