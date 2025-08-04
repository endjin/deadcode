using DeadCode.Core.Services;

namespace DeadCode.Tests.Core.Services;

[TestClass]
public class ExtractionOptionsTests
{
    [TestMethod]
    public void ExtractionOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        ExtractionOptions options = new();

        // Assert
        options.IncludeCompilerGenerated.ShouldBeFalse();
        options.Progress.ShouldBeNull();
    }

    [TestMethod]
    public void ExtractionOptions_WithInitializer_SetsProperties()
    {
        // Arrange
        IProgress<ExtractionProgress> progress = Substitute.For<IProgress<ExtractionProgress>>();

        // Act
        ExtractionOptions options = new()
        {
            IncludeCompilerGenerated = true,
            Progress = progress
        };

        // Assert
        options.IncludeCompilerGenerated.ShouldBeTrue();
        options.Progress.ShouldBe(progress);
    }

    [TestMethod]
    public void ExtractionProgress_Creation_SetsAllProperties()
    {
        // Arrange & Act
        ExtractionProgress progress = new(
            ProcessedAssemblies: 5,
            TotalAssemblies: 10,
            CurrentAssembly: "MyAssembly.dll"
        );

        // Assert
        progress.ProcessedAssemblies.ShouldBe(5);
        progress.TotalAssemblies.ShouldBe(10);
        progress.CurrentAssembly.ShouldBe("MyAssembly.dll");
    }

    [TestMethod]
    public void ExtractionProgress_RecordEquality_WorksCorrectly()
    {
        // Arrange
        ExtractionProgress progress1 = new(5, 10, "Test.dll");
        ExtractionProgress progress2 = new(5, 10, "Test.dll");
        ExtractionProgress progress3 = new(6, 10, "Test.dll");
        ExtractionProgress progress4 = new(5, 10, "Other.dll");

        // Act & Assert
        progress1.ShouldBe(progress2);
        progress1.ShouldNotBe(progress3);
        progress1.ShouldNotBe(progress4);
        (progress1 == progress2).ShouldBeTrue();
        (progress1 == progress3).ShouldBeFalse();
    }

    [TestMethod]
    public void ExtractionProgress_ToString_ReturnsFormattedString()
    {
        // Arrange
        ExtractionProgress progress = new(5, 10, "MyAssembly.dll");

        // Act
        string result = progress.ToString();

        // Assert
        result.ShouldContain("5");
        result.ShouldContain("10");
        result.ShouldContain("MyAssembly.dll");
    }
}