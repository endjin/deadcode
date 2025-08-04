# DeadCode - .NET Unused Code Analyzer

A .NET global tool that identifies unused code through static and dynamic analysis, generating LLM-ready cleanup plans.

## Overview

DeadCode combines static reflection-based method extraction with runtime trace profiling to identify methods that exist in your codebase but are never executed. The tool generates minimal JSON reports optimized for LLM consumption to create precise code cleanup tasks.

## Features

- **Static Analysis**: Extracts all methods from compiled assemblies using reflection
- **Dynamic Profiling**: Uses dotnet-trace to capture runtime execution data with JIT event tracking
- **Safety Classification**: Categorizes methods by removal safety (High/Medium/Low/DoNotRemove)
- **LLM-Ready Output**: Generates minimal JSON with file:line references
- **Rich CLI**: Beautiful terminal interface with progress indicators
- **Framework Filtering**: Automatically filters out System.*, Microsoft.*, and Internal.* methods
- **Smart Method Handling**: Special handling for async state machines, lambdas, and constructors
- **Interactive Setup**: Automatic dotnet-trace installation if missing
- **Modern .NET**: Built on .NET 9.0 with C# 12 language features

## Installation

Install as a global .NET tool:

```bash
dotnet tool install --global DeadCode
```

## Usage

### Quick Start - Full Analysis

Run complete analysis pipeline:

```bash
# Build your project first
dotnet build -c Release

# Run full analysis
deadcode full --assemblies ./bin/Release/net9.0/*.dll --executable ./bin/Release/net9.0/MyApp.exe
```

### Individual Commands

#### 1. Extract Method Inventory

```bash
deadcode extract bin/Release/net9.0/*.dll -o inventory.json
```

Example `inventory.json`:
```json
{
  "methods": [
    {
      "assemblyName": "MyApp",
      "typeName": "MyApp.Services.DataService",
      "methodName": "ProcessData",
      "signature": "ProcessData(String)",
      "visibility": "Public",
      "safetyLevel": "LowConfidence",
      "location": {
        "sourceFile": "/path/to/Services/DataService.cs",
        "declarationLine": 45,
        "bodyStartLine": 46,
        "bodyEndLine": 52
      },
      "fullyQualifiedName": "MyApp.Services.DataService.ProcessData",
      "hasSourceLocation": true
    }
  ]
}
```

#### 2. Profile Application Execution

```bash
deadcode profile MyApp.exe --scenarios scenarios.json -o traces/
```

**Supported trace formats:**
- `.nettrace` - Binary format from dotnet-trace (default)
- `.txt` - Text format for testing and debugging

#### 3. Analyze for Unused Code

```bash
deadcode analyze -i inventory.json -t traces/ -o report.json --min-confidence high
```

### Scenarios Configuration

Create `scenarios.json` to define test scenarios with all available options:

```json
{
  "scenarios": [
    {
      "name": "basic-functionality",
      "arguments": ["--help", "--verbose"],
      "duration": 30,
      "description": "Test help and basic commands"
    },
    {
      "name": "data-processing",
      "arguments": ["process", "--input", "data.csv", "--output", "results.json"],
      "description": "Test main data processing workflow"
    },
    {
      "name": "error-handling",
      "arguments": ["process", "--input", "invalid.csv"],
      "expectFailure": true,
      "description": "Test error handling with invalid input"
    },
    {
      "name": "api-endpoints",
      "arguments": ["serve", "--port", "8080"],
      "duration": 60,
      "description": "Test API server with various endpoints"
    }
  ]
}
```

**Scenario options:**
- `name` (required): Unique scenario identifier
- `arguments`: Command line arguments to pass
- `duration`: Maximum seconds to run (omit to run to completion)
- `description`: Human-readable description
- `expectFailure`: Set to true if the scenario should fail (e.g., for error handling tests)

## Output Format

The tool generates LLM-ready JSON that only includes methods with source locations:

```json
{
  "highConfidence": [
    {
      "file": "Services/UnusedService.cs",
      "line": 42,
      "method": "ProcessInternal",
      "dependencies": ["registration:Program.cs:23"]
    }
  ],
  "mediumConfidence": [],
  "lowConfidence": []
}
```

**Note**: Methods without source locations (e.g., compiler-generated methods) are automatically filtered from the output to keep it minimal and actionable.

## Safety Classification

### Classification Levels

| Level | Criteria | Examples | Recommendation |
|-------|----------|----------|----------------|
| **HighConfidence** | Private methods, no special attributes | Private helper methods, internal utilities | Safe to remove |
| **MediumConfidence** | Protected/Virtual methods, property accessors, event handlers | Override methods, getters/setters, OnClick handlers | Review carefully |
| **LowConfidence** | Public methods, test methods | API endpoints, public interfaces, unit tests | Likely false positives |
| **DoNotRemove** | Framework attributes, security code, P/Invoke | [DllImport], [Serializable], [SecurityCritical] | Never remove |

### Advanced Classification Rules

