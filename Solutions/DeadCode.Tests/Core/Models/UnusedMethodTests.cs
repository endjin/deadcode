using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class UnusedMethodTests
{
    [TestMethod]
    public void UnusedMethod_Creation_SetsAllProperties()
    {
        // Arrange
        MethodInfo method = CreateTestMethodInfo();
        List<string> dependencies = ["dep1", "dep2", "dep3"];

        // Act
        UnusedMethod unusedMethod = new(method, dependencies);

        // Assert
        unusedMethod.Method.ShouldBe(method);
        unusedMethod.Dependencies.Count.ShouldBe(3);
        unusedMethod.Dependencies.ShouldContain("dep1");
        unusedMethod.Dependencies.ShouldContain("dep2");
        unusedMethod.Dependencies.ShouldContain("dep3");
    }

    [TestMethod]
    public void UnusedMethod_FilePath_ReturnsLocationSourceFile()
    {
        // Arrange
        SourceLocation location = new("MyFile.cs", 10, 12, 20);
        MethodInfo method = CreateTestMethodInfo(location: location);
        UnusedMethod unusedMethod = new(method, []);

        // Act & Assert
        unusedMethod.FilePath.ShouldBe("MyFile.cs");
    }

    [TestMethod]
    public void UnusedMethod_FilePath_ReturnsNullWhenNoLocation()
    {
        // Arrange
        MethodInfo method = CreateTestMethodInfo(location: null);
        UnusedMethod unusedMethod = new(method, []);

        // Act & Assert
        unusedMethod.FilePath.ShouldBeNull();
    }

    [TestMethod]
    public void UnusedMethod_LineNumber_ReturnsLocationDeclarationLine()
    {
        // Arrange
        SourceLocation location = new("MyFile.cs", 42, 44, 50);
        MethodInfo method = CreateTestMethodInfo(location: location);
        UnusedMethod unusedMethod = new(method, []);

        // Act & Assert
        unusedMethod.LineNumber.ShouldBe(42);
    }

    [TestMethod]
    public void UnusedMethod_LineNumber_ReturnsNullWhenNoLocation()
    {
        // Arrange
        MethodInfo method = CreateTestMethodInfo(location: null);
        UnusedMethod unusedMethod = new(method, []);

        // Act & Assert
        unusedMethod.LineNumber.ShouldBeNull();
    }


    [TestMethod]
    public void UnusedMethod_Dependencies_IsReadOnlyList()
    {
        // Arrange
        MethodInfo method = CreateTestMethodInfo();
        List<string> dependencies = ["dep1"];

        // Act
        UnusedMethod unusedMethod = new(method, dependencies);

        // Assert
        unusedMethod.Dependencies.ShouldBeAssignableTo<IReadOnlyList<string>>();
    }

    // Helper method
    private static MethodInfo CreateTestMethodInfo(SourceLocation? location = null)
    {
        return new MethodInfo(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );
    }
}