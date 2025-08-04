using System.Runtime.InteropServices;

namespace SampleConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sample Console App");
        var calculator = new Calculator();
        var result = calculator.Add(5, 3);
        Console.WriteLine($"5 + 3 = {result}");
        
        // Call only some methods
        UsedPublicMethod();
        var helper = new Helper();
        helper.DoWork();
    }
    
    // This method is called
    public static void UsedPublicMethod()
    {
        Console.WriteLine("This method is used");
    }
    
    // This method is never called - should be High confidence for removal
    private static void UnusedPrivateMethod()
    {
        Console.WriteLine("This method is never called");
    }
    
    // This method is never called but is public - should be Low confidence
    public static void UnusedPublicMethod()
    {
        Console.WriteLine("This public method is never called");
    }
    
    // Virtual method never called - should be Medium confidence
    protected virtual void UnusedVirtualMethod()
    {
        Console.WriteLine("Virtual method not called");
    }
    
    // Method with DllImport - should be DoNotRemove
    [DllImport("kernel32.dll")]
    private static extern bool UnusedDllImportMethod();
    
    // Property getter/setter - should be Medium confidence if unused
    public string UnusedProperty { get; set; } = string.Empty;
}

public class Calculator
{
    public int Add(int a, int b) => a + b;
    
    // Unused method in Calculator - should be High confidence
    private int UnusedSubtract(int a, int b) => a - b;
    
    // Unused public method - should be Low confidence
    public int UnusedMultiply(int a, int b) => a * b;
}

internal class Helper
{
    public void DoWork()
    {
        Console.WriteLine("Helper is working");
    }
    
    // Unused private method - should be High confidence
    private void UnusedHelperMethod()
    {
        Console.WriteLine("This helper method is unused");
    }
}

// Completely unused class - all methods should be flagged
internal class UnusedClass
{
    public void UnusedMethod1()
    {
        Console.WriteLine("Method 1");
    }
    
    private void UnusedMethod2()
    {
        Console.WriteLine("Method 2");
    }
}