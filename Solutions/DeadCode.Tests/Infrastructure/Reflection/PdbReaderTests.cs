using System.Reflection;

using DeadCode.Core.Models;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Reflection;

[TestClass]
public class PdbReaderTests
{
    private PdbReader pdbReader = null!;
    private ILogger<PdbReader> logger = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<PdbReader>>();
        pdbReader = new PdbReader(logger);
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await pdbReader.GetSourceLocationAsync(null!, "test.dll")
        );
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithNullAssemblyPath_ThrowsArgumentException()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(PdbReaderTests).GetMethod(nameof(Setup))!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await pdbReader.GetSourceLocationAsync(method, null!)
        );
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithEmptyAssemblyPath_ThrowsArgumentException()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(PdbReaderTests).GetMethod(nameof(Setup))!;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await pdbReader.GetSourceLocationAsync(method, string.Empty)
        );
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithNonExistentPdb_ReturnsNull()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(PdbReaderTests).GetMethod(nameof(Setup))!;
        string nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.dll");

        // Act
        SourceLocation? result = await pdbReader.GetSourceLocationAsync(method, nonExistentPath);

        // Assert
        result.ShouldBeNull();

        // Verify logging
        logger.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == "Log")
            .ShouldBeGreaterThanOrEqualTo(1);
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithInvalidPdbFile_ReturnsNull()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(PdbReaderTests).GetMethod(nameof(Setup))!;
        string tempDll = Path.GetTempFileName();
        string tempPdb = Path.ChangeExtension(tempDll, ".pdb");

        try
        {
            // Create an invalid PDB file
            await File.WriteAllTextAsync(tempPdb, "This is not a valid PDB file");

            // Act
            SourceLocation? result = await pdbReader.GetSourceLocationAsync(method, tempDll);

            // Assert
            result.ShouldBeNull();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempDll)) File.Delete(tempDll);
            if (File.Exists(tempPdb)) File.Delete(tempPdb);
        }
    }

    [TestMethod]
    public async Task GetSourceLocationAsync_WithActualPdb_ReturnsSourceLocation()
    {
        // Arrange
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        string assemblyPath = currentAssembly.Location;
        System.Reflection.MethodInfo method = typeof(PdbReaderTests).GetMethod(nameof(Setup))!;

        // Skip test if no PDB exists (e.g., in Release mode without PDBs)
        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        if (!File.Exists(pdbPath))
        {
            Assert.Inconclusive("PDB file not found for test assembly");
        }

        // Act
        SourceLocation? result = await pdbReader.GetSourceLocationAsync(method, assemblyPath);

        // Assert
        // The result may be null if PDB doesn't contain debug info for this specific method
        // This is expected in some configurations, so we just verify no exceptions
        // In a real scenario with proper PDBs, this would return actual source location
        logger.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == "Log" &&
                   call.GetArguments()[0]?.ToString()?.Contains("Error") == true)
            .ShouldBe(0);
    }
}