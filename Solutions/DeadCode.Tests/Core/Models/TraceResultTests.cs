using DeadCode.Core.Models;

namespace DeadCode.Tests.Core.Models;

[TestClass]
public class TraceResultTests
{
    [TestMethod]
    public void Constructor_WithAllValues_CreatesInstance()
    {
        // Arrange
        DateTime startTime = DateTime.UtcNow.AddMinutes(-5);
        DateTime endTime = DateTime.UtcNow;

        // Act
        TraceResult result = new(
            TraceFilePath: "/path/to/trace.nettrace",
            ScenarioName: "test-scenario",
            StartTime: startTime,
            EndTime: endTime,
            IsSuccessful: true,
            ErrorMessage: null
        );

        // Assert
        result.TraceFilePath.ShouldBe("/path/to/trace.nettrace");
        result.ScenarioName.ShouldBe("test-scenario");
        result.StartTime.ShouldBe(startTime);
        result.EndTime.ShouldBe(endTime);
        result.IsSuccessful.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [TestMethod]
    public void Constructor_WithErrorMessage_CreatesFailedResult()
    {
        // Arrange
        DateTime startTime = DateTime.UtcNow.AddMinutes(-5);
        DateTime endTime = DateTime.UtcNow;

        // Act
        TraceResult result = new(
            TraceFilePath: "/path/to/trace.nettrace",
            ScenarioName: "failed-scenario",
            StartTime: startTime,
            EndTime: endTime,
            IsSuccessful: false,
            ErrorMessage: "Process exited with error code 1"
        );

        // Assert
        result.IsSuccessful.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Process exited with error code 1");
    }

    [TestMethod]
    public void Duration_CalculatesCorrectly()
    {
        // Arrange
        DateTime startTime = DateTime.UtcNow.AddMinutes(-10);
        DateTime endTime = DateTime.UtcNow;
        TimeSpan expectedDuration = endTime - startTime;

        TraceResult result = new(
            TraceFilePath: "/trace.nettrace",
            ScenarioName: "test",
            StartTime: startTime,
            EndTime: endTime,
            IsSuccessful: true
        );

        // Act
        TimeSpan duration = result.Duration;

        // Assert
        duration.ShouldBe(expectedDuration);
        duration.TotalMinutes.ShouldBeInRange(9.9, 10.1); // Allow small variance
    }

    [TestMethod]
    public void TraceFileExists_WhenFileExists_ReturnsTrue()
    {
        // Arrange
        string tempFile = Path.GetTempFileName();
        try
        {
            TraceResult result = new(
                TraceFilePath: tempFile,
                ScenarioName: "test",
                StartTime: DateTime.UtcNow,
                EndTime: DateTime.UtcNow,
                IsSuccessful: true
            );

            // Act & Assert
            result.TraceFileExists.ShouldBeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void TraceFileExists_WhenFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        TraceResult result = new(
            TraceFilePath: "/nonexistent/file.nettrace",
            ScenarioName: "test",
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow,
            IsSuccessful: true
        );

        // Act & Assert
        result.TraceFileExists.ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        DateTime startTime = DateTime.UtcNow;
        DateTime endTime = startTime.AddMinutes(5);

        TraceResult result1 = new("/trace.nettrace", "test", startTime, endTime, true, null);
        TraceResult result2 = new("/trace.nettrace", "test", startTime, endTime, true, null);

        // Act & Assert
        result1.Equals(result2).ShouldBeTrue();
        (result1 == result2).ShouldBeTrue();
        result1.GetHashCode().ShouldBe(result2.GetHashCode());
    }

    [TestMethod]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        DateTime startTime = DateTime.UtcNow;
        DateTime endTime = startTime.AddMinutes(5);

        TraceResult result1 = new("/trace1.nettrace", "test", startTime, endTime, true);
        TraceResult result2 = new("/trace2.nettrace", "test", startTime, endTime, true);
        TraceResult result3 = new("/trace1.nettrace", "different", startTime, endTime, true);
        TraceResult result4 = new("/trace1.nettrace", "test", startTime, endTime, false);

        // Act & Assert
        result1.Equals(result2).ShouldBeFalse();
        result1.Equals(result3).ShouldBeFalse();
        result1.Equals(result4).ShouldBeFalse();
        (result1 != result2).ShouldBeTrue();
    }

    [TestMethod]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        TraceResult result = new(
            TraceFilePath: "/path/to/trace.nettrace",
            ScenarioName: "test-scenario",
            StartTime: DateTime.UtcNow,
            EndTime: DateTime.UtcNow.AddMinutes(5),
            IsSuccessful: true
        );

        // Act
        string str = result.ToString();

        // Assert
        str.ShouldContain("TraceFilePath = /path/to/trace.nettrace");
        str.ShouldContain("ScenarioName = test-scenario");
        str.ShouldContain("IsSuccessful = True");
    }

    [TestMethod]
    public void With_ModifiesSpecificProperties()
    {
        // Arrange
        TraceResult original = new(
            "/trace.nettrace",
            "test",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5),
            true
        );

        // Act
        TraceResult modified = original with { IsSuccessful = false, ErrorMessage = "Test error" };

        // Assert
        modified.IsSuccessful.ShouldBeFalse();
        modified.ErrorMessage.ShouldBe("Test error");
        modified.TraceFilePath.ShouldBe(original.TraceFilePath);
        modified.ScenarioName.ShouldBe(original.ScenarioName);
        original.IsSuccessful.ShouldBeTrue(); // Original unchanged
    }
}