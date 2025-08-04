using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using DeadCode.Core.Models;
using DeadCode.Core.Services;

using Microsoft.Extensions.Logging;

using MethodInfo = DeadCode.Core.Models.MethodInfo;

namespace DeadCode.Infrastructure.Reflection;

/// <summary>
/// Extracts method inventory using reflection
/// </summary>
public class ReflectionMethodExtractor : IMethodInventoryExtractor
{
    private readonly ILogger<ReflectionMethodExtractor> logger;
    private readonly ISafetyClassifier safetyClassifier;
    private readonly IPdbReader pdbReader;

    public ReflectionMethodExtractor(ILogger<ReflectionMethodExtractor> logger, ISafetyClassifier safetyClassifier, IPdbReader pdbReader)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(safetyClassifier);
        ArgumentNullException.ThrowIfNull(pdbReader);
        this.logger = logger;
        this.safetyClassifier = safetyClassifier;
        this.pdbReader = pdbReader;
    }

    public async Task<MethodInventory> ExtractAsync(string[] assemblyPaths, ExtractionOptions options)
    {
        logger.LogInformation("Extracting methods from {Count} assemblies", assemblyPaths.Length);

        MethodInventory inventory = new();

        foreach (string path in assemblyPaths)
        {
            try
            {
                IEnumerable<MethodInfo> methods = await ExtractFromAssemblyAsync(path, options);
                inventory.AddMethods(methods);

                options.Progress?.Report(new ExtractionProgress(
                    ProcessedAssemblies: Array.IndexOf(assemblyPaths, path) + 1,
                    TotalAssemblies: assemblyPaths.Length,
                    CurrentAssembly: Path.GetFileName(path)
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to extract methods from {Path}", path);
                // Continue processing other assemblies
            }
        }

        logger.LogInformation(
            "Extracted {Count} methods from {AssemblyCount} assemblies",
            inventory.Count, assemblyPaths.Length);

        return inventory;
    }

    private async Task<IEnumerable<MethodInfo>> ExtractFromAssemblyAsync(string assemblyPath, ExtractionOptions options)
    {
        AssemblyLoadContext context = new($"MethodExtraction_{Path.GetFileName(assemblyPath)}", isCollectible: true);
        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);
            return await ExtractMethodsAsync(assembly, assemblyPath);
        }
        finally
        {
            context.Unload();
        }
    }

    public async Task<IEnumerable<MethodInfo>> ExtractMethodsAsync(Assembly assembly, string assemblyPath)
    {
        logger.LogDebug("Extracting methods from assembly {Name}", assembly.GetName().Name);

        List<MethodInfo> methods = [];

        try
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(t => !IsCompilerGenerated(t) && !t.IsEnum && !t.IsInterface);

            foreach (Type? type in types)
            {
                try
                {
                    await foreach (MethodInfo method in ExtractMethodsFromTypeAsync(type, assembly.GetName().Name ?? "Unknown", assemblyPath))
                    {
                        methods.Add(method);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to extract methods from type {Type}", type.FullName);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            logger.LogWarning("Some types could not be loaded: {Message}", ex.Message);
            // Try to process the types that were successfully loaded
            IEnumerable<Type?> loadedTypes = ex.Types.Where(t => t != null && !IsCompilerGenerated(t!) && !t!.IsEnum && !t.IsInterface);
            foreach (Type? type in loadedTypes)
            {
                try
                {
                    await foreach (MethodInfo method in ExtractMethodsFromTypeAsync(type!, assembly.GetName().Name ?? "Unknown", assemblyPath))
                    {
                        methods.Add(method);
                    }
                }
                catch (Exception innerEx)
                {
                    logger.LogWarning(innerEx, "Failed to extract methods from type {Type}", type!.FullName);
                }
            }
        }

        return methods;
    }

    // Backward compatibility wrapper for synchronous usage
    public IEnumerable<MethodInfo> ExtractMethods(Assembly assembly, string assemblyPath)
    {
        return ExtractMethodsAsync(assembly, assemblyPath).GetAwaiter().GetResult();
    }

    private async IAsyncEnumerable<MethodInfo> ExtractMethodsFromTypeAsync(Type type, string assemblyName, string assemblyPath)
    {
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Instance | BindingFlags.Static |
                          BindingFlags.DeclaredOnly;

        // Get regular methods
        IEnumerable<System.Reflection.MethodInfo> methods = type.GetMethods(bindingFlags)
            .Where(m => !IsCompilerGenerated(m));

        foreach (System.Reflection.MethodInfo? method in methods)
        {
            SourceLocation? location = await pdbReader.GetSourceLocationAsync(method, assemblyPath);
            yield return new MethodInfo(
                AssemblyName: assemblyName,
                TypeName: type.FullName ?? type.Name,
                MethodName: method.Name,
                Signature: GetMethodSignature(method),
                Visibility: GetMethodVisibility(method),
                SafetyLevel: safetyClassifier.ClassifyMethod(method),
                Location: location
            );
        }

        // Get constructors (instance and static)
        MethodBase?[] constructorArray = [
            ..type.GetConstructors(bindingFlags).Cast<MethodBase>(),
            type.TypeInitializer
        ];
        IEnumerable<MethodBase?> constructors = constructorArray
            .Where(c => c != null && !IsCompilerGenerated(c));

        foreach (MethodBase? ctor in constructors)
        {
            SourceLocation? location = await pdbReader.GetSourceLocationAsync(ctor!, assemblyPath);
            yield return new MethodInfo(
                AssemblyName: assemblyName,
                TypeName: type.FullName ?? type.Name,
                MethodName: ctor!.Name,
                Signature: GetConstructorSignature(ctor),
                Visibility: GetMethodVisibility(ctor),
                SafetyLevel: safetyClassifier.ClassifyMethod(ctor),
                Location: location
            );
        }
    }

    private IEnumerable<MethodInfo> ExtractMethodsFromType(Type type, string assemblyName, string assemblyPath)
    {
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Instance | BindingFlags.Static |
                          BindingFlags.DeclaredOnly;

        // Get regular methods
        IEnumerable<System.Reflection.MethodInfo> methods = type.GetMethods(bindingFlags)
            .Where(m => !IsCompilerGenerated(m));

        foreach (System.Reflection.MethodInfo? method in methods)
        {
            SourceLocation? location = pdbReader.GetSourceLocationAsync(method, assemblyPath).GetAwaiter().GetResult();
            yield return new MethodInfo(
                AssemblyName: assemblyName,
                TypeName: type.FullName ?? type.Name,
                MethodName: method.Name,
                Signature: GetMethodSignature(method),
                Visibility: GetMethodVisibility(method),
                SafetyLevel: safetyClassifier.ClassifyMethod(method),
                Location: location
            );
        }

        // Get constructors (instance and static)
        MethodBase?[] constructorArray = [
            ..type.GetConstructors(bindingFlags).Cast<MethodBase>(),
            type.TypeInitializer
        ];
        IEnumerable<MethodBase?> constructors = constructorArray
            .Where(c => c != null && !IsCompilerGenerated(c));

        foreach (MethodBase? ctor in constructors)
        {
            SourceLocation? location = pdbReader.GetSourceLocationAsync(ctor!, assemblyPath).GetAwaiter().GetResult();
            yield return new MethodInfo(
                AssemblyName: assemblyName,
                TypeName: type.FullName ?? type.Name,
                MethodName: ctor!.Name,
                Signature: GetConstructorSignature(ctor),
                Visibility: GetMethodVisibility(ctor),
                SafetyLevel: safetyClassifier.ClassifyMethod(ctor),
                Location: location
            );
        }
    }

    private static bool IsCompilerGenerated(MemberInfo member)
    {
        return member.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
               member.Name.Contains('<') && member.Name.Contains('>') ||
               member.Name.Contains("__BackingField");
    }

    private static string GetMethodSignature(System.Reflection.MethodInfo method)
    {
        List<string> parameters = method.GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToList();

        return $"{method.Name}({string.Join(", ", parameters)})";
    }

    private static string GetConstructorSignature(MethodBase ctor)
    {
        List<string> parameters = ctor.GetParameters()
            .Select(p => p.ParameterType.Name)
            .ToList();

        return $"{ctor.Name}({string.Join(", ", parameters)})";
    }

    private static MethodVisibility GetMethodVisibility(MethodBase method)
    {
        if (method.IsPublic) return MethodVisibility.Public;
        if (method.IsPrivate) return MethodVisibility.Private;
        if (method.IsFamily) return MethodVisibility.Protected;
        if (method.IsAssembly) return MethodVisibility.Internal;
        if (method.IsFamilyOrAssembly) return MethodVisibility.ProtectedInternal;

        return MethodVisibility.Private; // Default
    }
}