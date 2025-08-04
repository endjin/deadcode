namespace SampleAppWithDeadCode.Models;

// DEAD CODE: Entire interface never implemented
public interface IObsoleteService
{
    void PerformObsoleteOperation();
    Task<bool> CheckObsoleteStatusAsync();
}

// DEAD CODE: Abstract class never inherited
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    protected abstract void Validate();
    
    public virtual void Save()
    {
        Validate();
        UpdatedAt = DateTime.UtcNow;
    }
}

// DEAD CODE: Unused model class
public class Customer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    
    public int GetAge()
    {
        return DateTime.Today.Year - DateOfBirth.Year;
    }
    
    private bool IsValidEmail()
    {
        return Email.Contains("@") && Email.Contains(".");
    }
}

// DEAD CODE: Static class with unused constants
public static class Constants
{
    public const string DefaultConnectionString = "Server=localhost;Database=SampleDb";
    public const int MaxRetryCount = 3;
    public const double TaxRate = 0.08;
    
    public static readonly string[] SupportedFileTypes = { ".txt", ".csv", ".json" };
}

// DEAD CODE: Singleton pattern never instantiated
public class ConfigurationManager
{
    private static ConfigurationManager? instance;
    private static readonly object @lock = new object();
    
    private ConfigurationManager() { }
    
    public static ConfigurationManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (@lock)
                {
                    instance ??= new ConfigurationManager();
                }
            }
            return instance;
        }
    }
    
    public string GetSetting(string key)
    {
        // Placeholder implementation
        return $"Value for {key}";
    }
}