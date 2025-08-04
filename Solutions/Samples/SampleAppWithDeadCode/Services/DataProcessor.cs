namespace SampleAppWithDeadCode.Services;

public class DataProcessor
{
    private readonly List<string> processedData = new();

    // Used method
    public void ProcessData(string data)
    {
        if (ValidateData(data))
        {
            string normalized = NormalizeData(data);
            processedData.Add(normalized);
            Console.WriteLine($"Processed: {normalized}");
        }
    }

    // Used by ProcessData
    private bool ValidateData(string data)
    {
        return !string.IsNullOrWhiteSpace(data);
    }

    // Used by ProcessData
    private string NormalizeData(string data)
    {
        return data.Trim().ToUpperInvariant();
    }

    // DEAD CODE: Never called
    public void ClearData()
    {
        processedData.Clear();
    }

    // DEAD CODE: Never called
    public IReadOnlyList<string> GetProcessedData()
    {
        return processedData.AsReadOnly();
    }

    // DEAD CODE: Private method never called
    private void LogData(string data)
    {
        Console.WriteLine($"[LOG] {DateTime.Now}: {data}");
    }

    // DEAD CODE: Complex method never called
    public async Task<int> ProcessBatchAsync(IEnumerable<string> items)
    {
        int count = 0;
        foreach (string item in items)
        {
            await Task.Delay(100); // Simulate async work
            ProcessData(item);
            count++;
        }
        return count;
    }

    // DEAD CODE: Event handler never subscribed
    public event EventHandler<string>? DataProcessed;

    // DEAD CODE: Method to raise event
    protected virtual void OnDataProcessed(string data)
    {
        DataProcessed?.Invoke(this, data);
    }
}