# Sample App with Dead Code

This sample application is designed to test the DeadCode analyzer. It contains various patterns of unused code that should be detected by the tool.

## Dead Code Patterns

### Calculator.cs
- `Subtract()` - Public method never called
- `Multiply()` - Public method never called  
- `Divide()` - Public method never called
- `CalculateSquareRoot()` - Private method never called
- `CalculateLogarithm()` - Protected virtual method never called

### DataProcessor.cs
- `ClearData()` - Public method never called
- `GetProcessedData()` - Public method never called
- `LogData()` - Private method never called
- `ProcessBatchAsync()` - Async method never called
- `DataProcessed` - Event never subscribed
- `OnDataProcessed()` - Event raiser never called

### StringHelper.cs
- `ToCamelCase()` - Static method never called
- `IsPalindrome()` - Static method never called
- `Truncate()` - Extension method never used
- `JoinWithSeparator<T>()` - Generic method never instantiated

### UnusedModels.cs
- `IObsoleteService` - Interface never implemented
- `BaseEntity` - Abstract class never inherited
- `Customer` - Class never instantiated
- `Constants` - Static class with unused members
- `ConfigurationManager` - Singleton never accessed

## Expected Detection Results

When running DeadCode analyzer on this project, it should identify all the above methods and types as unused code with appropriate confidence levels:

- **High Confidence**: Private methods like `CalculateSquareRoot()`, `LogData()`
- **Medium Confidence**: Protected/virtual methods, potential DI services
- **Low Confidence**: Public methods that might be called externally

## Running the Sample

```bash
dotnet run
```

The application will execute and use only a subset of the available methods, leaving the rest as dead code.