using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class MethodInfoTests
{
    [TestMethod]
    public void MethodInfo_Creation_SetsAllProperties()
    {
        // Arrange & Act
        SourceLocation location = new("Test.cs", 10, 12, 20);
        MethodInfo methodInfo = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod(string, int)",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );

        // Assert
        methodInfo.AssemblyName.ShouldBe("TestAssembly");
        methodInfo.TypeName.ShouldBe("TestType");
        methodInfo.MethodName.ShouldBe("TestMethod");
        methodInfo.Signature.ShouldBe("TestMethod(string, int)");
        methodInfo.Visibility.ShouldBe(MethodVisibility.Public);
        methodInfo.SafetyLevel.ShouldBe(SafetyClassification.HighConfidence);
        methodInfo.Location.ShouldBe(location);
    }

    [TestMethod]
    public void MethodInfo_FullyQualifiedName_ReturnsCorrectFormat()
    {
        // Arrange
        MethodInfo methodInfo = new(
            AssemblyName: "TestAssembly",
            TypeName: "MyNamespace.MyClass",
            MethodName: "MyMethod",
            Signature: "MyMethod()",
            Visibility: MethodVisibility.Private,
            SafetyLevel: SafetyClassification.MediumConfidence
        );

        // Act
        string fullyQualifiedName = methodInfo.FullyQualifiedName;

        // Assert
        fullyQualifiedName.ShouldBe("MyNamespace.MyClass.MyMethod");
    }

    [TestMethod]
    public void MethodInfo_HasSourceLocation_ReturnsTrueWhenLocationProvided()
    {
        // Arrange
        MethodInfo methodInfo = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Internal,
            SafetyLevel: SafetyClassification.LowConfidence,
            Location: new SourceLocation("Test.cs", 1, 2, 3)
        );

        // Act & Assert
        methodInfo.HasSourceLocation.ShouldBeTrue();
    }

    [TestMethod]
    public void MethodInfo_HasSourceLocation_ReturnsFalseWhenLocationNull()
    {
        // Arrange
        MethodInfo methodInfo = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Protected,
            SafetyLevel: SafetyClassification.DoNotRemove,
            Location: null
        );

        // Act & Assert
        methodInfo.HasSourceLocation.ShouldBeFalse();
    }

    [TestMethod]
    public void MethodInfo_RecordEquality_WorksCorrectly()
    {
        // Arrange
        SourceLocation location = new("Test.cs", 10, 12, 20);
        MethodInfo methodInfo1 = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );

        MethodInfo methodInfo2 = new(
            AssemblyName: "TestAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );

        MethodInfo methodInfo3 = new(
            AssemblyName: "DifferentAssembly",
            TypeName: "TestType",
            MethodName: "TestMethod",
            Signature: "TestMethod()",
            Visibility: MethodVisibility.Public,
            SafetyLevel: SafetyClassification.HighConfidence,
            Location: location
        );

        // Act & Assert
        methodInfo1.ShouldBe(methodInfo2);
        methodInfo1.ShouldNotBe(methodInfo3);
        (methodInfo1 == methodInfo2).ShouldBeTrue();
        (methodInfo1 == methodInfo3).ShouldBeFalse();
    }

    [TestMethod]
    public void SourceLocation_Creation_SetsAllProperties()
    {
        // Arrange & Act
        SourceLocation location = new(
            SourceFile: "MyFile.cs",
            DeclarationLine: 10,
            BodyStartLine: 12,
            BodyEndLine: 20
        );

        // Assert
        location.SourceFile.ShouldBe("MyFile.cs");
        location.DeclarationLine.ShouldBe(10);
        location.BodyStartLine.ShouldBe(12);
        location.BodyEndLine.ShouldBe(20);
    }
}