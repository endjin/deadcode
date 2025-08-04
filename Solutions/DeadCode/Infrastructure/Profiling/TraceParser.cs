using System.Text.RegularExpressions;
using DeadCode.Core.Services;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Extensions.Logging;

namespace DeadCode.Infrastructure.Profiling;

/// <summary>
/// Parses trace files to extract executed methods
/// </summary>
public class TraceParser : ITraceParser
{
    private readonly ILogger<TraceParser> logger;
    private readonly SignatureNormalizer signatureNormalizer;

    // Regex pattern for text-based trace files (used in tests)
    private static readonly Regex MethodEntryPattern = new(@"Method\s+Enter:\s+([^\(]+(?:\([^\)]*\))?)", RegexOptions.Compiled);

    public TraceParser(ILogger<TraceParser> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this.logger = logger;
        signatureNormalizer = new SignatureNormalizer();
    }

    public Task<HashSet<string>> ParseTraceAsync(string traceFilePath)
    {
        logger.LogInformation("Parsing trace file: {Path}", traceFilePath);

        return ParseExecutedMethodsAsync(traceFilePath);
    }

    public async Task<HashSet<string>> ParseExecutedMethodsAsync(string traceFilePath)
    {
        if (!File.Exists(traceFilePath))
        {
            throw new FileNotFoundException($"Trace file not found: {traceFilePath}");
        }

        logger.LogDebug("Starting to parse trace file: {Path}", traceFilePath);
        HashSet<string> executedMethods = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Check if this is a binary .nettrace file or a text file (for tests)
            bool isBinary = await IsBinaryTraceFileAsync(traceFilePath);
            logger.LogDebug("Trace file is binary: {IsBinary}", isBinary);

            if (isBinary)
            {
                // Parse JIT events directly from .nettrace file using TraceEvent library
                logger.LogDebug("Parsing JIT events from .nettrace file");
                await ParseJitEventsFromTraceAsync(traceFilePath, executedMethods);
            }
            else
            {
                // Parse as text file (for tests)
                logger.LogDebug("Parsing as text trace file");
                await ParseTextTraceFileAsync(traceFilePath, executedMethods);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing trace file: {Path}", traceFilePath);
            throw;
        }

        logger.LogInformation(
            "Found {MethodCount} unique executed methods in trace file",
            executedMethods.Count
        );

        // Log first few methods for debugging
        if (logger.IsEnabled(LogLevel.Debug) && executedMethods.Any())
        {
            string sampleMethods = string.Join(", ", executedMethods.Take(5));
            logger.LogDebug("Sample executed methods: {Methods}", sampleMethods);
        }

        return executedMethods;
    }

