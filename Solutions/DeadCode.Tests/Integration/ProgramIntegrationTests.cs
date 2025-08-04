using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace DeadCode.Tests.Integration;

[TestClass]
public class ProgramIntegrationTests
{
    [TestMethod]
    public async Task Main_WithHelpFlag_ShowsHelp()
    {
        // Arrange
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "DeadCode.dll --help",
                WorkingDirectory = Path.GetDirectoryName(typeof(TypeRegistrar).Assembly.Location),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Act
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        process.ExitCode.ShouldBe(0);
        output.ShouldContain("USAGE:");
        output.ShouldContain("deadcode");
        output.ShouldContain("extract");
        output.ShouldContain("profile");
        output.ShouldContain("analyze");
        output.ShouldContain("full");
    }

    [TestMethod]
    public async Task Main_WithVersionFlag_ShowsVersion()
    {
        // Arrange
        Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "DeadCode.dll --version",
                WorkingDirectory = Path.GetDirectoryName(typeof(TypeRegistrar).Assembly.Location),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Act
        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        process.ExitCode.ShouldBe(0);
        output.ShouldContain("1.0.0");
    }

    [TestMethod]
    public void TypeRegistrar_CanBeCreatedWithServiceProvider()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act
        TypeRegistrar registrar = new(serviceProvider);
        ITypeResolver resolver = registrar.Build();

        // Assert
        registrar.ShouldNotBeNull();
        resolver.ShouldNotBeNull();
        resolver.ShouldBeOfType<TypeResolver>();
    }

    [TestMethod]
    public void TypeResolver_ResolvesRegisteredServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton<ITestInterface, TestImplementation>();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        TypeResolver resolver = new(serviceProvider);

        // Act
        object? resolved = resolver.Resolve(typeof(ITestInterface));

        // Assert
        resolved.ShouldNotBeNull();
        resolved.ShouldBeOfType<TestImplementation>();
    }

    [TestMethod]
    public void TypeResolver_ReturnsNullForUnregisteredType()
    {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        TypeResolver resolver = new(serviceProvider);

        // Act
        object? resolved = resolver.Resolve(typeof(IUnregisteredInterface));

        // Assert
        resolved.ShouldBeNull();
    }

    private interface ITestInterface { }
    private class TestImplementation : ITestInterface { }
    private interface IUnregisteredInterface { }
}