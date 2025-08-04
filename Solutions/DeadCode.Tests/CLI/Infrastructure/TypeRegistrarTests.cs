using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace DeadCode.Tests.CLI.Infrastructure;

[TestClass]
public class TypeRegistrarTests
{
    private readonly IServiceProvider serviceProvider;
    private readonly TypeRegistrar registrar;

    public TypeRegistrarTests()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITestService, TestService>();
        serviceProvider = services.BuildServiceProvider();
        registrar = new TypeRegistrar(serviceProvider);
    }

    [TestMethod]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TypeRegistrar(null!));
    }

    [TestMethod]
    public void Build_ReturnsTypeResolver()
    {
        // Act
        ITypeResolver resolver = registrar.Build();

        // Assert
        resolver.ShouldNotBeNull();
        resolver.ShouldBeOfType<TypeResolver>();
    }

    [TestMethod]
    public void Register_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => registrar.Register(typeof(ITestService), typeof(TestService)));
    }

    [TestMethod]
    public void RegisterInstance_DoesNotThrow()
    {
        // Arrange
        TestService instance = new();

        // Act & Assert
        Should.NotThrow(() => registrar.RegisterInstance(typeof(ITestService), instance));
    }

    [TestMethod]
    public void RegisterLazy_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => registrar.RegisterLazy(typeof(ITestService), () => new TestService()));
    }

    [TestMethod]
    public void TypeRegistrar_ImplementsITypeRegistrar()
    {
        // Act & Assert
        registrar.ShouldBeAssignableTo<ITypeRegistrar>();
    }

    // Test service interfaces for testing
    public interface ITestService { }
    public class TestService : ITestService { }
}

[TestClass]
public class TypeResolverTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TypeResolver _resolver;

    public TypeResolverTests()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITestService, TestService>();
        services.AddSingleton<TestService>();
        _serviceProvider = services.BuildServiceProvider();
        _resolver = new TypeResolver(_serviceProvider);
    }

    [TestMethod]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TypeResolver(null!));
    }

    [TestMethod]
    public void Resolve_WithNullType_ReturnsNull()
    {
        // Act
        object? result = _resolver.Resolve(null);

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public void Resolve_WithRegisteredType_ReturnsInstance()
    {
        // Act
        object? result = _resolver.Resolve(typeof(ITestService));

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestService>();
    }

    [TestMethod]
    public void Resolve_WithConcreteType_ReturnsInstance()
    {
        // Act
        object? result = _resolver.Resolve(typeof(TestService));

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestService>();
    }

    [TestMethod]
    public void Resolve_WithUnregisteredType_ReturnsNull()
    {
        // Act
        object? result = _resolver.Resolve(typeof(UnregisteredService));

        // Assert
        result.ShouldBeNull();
    }

    [TestMethod]
    public void TypeResolver_ImplementsITypeResolver()
    {
        // Act & Assert
        _resolver.ShouldBeAssignableTo<ITypeResolver>();
    }

    // Test service interfaces for testing
    public interface ITestService { }
    public class TestService : ITestService { }
    public class UnregisteredService { }
}