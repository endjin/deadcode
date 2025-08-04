using System.Reflection;
using System.Runtime.InteropServices;

using DeadCode.Core.Models;
using DeadCode.Infrastructure.Reflection;

using Microsoft.Extensions.Logging;

namespace DeadCode.Tests.Infrastructure.Reflection;

[TestClass]
public class RuleBasedSafetyClassifierTests
{
    private RuleBasedSafetyClassifier classifier = null!;
    private ILogger<RuleBasedSafetyClassifier> logger = null!;

    [TestInitialize]
    public void Setup()
    {
        logger = Substitute.For<ILogger<RuleBasedSafetyClassifier>>();
        classifier = new RuleBasedSafetyClassifier(logger);
    }

    [TestMethod]
    public void ClassifyMethod_WithNullMethod_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => classifier.ClassifyMethod(null!));
    }

    [TestMethod]
    public void ClassifyMethod_WithDllImportAttribute_ReturnsDoNotRemove()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithAttributes).GetMethod(nameof(TestClassWithAttributes.DllImportMethod))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.DoNotRemove);
    }

    [TestMethod]
    public void ClassifyMethod_WithSerializableAttribute_ReturnsDoNotRemove()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(SerializableTestClass).GetMethod(nameof(SerializableTestClass.SerializableMethod))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.DoNotRemove);
    }

    [TestMethod]
    public void ClassifyMethod_WithPublicMethod_ReturnsLowConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithVisibility).GetMethod(nameof(TestClassWithVisibility.PublicMethod))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.LowConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithPrivateMethod_ReturnsHighConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithVisibility).GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.HighConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithProtectedMethod_ReturnsMediumConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithVisibility).GetMethod("ProtectedMethod", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.MediumConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithVirtualMethod_ReturnsMediumConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithVisibility).GetMethod(nameof(TestClassWithVisibility.VirtualMethod))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.MediumConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithTestMethodAttribute_ReturnsLowConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestMethodClass).GetMethod(nameof(TestMethodClass.SomeTestMethod))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.LowConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithEventHandlerSignature_ReturnsMediumConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithEventHandler).GetMethod(nameof(TestClassWithEventHandler.Button_Click))!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.MediumConfidence);
    }

    [TestMethod]
    public void ClassifyMethod_WithSpecialNameMethod_ReturnsMediumConfidence()
    {
        // Arrange
        System.Reflection.MethodInfo method = typeof(TestClassWithProperty).GetMethod("get_Property")!;

        // Act
        SafetyClassification result = classifier.ClassifyMethod(method);

        // Assert
        result.ShouldBe(SafetyClassification.MediumConfidence);
    }

    // Test helper classes
#pragma warning disable CA1060 // Move pinvokes to native methods class - Required for testing
    private class TestClassWithAttributes
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool DllImportMethod();
    }
#pragma warning restore CA1060

    [Serializable]
    private class SerializableTestClass
    {
        public void SerializableMethod() { }
    }

    private class TestClassWithVisibility
    {
        public void PublicMethod() { }
        private void PrivateMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }
        public virtual void VirtualMethod() { }
    }

    // Custom attribute to simulate test methods
    private class TestAttribute : Attribute { }

    private class TestMethodClass
    {
        [Test]
        public void SomeTestMethod() { }
    }

    private class TestClassWithEventHandler
    {
        public void Button_Click(object sender, EventArgs e) { }
    }

    private class TestClassWithProperty
    {
        public string Property { get; set; } = string.Empty;
    }
}