The tool detects and classifies based on:
- **Event Handlers**: Methods with `(object sender, EventArgs e)` signature → MediumConfidence
- **Test Methods**: Attributes containing Test, Fact, or Theory → LowConfidence
- **COM Interop**: Methods with [ComVisible] → DoNotRemove
- **Generated Code**: [GeneratedCode] attribute → DoNotRemove
- **Security**: [SecurityCritical], [SecuritySafeCritical] → DoNotRemove
- **Serialization**: [Serializable] on type → DoNotRemove for all methods
- **Constructors**: Special handling for .ctor and .cctor
- **Async Methods**: Normalizes async state machine methods to their original names
- **Lambda Classes**: Normalizes compiler-generated lambda display classes

## Commands

### `extract`
Extract method inventory from assemblies through static analysis.

**Options:**
- `[ASSEMBLIES]` - Assembly file paths to analyze (supports wildcards)
- `-o, --output` - Output path for inventory JSON (default: inventory.json)
- `--include-generated` - Include compiler-generated methods

### `profile`
Profile application execution to collect runtime trace data.

**Options:**
- `<EXECUTABLE>` - Path to executable to profile
- `--scenarios` - Path to scenarios JSON file
- `--args` - Arguments for single execution
- `-o, --output` - Output directory for trace files (default: traces/)
- `--duration` - Profiling duration in seconds

**Note**: If the executable is a DLL, the tool automatically uses `dotnet` to run it.

### `analyze`
Analyze method inventory against trace data to find unused code.

**Options:**
- `-i, --inventory` - Method inventory JSON file
- `-t, --traces` - Trace files or directory (supports both .nettrace and .txt)
- `-o, --output` - Output path for report
- `--min-confidence` - Minimum confidence level (high/medium/low)

### `full`
Run complete analysis pipeline.

**Options:**
- `--assemblies` - Assembly paths to analyze
- `--executable` - Executable to profile
- `--scenarios` - Scenarios configuration
- `--output` - Output directory for all artifacts
- `--min-confidence` - Minimum confidence level

## Technical Details

### Profiling Implementation
- Uses **JIT event tracking** for deterministic method detection
- Event providers: `Microsoft-Windows-DotNETRuntime:0x4C14FCCBD:5`
- Buffer size: 512 MB for method-heavy applications
- Captures methods as they are JIT-compiled, ensuring complete coverage

### Method Filtering
- **Framework methods** (System.*, Microsoft.*, Internal.*) are automatically filtered
- Focus on **application code** only
- Configurable with `--include-generated` flag for compiler-generated methods

### Interactive Features
- **Automatic dependency installation**: Prompts to install dotnet-trace if missing
- **Rich progress indicators**: Real-time status for all operations
- **Detailed summaries**: Tables showing analysis results

## Architecture

Built following clean architecture principles:

```
Solutions/
├── DeadCode/
│   ├── Core/                  # Domain layer
│   │   ├── Models/           # Domain entities (MethodInfo, UnusedMethod, etc.)
│   │   └── Services/         # Service interfaces
│   ├── Infrastructure/       # External concerns
│   │   ├── IO/              # File I/O, report generation
│   │   ├── Profiling/       # Trace collection and parsing
│   │   └── Reflection/      # Assembly analysis, safety classification
│   └── CLI/                 # Presentation layer
│       └── Commands/        # Command implementations
├── DeadCode.Tests/          # Comprehensive test suite
└── Samples/                 # Example applications
```

## Requirements

- .NET 9.0 SDK or later
- Windows, Linux, or macOS
- dotnet-trace (automatically installed if missing)

## Limitations

- **PDB Required**: Source locations need debug symbols
- **Dynamic Calls**: Cannot detect reflection/dynamic method calls
- **External Triggers**: Misses webhook/scheduled task handlers
- **DI Services**: May flag injected but unused services
- **Edge Cases**: Some dynamic/reflection calls and external triggers may be missed. See [LIMITATIONS.md](LIMITATIONS.md) for details and alternatives

## Troubleshooting

### Common Issues

**Issue**: "Executable not found" error
- **Solution**: Ensure the executable path is correct and the file exists

**Issue**: No methods found in trace
- **Solution**: Verify the application actually executes code paths. Increase duration or add more comprehensive scenarios

**Issue**: Large trace files
- **Solution**: Limit profiling duration or focus on specific scenarios. Trace files can grow large for long-running applications

**Issue**: Missing source locations
- **Solution**: Build with debug symbols (PDB files) and ensure they're in the same directory as assemblies

### Debug Logging

Enable detailed logging by setting the minimum log level:
```bash
# Set via environment variable (example for bash)
export Logging__LogLevel__Default=Debug
deadcode analyze -i inventory.json -t traces/
```

## Development

### Building from Source

```bash
git clone https://github.com/endjin/deadcode
cd deadcode/Solutions
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Installing Locally

```bash
dotnet pack
dotnet tool install --global --add-source ./DeadCode/bin/Release DeadCode
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

Apache License 2.0 - see LICENSE file for details.

## Example Workflow

1. **Build Project**: `dotnet build -c Release`
2. **Run Analysis**: `deadcode full --assemblies bin/Release/net9.0/*.dll --executable MyApp.exe`
3. **Review Report**: Check `analysis/report.json` for unused methods
4. **LLM Cleanup**: Feed report to Claude/GPT for automated cleanup tasks

The tool provides file:line references perfect for LLM-driven code refactoring!

---

*Built entirely from prompts with Claude Code using Opus 4 & Sonnet 4 models*