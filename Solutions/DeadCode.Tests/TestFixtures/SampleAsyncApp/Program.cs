namespace SampleAsyncApp;

class Program
{
    private static readonly HttpClient httpClient = new();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("Sample Async App");
        
        var processor = new DataProcessor();
        processor.DataProcessed += OnDataProcessed;
        
        // Call some async methods
        await processor.ProcessDataAsync("test data");
        await UsedAsyncMethod();
        
        // Use lambda
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var doubled = numbers.Select(n => n * 2);
        Console.WriteLine($"Doubled: {string.Join(", ", doubled)}");
    }
    
    // Used async method
    private static async Task UsedAsyncMethod()
    {
        await Task.Delay(100);
        Console.WriteLine("Async method completed");
    }
    
    // Unused async method - should be High confidence
    private static async Task UnusedAsyncMethod()
    {
        await Task.Delay(100);
        Console.WriteLine("This async method is never called");
    }
    
    // Event handler - should be Medium confidence (called via event)
    private static void OnDataProcessed(object? sender, EventArgs e)
    {
        Console.WriteLine("Data was processed");
    }
    
    // Unused event handler - should be Medium confidence
    private static void UnusedEventHandler(object? sender, EventArgs e)
    {
        Console.WriteLine("This handler is never attached");
    }
}

public class DataProcessor
{
    public event EventHandler? DataProcessed;
    
    public async Task ProcessDataAsync(string data)
    {
        await Task.Delay(50);
        Console.WriteLine($"Processing: {data}");
        OnDataProcessed();
    }
    
    // Used by event
    protected virtual void OnDataProcessed()
    {
        DataProcessed?.Invoke(this, EventArgs.Empty);
    }
    
    // Unused async method with cancellation - should be Low confidence (public)
    public async Task UnusedProcessWithCancellationAsync(string data, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Processing with cancellation: {data}");
    }
    
    // Unused private async - should be High confidence
    private async Task<int> UnusedComputeAsync()
    {
        await Task.Delay(100);
        return 42;
    }
}

// Generic class with unused methods
public class GenericProcessor<T>
{
    public void Process(T item)
    {
        Console.WriteLine($"Processing item of type {typeof(T)}");
    }
    
    // Unused generic method - should be Low confidence (public)
    public TResult UnusedTransform<TResult>(T input, Func<T, TResult> transformer)
    {
        return transformer(input);
    }
    
    // Unused private generic - should be High confidence
    private bool UnusedCompare<TCompare>(TCompare a, TCompare b) where TCompare : IComparable<TCompare>
    {
        return a.CompareTo(b) > 0;
    }
}