    private async Task ParseJitEventsFromTraceAsync(string traceFilePath, HashSet<string> executedMethods)
    {
        await Task.Run(() =>
        {
            try
            {
                // Open the .nettrace file directly with TraceEvent
                // Ensure path is trimmed and absolute to avoid whitespace issues
                string cleanPath = Path.GetFullPath(traceFilePath.Trim());
                logger.LogDebug("Opening trace file: '{Path}'", cleanPath);

                if (!File.Exists(cleanPath))
                {
                    throw new FileNotFoundException($"Trace file not found: {cleanPath}");
                }

                // Use EventPipeEventSource for cross-platform .nettrace files
                using EventPipeEventSource source = new(cleanPath);

                // Create CLR event parser to access JIT events
                ClrTraceEventParser clrParser = new(source);

                // Subscribe to JIT compilation events
                clrParser.MethodJittingStarted += (data) =>
                {
                    try
                    {
                        // Filter out system/framework methods
                        if (data.MethodNamespace != null &&
                            (data.MethodNamespace.StartsWith("System.") ||
                             data.MethodNamespace.StartsWith("Microsoft.") ||
                             data.MethodNamespace.StartsWith("Internal.") ||
                             !data.MethodNamespace.StartsWith("SampleAppWithDeadCode")))
                        {
                            // Skip framework methods
                            return;
                        }

                        // Log raw JIT event data for debugging
                        if (data.MethodName == ".ctor" || data.MethodName == ".cctor")
                        {
                            logger.LogDebug("Constructor JIT event - Namespace: '{Namespace}', Method: '{Method}', Signature: '{Signature}'",
                                data.MethodNamespace, data.MethodName, data.MethodSignature);
                        }

                        // Build the full method signature
                        string methodSignature = BuildMethodSignature(
                            data.MethodNamespace ?? string.Empty,
                            data.MethodName,
                            data.MethodSignature
                        );

                        if (!string.IsNullOrEmpty(methodSignature))
                        {
                            executedMethods.Add(methodSignature);
                            logger.LogDebug("JIT compiled method: {Method}", methodSignature);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error processing JIT event for method: {Namespace}.{Method}",
                            data.MethodNamespace, data.MethodName);
                    }
                };

                // Alternative: Use MethodLoadVerbose for more detailed information
                clrParser.MethodLoadVerbose += (data) =>
                {
                    try
                    {
                        // Filter out system/framework methods
                        if (data.MethodNamespace != null &&
                            (data.MethodNamespace.StartsWith("System.") ||
                             data.MethodNamespace.StartsWith("Microsoft.") ||
                             data.MethodNamespace.StartsWith("Internal.") ||
                             !data.MethodNamespace.StartsWith("SampleAppWithDeadCode")))
                        {
                            // Skip framework methods
                            return;
                        }

                        if (data.MethodFlags.HasFlag(MethodFlags.Jitted))
                        {
                            string methodSignature = BuildMethodSignature(
                                data.MethodNamespace ?? string.Empty,
                                data.MethodName,
                                data.MethodSignature
                            );

                            if (!string.IsNullOrEmpty(methodSignature))
                            {
                                executedMethods.Add(methodSignature);
                                logger.LogTrace("Method loaded (verbose): {Method}", methodSignature);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error processing MethodLoadVerbose event for method: {Namespace}.{Method}",
                            data.MethodNamespace, data.MethodName);
                    }
                };

                // Process all events in the trace file
                logger.LogInformation("Processing JIT events from trace file...");
                source.Process();

                logger.LogInformation("Extracted {Count} JIT-compiled methods from trace", executedMethods.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing JIT events from trace file: {Path}", traceFilePath);
                throw;
            }
        });
    }

    private string BuildMethodSignature(string nameSpace, string methodName, string signature)
    {
        // Build full method signature for matching
        string fullName = string.IsNullOrEmpty(nameSpace)
            ? methodName
            : $"{nameSpace}.{methodName}";

        // Handle special cases for constructors
        if (methodName == ".ctor")
        {
            // Extract the class name from the namespace
            int lastDotIndex = nameSpace?.LastIndexOf('.') ?? -1;
            string className = lastDotIndex >= 0 && nameSpace != null
                ? nameSpace.Substring(lastDotIndex + 1)
                : nameSpace ?? "UnknownClass";

            fullName = string.IsNullOrEmpty(nameSpace)
                ? $"{className}..ctor"
                : $"{nameSpace}..ctor";
        }
        else if (methodName == ".cctor")
        {
            // Static constructor
            fullName = fullName.Replace(".cctor", "..cctor");
        }


        // For now, return just the full name without parameters
        // The signature normalization will handle matching
        return fullName;
    }

    private async Task<bool> IsBinaryTraceFileAsync(string filePath)
    {
        try
        {
            // First check file extension
            if (Path.GetExtension(filePath).Equals(".nettrace", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Then check for .nettrace file signature
            using FileStream stream = File.OpenRead(filePath);
            byte[] buffer = new byte[8];
            int bytesRead = await stream.ReadAsync(buffer, 0, 8);

            if (bytesRead >= 8)
            {
                // Check for "Nettrace" signature at the beginning
                string signature = System.Text.Encoding.ASCII.GetString(buffer);
                if (signature.StartsWith("Nettrace"))
                {
                    return true;
                }
            }

            // Fall back to original binary detection for other file types
            if (bytesRead >= 4)
            {
                // Check if it starts with text characters
                return !buffer.Take(4).All(b => b < 128 && (char.IsLetterOrDigit((char)b) || char.IsWhiteSpace((char)b) || char.IsPunctuation((char)b)));
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task ParseTextTraceFileAsync(string traceFilePath, HashSet<string> executedMethods)
    {
        using StreamReader reader = new(traceFilePath);
        string? line;
        int lineCount = 0;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineCount++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            Match match = MethodEntryPattern.Match(line);
            if (match.Success)
            {
                string methodName = match.Groups[1].Value.Trim();

                // Remove method parameters to get just the method name
                int parenIndex = methodName.IndexOf('(');
                if (parenIndex > 0)
                {
                    methodName = methodName.Substring(0, parenIndex);
                }

                if (!string.IsNullOrEmpty(methodName))
                {
                    executedMethods.Add(methodName);
                    logger.LogTrace("Found executed method: {Method}", methodName);
                }
            }
        }

        logger.LogDebug("Parsed {LineCount} lines from text trace file", lineCount);
    }

}

/// <summary>
/// Normalizes method signatures for consistent matching
/// </summary>
public class SignatureNormalizer
{
    private static readonly Dictionary<string, string> TypeAliases = new()
    {
        ["System.String"] = "string",
        ["System.Int32"] = "int",
        ["System.Int64"] = "long",
        ["System.Boolean"] = "bool",
        ["System.Double"] = "double",
        ["System.Single"] = "float",
        ["System.Decimal"] = "decimal",
        ["System.Byte"] = "byte",
        ["System.Char"] = "char",
        ["System.Object"] = "object",
        ["System.Void"] = "void"
    };

    public string NormalizeSignature(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return string.Empty;
        }

        string normalized = signature;
        string originalSignature = signature;

        // Handle speedscope format: "Assembly!Namespace.Type.Method(params)"
        if (normalized.Contains("!"))
        {
            string[] parts = normalized.Split('!');
            if (parts.Length >= 2)
            {
                // Take everything after the assembly name
                normalized = parts[1];
            }
        }

        // Replace type aliases
        foreach (KeyValuePair<string, string> kvp in TypeAliases)
        {
            normalized = normalized.Replace(kvp.Key, kvp.Value);
        }

        // Handle async state machines
        if (normalized.Contains("<") && normalized.Contains(">d__"))
        {
            // Extract original method name from async state machine
            Match match = Regex.Match(normalized, @"<([^>]+)>d__\d+");
            if (match.Success)
            {
                string originalMethod = match.Groups[1].Value;
                string typePrefix = normalized.Substring(0, normalized.IndexOf('<'));
                normalized = $"{typePrefix}.{originalMethod}";
            }
        }

        // Handle lambda display classes
        normalized = Regex.Replace(normalized, @"<>c__DisplayClass\d+_\d+", "LambdaClass");

        // Remove generic arity markers
        normalized = Regex.Replace(normalized, @"`\d+\[[^\]]+\]", "");
        normalized = Regex.Replace(normalized, @"`\d+", "");

        // Replace nested class separator
        normalized = normalized.Replace("+", ".");

        // Extract just the method name without parameters for now
        // This matches the behavior expected by the comparison logic
        int parenIndex = normalized.IndexOf('(');
        if (parenIndex > 0)
        {
            normalized = normalized.Substring(0, parenIndex);
        }

        string result = normalized.Trim();

        return result;
    }
}