using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace DeadCode.Tests.CLI.Infrastructure;

[TestClass]
public class TypeRegistrarTests
{
    private readonly ServiceCollection services;
    private readonly TypeRegistrar registrar;

    public TypeRegistrarTests()
    {
        services = new ServiceCollection();
        services.AddSingleton<ITestService, TestService>();
        registrar = new TypeRegistrar(services);
    }

    [TestMethod]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
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
    public void Register_MakesTypeResolvable()
    {
        // Arrange
        ServiceCollection freshServices = new();
        TypeRegistrar freshRegistrar = new(freshServices);

        // Act
        freshRegistrar.Register(typeof(ITestService), typeof(TestService));
        ITypeResolver resolver = freshRegistrar.Build();

        // Assert
        object? result = resolver.Resolve(typeof(ITestService));
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestService>();
    }

    [TestMethod]
    public void RegisterInstance_MakesInstanceResolvable()
    {
        // Arrange
        ServiceCollection freshServices = new();
        TypeRegistrar freshRegistrar = new(freshServices);
        TestService instance = new();

        // Act
        freshRegistrar.RegisterInstance(typeof(ITestService), instance);
        ITypeResolver resolver = freshRegistrar.Build();

        // Assert
        object? result = resolver.Resolve(typeof(ITestService));
        result.ShouldBe(instance);
    }

    [TestMethod]
    public void RegisterLazy_MakesLazyInstanceResolvable()
    {
        // Arrange
        ServiceCollection freshServices = new();
        TypeRegistrar freshRegistrar = new(freshServices);
        TestService instance = new();

        // Act
        freshRegistrar.RegisterLazy(typeof(ITestService), () => instance);
        ITypeResolver resolver = freshRegistrar.Build();

        // Assert
        object? result = resolver.Resolve(typeof(ITestService));
        result.ShouldBe(instance);
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
