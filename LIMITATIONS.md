# DeadCode Tool Limitations

## JIT Event Tracking

The DeadCode tool now successfully uses the Microsoft.Diagnostics.Tracing.TraceEvent library to extract JIT compilation events from .nettrace files, providing deterministic method tracking.

### How It Works

1. **JIT Event Capture**: Using `--providers "Microsoft-Windows-DotNETRuntime:0x4C14FCCBD:5"` captures JIT compilation events (MethodJittingStarted, MethodLoadVerbose).

2. **TraceEvent Library Integration**: The tool uses `EventPipeEventSource` to parse .nettrace files and extract JIT events directly:
```csharp
using var source = new EventPipeEventSource(traceFilePath);
var clrParser = new ClrTraceEventParser(source);
clrParser.MethodJittingStarted += (data) => {
    // Extract method: data.MethodNamespace, data.MethodName, data.MethodSignature
};
```

3. **Accurate Results**: The tool correctly identifies which methods were JIT-compiled (and thus executed) during runtime. It filters application methods from framework methods to focus on user code.

## Remaining Limitations

### 1. Dynamic/Reflection Calls
The tool cannot detect methods called through reflection or dynamic invocation, as these don't go through normal JIT compilation.

### 2. External Triggers
Methods triggered by external events (webhooks, scheduled tasks, message handlers) may not be exercised during profiling scenarios.

### 3. Lazy Initialization
Methods that are only called under specific conditions may be missed if those conditions aren't met during profiling.

### 4. Interface/Abstract Methods
Abstract methods and interface definitions show as having no source location since they have no implementation body.

## Alternative Approaches

### 1. Code Coverage Integration
Consider using code coverage tools for comprehensive analysis:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
Code coverage data can complement JIT trace analysis for test-driven scenarios.

### 3. Custom EventSource Integration
Add method tracking directly in the application:
```csharp
[EventSource(Name = "MyApp-MethodTracker")]
public sealed class MethodTracker : EventSource
{
    [Event(1)]
    public void MethodEntry(string methodName) => WriteEvent(1, methodName);
}
```

### 4. Runtime Instrumentation
Implement custom instrumentation using:
- Assembly weaving (e.g., with Fody)
- .NET Profiling APIs
- Runtime hooks

### 5. Production Telemetry
Instrument production code to log method usage over time.

## Test Support

For unit testing purposes, the tool also accepts `.txt` trace files with a simple format:
```
Method Enter: Namespace.Class.Method(Parameters)
```

This allows testing the analysis pipeline without generating real .nettrace files, making unit tests fast and deterministic.

## Future Improvements

1. Enhanced integration with code coverage tools (coverlet)
2. Custom profiler using .NET Profiling APIs for deeper analysis
3. Production telemetry data support for real-world usage patterns
4. Assembly instrumentation options for comprehensive tracking