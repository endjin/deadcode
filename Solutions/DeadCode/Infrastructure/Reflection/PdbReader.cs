using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.Reflection;

/// <summary>
/// Reads debug information from PDB files using System.Reflection.Metadata
/// </summary>
public class PdbReader : IPdbReader
{
    private readonly ILogger<PdbReader> logger;

    public PdbReader(ILogger<PdbReader> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
    }

    public Task<SourceLocation?> GetSourceLocationAsync(MethodBase method, string assemblyPath)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (string.IsNullOrEmpty(assemblyPath)) throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));

        string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
        if (!File.Exists(pdbPath))
        {
            logger.LogDebug("PDB file not found: {Path}", pdbPath);
            return Task.FromResult<SourceLocation?>(null);
        }

        // Wrap synchronous I/O in Task.Run to avoid blocking
        return Task.Run(() =>
        {
            try
            {
                using FileStream pdbStream = File.OpenRead(pdbPath);
                using MetadataReaderProvider metadataProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
                MetadataReader metadataReader = metadataProvider.GetMetadataReader();

                // Get method metadata token
                int methodToken = method.MetadataToken;
                MethodDefinitionHandle methodHandle = MetadataTokens.MethodDefinitionHandle(methodToken);

                if (!methodHandle.IsNil)
                {
                    MethodDebugInformation methodDebugInfo = metadataReader.GetMethodDebugInformation(methodHandle);
                    SequencePointCollection sequencePoints = methodDebugInfo.GetSequencePoints();

                    if (sequencePoints.Any())
                    {
                        SequencePoint firstPoint = sequencePoints.First();
                        SequencePoint lastPoint = sequencePoints.Last();

                        DocumentHandle documentHandle = firstPoint.Document;
                        if (!documentHandle.IsNil)
                        {
                            Document document = metadataReader.GetDocument(documentHandle);
                            string documentName = metadataReader.GetString(document.Name);

                            return new SourceLocation(
                                SourceFile: documentName,
                                DeclarationLine: firstPoint.StartLine,
                                BodyStartLine: firstPoint.StartLine,
                                BodyEndLine: lastPoint.EndLine
                            );
                        }
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                logger.LogWarning(ex, "Invalid PDB format for {Path}", pdbPath);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading PDB file: {Path}", pdbPath);
                return null;
            }

            return (SourceLocation?)null;
        });
    }
}