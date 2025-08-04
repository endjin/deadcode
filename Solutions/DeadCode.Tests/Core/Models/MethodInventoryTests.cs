using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class MethodInventoryTests
{
    private MethodInventory inventory = null!;

    [TestInitialize]
    public void Setup()
    {
        inventory = new MethodInventory();
    }

    [TestMethod]
    public void MethodInventory_StartsEmpty()
    {
        // Assert
        inventory.Methods.ShouldBeEmpty();
    }

    [TestMethod]
    public void AddMethod_WithValidMethod_AddsToInventory()
    {
        // Arrange
        MethodInfo method = CreateTestMethod("TestMethod");

        // Act
        inventory.AddMethod(method);

        // Assert
        inventory.Methods.Count.ShouldBe(1);
        inventory.Methods.First().ShouldBe(method);
    }

    [TestMethod]
    public void AddMethod_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => inventory.AddMethod(null!));
    }

    [TestMethod]
    public void AddMethods_WithMultipleMethods_AddsAllToInventory()
    {
        // Arrange
        MethodInfo[] methods = new[]
        {
            CreateTestMethod("Method1"),
            CreateTestMethod("Method2"),
            CreateTestMethod("Method3")
        };

        // Act
        inventory.AddMethods(methods);

        // Assert
        inventory.Methods.Count.ShouldBe(3);
        inventory.Methods.ShouldContain(methods[0]);
        inventory.Methods.ShouldContain(methods[1]);
        inventory.Methods.ShouldContain(methods[2]);
    }

    [TestMethod]
    public void AddMethods_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => inventory.AddMethods(null!));
    }

    [TestMethod]
    public void MethodsByAssembly_GroupsMethodsCorrectly()
    {
        // Arrange
        MethodInfo[] assembly1Methods = new[]
        {
            CreateTestMethod("Method1", "Assembly1"),
            CreateTestMethod("Method2", "Assembly1")
        };
        MethodInfo[] assembly2Methods = new[]
        {
            CreateTestMethod("Method3", "Assembly2"),
            CreateTestMethod("Method4", "Assembly2")
        };

        inventory.AddMethods(assembly1Methods);
        inventory.AddMethods(assembly2Methods);

        // Act
        IReadOnlyDictionary<string, List<MethodInfo>> methodsByAssembly = inventory.MethodsByAssembly;

        // Assert
        methodsByAssembly.Count.ShouldBe(2);
        methodsByAssembly.ContainsKey("Assembly1").ShouldBeTrue();
        methodsByAssembly.ContainsKey("Assembly2").ShouldBeTrue();
        methodsByAssembly["Assembly1"].Count.ShouldBe(2);
        methodsByAssembly["Assembly2"].Count.ShouldBe(2);
    }

    [TestMethod]
    public void GetMethodsBySafety_ReturnsCorrectMethods()
    {
        // Arrange
        MethodInfo[] highConfMethods = new[]
        {
            CreateTestMethod("High1", safety: SafetyClassification.HighConfidence),
            CreateTestMethod("High2", safety: SafetyClassification.HighConfidence)
        };
        MethodInfo[] lowConfMethods = new[]
        {
            CreateTestMethod("Low1", safety: SafetyClassification.LowConfidence)
        };

        inventory.AddMethods(highConfMethods);
        inventory.AddMethods(lowConfMethods);

        // Act
        IEnumerable<MethodInfo> result = inventory.GetMethodsBySafety(SafetyClassification.HighConfidence);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(highConfMethods[0]);
        result.ShouldContain(highConfMethods[1]);
        result.ShouldNotContain(lowConfMethods[0]);
    }


    [TestMethod]
    public void Methods_ReturnsReadOnlyCollection()
    {
        // Arrange
        MethodInfo method = CreateTestMethod("TestMethod");
        inventory.AddMethod(method);

        // Act
        List<MethodInfo> methods = inventory.Methods;

        // Assert
        methods.ShouldBeAssignableTo<IReadOnlyCollection<MethodInfo>>();
        methods.Count.ShouldBe(1);
    }

    [TestMethod]
    public void AddMethod_AllowsDuplicates()
    {
        // Arrange
        MethodInfo method = CreateTestMethod("TestMethod");

        // Act
        inventory.AddMethod(method);
        inventory.AddMethod(method);

        // Assert
        inventory.Methods.Count.ShouldBe(2);
    }

    // Helper method
    private static MethodInfo CreateTestMethod(
        string name,
        string assemblyName = "TestAssembly",
        string typeName = "TestType",
        SafetyClassification safety = SafetyClassification.HighConfidence)
    {
        return new MethodInfo(
            AssemblyName: assemblyName,
            TypeName: typeName,
            MethodName: name,
            Signature: $"{name}()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: safety
        );
    }
}