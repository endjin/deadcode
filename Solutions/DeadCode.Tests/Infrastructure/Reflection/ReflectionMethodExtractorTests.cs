using System.Reflection;

using DeadCode.Core.Models;
using DeadCode.Core.Services;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Reflection;

[TestClass]
public class ReflectionMethodExtractorTests
{
    private ReflectionMethodExtractor extractor = null!;
    private ILogger<ReflectionMethodExtractor> logger = null!;
    private RuleBasedSafetyClassifier classifier = null!;
    private IPdbReader pdbReader = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<ReflectionMethodExtractor>>();
        ILogger<RuleBasedSafetyClassifier> classifierLogger = Substitute.For<ILogger<RuleBasedSafetyClassifier>>();
        classifier = new RuleBasedSafetyClassifier(classifierLogger);
        pdbReader = Substitute.For<IPdbReader>();

        // Setup default PDB reader behavior
        pdbReader.GetSourceLocationAsync(Arg.Any<MethodBase>(), Arg.Any<string>())
            .Returns(Task.FromResult<SourceLocation?>(null));

        extractor = new ReflectionMethodExtractor(logger, classifier, pdbReader);
    }

    [TestMethod]
    public void ExtractMethods_FromCurrentAssembly_FindsMethods()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        methods.ShouldNotBeNull();
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.ToList();
        methodList.ShouldNotBeEmpty();

        // Should find this test method
        methodList.ShouldContain(m =>
            m.MethodName == nameof(ExtractMethods_FromCurrentAssembly_FindsMethods) &&
            m.TypeName.Contains(nameof(ReflectionMethodExtractorTests)));
    }

    [TestMethod]
    public void ExtractMethods_ExcludesCompilerGeneratedTypes()
    {
        // Arrange
        Assembly assembly = typeof(TestClassWithLambda).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.ToList();

        // Should not include compiler-generated display classes
        methodList.ShouldNotContain(m => m.TypeName.Contains("<>c__DisplayClass"));
        methodList.ShouldNotContain(m => m.TypeName.Contains("<<"));
        methodList.ShouldNotContain(m => m.MethodName.Contains("<") && m.MethodName.Contains(">"));
    }

    [TestMethod]
    public void ExtractMethods_IncludesAllVisibilityLevels()
    {
        // Arrange
        Assembly assembly = typeof(VisibilityTestClass).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.Where(m => m.TypeName.Contains(nameof(VisibilityTestClass))).ToList();

        // Should find methods of all visibility levels
        methodList.ShouldContain(m => m.MethodName == "PublicMethod" && m.Visibility == MethodVisibility.Public);
        methodList.ShouldContain(m => m.MethodName == "PrivateMethod" && m.Visibility == MethodVisibility.Private);
        methodList.ShouldContain(m => m.MethodName == "ProtectedMethod" && m.Visibility == MethodVisibility.Protected);
        methodList.ShouldContain(m => m.MethodName == "InternalMethod" && m.Visibility == MethodVisibility.Internal);
    }

    [TestMethod]
    public void ExtractMethods_HandlesConstructors()
    {
        // Arrange
        Assembly assembly = typeof(ConstructorTestClass).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.Where(m => m.TypeName.Contains(nameof(ConstructorTestClass))).ToList();

        // Should find constructors
        methodList.ShouldContain(m => m.MethodName == ".ctor" && m.Signature == ".ctor()");
        methodList.ShouldContain(m => m.MethodName == ".ctor" && m.Signature.Contains("String"));

        // Should find static constructor if present
        if (typeof(ConstructorTestClass).TypeInitializer != null)
        {
            methodList.ShouldContain(m => m.MethodName == ".cctor");
        }
    }

    [TestMethod]
    public void ExtractMethods_IncludesSignatureInformation()
    {
        // Arrange
        Assembly assembly = typeof(SignatureTestClass).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.Where(m => m.TypeName.Contains(nameof(SignatureTestClass))).ToList();

        // Verify signatures contain parameter information
        methodList.ShouldContain(m => m.MethodName == "MethodWithNoParams" && m.Signature == "MethodWithNoParams()");
        methodList.ShouldContain(m => m.MethodName == "MethodWithOneParam" && m.Signature.Contains("String"));
        methodList.ShouldContain(m => m.MethodName == "MethodWithMultipleParams" &&
            m.Signature.Contains("String") && m.Signature.Contains("Int32"));
    }

    [TestMethod]
    public void ExtractMethods_HandlesGenericTypes()
    {
        // Arrange
        Assembly assembly = typeof(GenericTestClass<>).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.Where(m => m.TypeName.Contains("GenericTestClass")).ToList();

        methodList.ShouldNotBeEmpty();
        methodList.ShouldContain(m => m.MethodName == "GenericMethod");
        methodList.ShouldContain(m => m.MethodName == "Process");
    }

    [TestMethod]
    public void ExtractMethods_HandlesNestedTypes()
    {
        // Arrange
        Assembly assembly = typeof(OuterClass.NestedClass).Assembly;

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.ToList();

        // Should find methods in nested types
        methodList.ShouldContain(m => m.TypeName.Contains("NestedClass") && m.MethodName == "NestedMethod");
        methodList.ShouldContain(m => m.TypeName.Contains("OuterClass") && m.MethodName == "OuterMethod");
    }

    [TestMethod]
    public void ExtractMethods_AppliesSafetyClassification()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        IEnumerable<DeadCode.Core.Models.MethodInfo> methods = extractor.ExtractMethods(assembly, assembly.Location);

        // Assert
        List<DeadCode.Core.Models.MethodInfo> methodList = methods.ToList();

        // Verify safety classifications are applied
        methodList.Where(m => m.SafetyLevel == SafetyClassification.HighConfidence).ShouldNotBeEmpty();
        methodList.Where(m => m.SafetyLevel == SafetyClassification.MediumConfidence).ShouldNotBeEmpty();
        methodList.Where(m => m.SafetyLevel == SafetyClassification.LowConfidence).ShouldNotBeEmpty();
    }

    // Test helper classes
    private class TestClassWithLambda
    {
        public void MethodWithLambda()
        {
            int[] numbers = new[] { 1, 2, 3 };
            IEnumerable<int> doubled = numbers.Select(n => n * 2);
        }
    }

    private class VisibilityTestClass
    {
        public void PublicMethod() { }
        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
    }

    private class ConstructorTestClass
    {
        public ConstructorTestClass() { }
        public ConstructorTestClass(string value) { }
    }

    private class SignatureTestClass
    {
        public void MethodWithNoParams() { }
        public void MethodWithOneParam(string param) { }
        public void MethodWithMultipleParams(string param1, int param2, bool param3) { }
        public int MethodWithReturnType() => 42;
    }

    private class GenericTestClass<T>
    {
        public void Process(T item) { }
        public TResult GenericMethod<TResult>(T input) => default!;
    }

    private class OuterClass
    {
        public void OuterMethod() { }

        public class NestedClass
        {
            public void NestedMethod() { }
        }
    }
}