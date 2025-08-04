using SampleAppWithDeadCode.Services;
using SampleAppWithDeadCode.Utilities;

namespace SampleAppWithDeadCode;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sample App with Dead Code");

        // Parse command line arguments
        string command = args.Length > 0 ? args[0] : "";
        
        switch (command)
        {
            case "--help":
                ShowHelp();
                break;
                
            case "--verbose":
                RunWithVerbose();
                break;
                
            case "--calculator":
                RunCalculatorOnly();
                break;
                
            case "--process":
                string dataFile = args.Length > 1 ? args[1] : "default-data.txt";
                RunDataProcessing(dataFile);
                break;
                
            case "--stress":
                int iterations = args.Length > 1 && int.TryParse(args[1], out int count) ? count : 10;
                RunStressTest(iterations);
                break;
                
            default:
                // Default execution path
                RunDefault();
                break;
        }
    }
    
    static void ShowHelp()
    {
        Console.WriteLine("Usage: SampleAppWithDeadCode [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --help              Show this help message");
        Console.WriteLine("  --verbose           Run with verbose output");
        Console.WriteLine("  --calculator        Test calculator functionality only");
        Console.WriteLine("  --process <file>    Process data from file");
        Console.WriteLine("  --stress <count>    Run stress test with specified iterations");
    }
    
    static void RunDefault()
    {
        // Use some services
        Calculator calculator = new Calculator();
        int result = calculator.Add(5, 3);
        Console.WriteLine($"5 + 3 = {result}");

        // Use string utilities
        string text = "Hello World";
        string reversed = StringHelper.Reverse(text);
        Console.WriteLine($"Reversed: {reversed}");

        // Call a method that uses some internal methods
        DataProcessor processor = new DataProcessor();
        processor.ProcessData("sample data");
    }
    
    static void RunWithVerbose()
    {
        Console.WriteLine("[VERBOSE] Starting application...");
        RunDefault();
        
        // Additional verbose operations
        Console.WriteLine("[VERBOSE] Testing calculator operations...");
        Calculator calc = new Calculator();
        Console.WriteLine($"[VERBOSE] 10 - 5 = {calc.Subtract(10, 5)}");
        Console.WriteLine($"[VERBOSE] 3 * 4 = {calc.Multiply(3, 4)}");
        Console.WriteLine("[VERBOSE] Application completed.");
    }
    
    static void RunCalculatorOnly()
    {
        Console.WriteLine("Running calculator tests...");
        Calculator calc = new Calculator();
        
        Console.WriteLine($"Add: 10 + 5 = {calc.Add(10, 5)}");
        Console.WriteLine($"Subtract: 10 - 5 = {calc.Subtract(10, 5)}");
        Console.WriteLine($"Multiply: 10 * 5 = {calc.Multiply(10, 5)}");
        Console.WriteLine($"Divide: 10 / 5 = {calc.Divide(10, 5)}");
        
        // Note: Still not calling CalculateSquareRoot or CalculateLogarithm
        // to keep them as dead code
    }
    
    static void RunDataProcessing(string dataFile)
    {
        Console.WriteLine($"Processing data from: {dataFile}");
        DataProcessor processor = new DataProcessor();

        // Process multiple items
        string[] items = new[] { "Item1", "Item2", "Item3", dataFile };
        foreach (string? item in items)
        {
            processor.ProcessData(item);
        }

        // Use some methods that were previously dead
        IReadOnlyList<string> processedData = processor.GetProcessedData();
        Console.WriteLine($"Processed {processedData.Count} items");
    }
    
    static void RunStressTest(int iterations)
    {
        Console.WriteLine($"Running stress test with {iterations} iterations...");

        DataProcessor processor = new DataProcessor();
        IEnumerable<string> items = Enumerable.Range(1, iterations).Select(i => $"Item{i}");

        // Use the async batch processing method
        Task<int> task = processor.ProcessBatchAsync(items);
        task.Wait(); // Block to wait for completion
        Console.WriteLine($"Stress test completed: processed {task.Result} items");
        
        // Test string utilities under stress
        for (int i = 0; i < iterations; i++)
        {
            string testString = $"TestString{i}";
            if (StringHelper.IsPalindrome(testString))
            {
                Console.WriteLine($"Found palindrome: {testString}");
            }
        }
    }
}