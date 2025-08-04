using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class SourceLocationTests
{
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

    [TestMethod]
    public void SourceLocation_Equality_WorksCorrectly()
    {
        // Arrange
        SourceLocation location1 = new("File.cs", 10, 12, 20);
        SourceLocation location2 = new("File.cs", 10, 12, 20);
        SourceLocation location3 = new("OtherFile.cs", 10, 12, 20);
        SourceLocation location4 = new("File.cs", 15, 17, 25);

        // Act & Assert
        location1.ShouldBe(location2);
        location1.ShouldNotBe(location3);
        location1.ShouldNotBe(location4);
        (location1 == location2).ShouldBeTrue();
        (location1 == location3).ShouldBeFalse();
    }

    [TestMethod]
    public void SourceLocation_ToString_ReturnsFormattedString()
    {
        // Arrange
        SourceLocation location = new("MyFile.cs", 10, 12, 20);

        // Act
        string result = location.ToString();

        // Assert
        result.ShouldContain("MyFile.cs");
        result.ShouldContain("10");
        result.ShouldContain("12");
        result.ShouldContain("20");
    }

    [TestMethod]
    public void SourceLocation_WithNullSourceFile_HandlesCorrectly()
    {
        // Arrange & Act
        SourceLocation location = new(null!, 10, 12, 20);

        // Assert
        location.SourceFile.ShouldBeNull();
        location.DeclarationLine.ShouldBe(10);
    }
}