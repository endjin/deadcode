# DeadCode - .NET Unused Code Analyzer

A .NET global tool that identifies unused code through static and dynamic analysis, generating LLM-ready cleanup plans.

## Overview

DeadCode combines static reflection-based method extraction with runtime trace profiling to identify methods that exist in your codebase but are never executed. The tool generates minimal JSON reports optimized for LLM consumption to create precise code cleanup tasks.

## Features

- **Static Analysis**: Extracts all methods from compiled assemblies using reflection
- **Dynamic Profiling**: Uses dotnet-trace to capture runtime execution data
- **Safety Classification**: Categorizes methods by removal safety (High/Medium/Low/DoNotRemove)
- **LLM-Ready Output**: Generates minimal JSON with file:line references
- **Rich CLI**: Beautiful terminal interface with progress indicators
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

**Note**: Profiling uses dotnet-trace with JIT event tracking for deterministic method detection. The tool will automatically install dotnet-trace if it's not present. See [LIMITATIONS.md](LIMITATIONS.md) for details on remaining edge cases.

#### 3. Analyze for Unused Code

```bash
deadcode analyze -i inventory.json -t traces/ -o report.json --min-confidence high
```

### Scenarios Configuration

Create `scenarios.json` to define test scenarios:

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
      "name": "api-endpoints",
      "arguments": ["serve", "--port", "8080"],
      "duration": 60,
      "description": "Test API server with various endpoints"
    }
  ]
}
```

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

Methods without source locations (e.g., compiler-generated methods) are automatically filtered from the output.

## Safety Classification

| Level                | Criteria                                                      | Examples                                            | Recommendation         |
|----------------------|---------------------------------------------------------------|-----------------------------------------------------|------------------------|
| **HighConfidence**   | Private methods, no special attributes                        | Private helper methods, internal utilities          | Safe to remove         |
| **MediumConfidence** | Protected/Virtual methods, property accessors, event handlers | Override methods, getters/setters, OnClick handlers | Review carefully       |
| **LowConfidence**    | Public methods, test methods                                  | API endpoints, public interfaces, unit tests        | Likely false positives |
| **DoNotRemove**      | Framework attributes, security code, P/Invoke                 | [DllImport], [Serializable], [SecurityCritical]     | Never remove           |

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

### `analyze`
Analyze method inventory against trace data to find unused code.

**Options:**
- `-i, --inventory` - Method inventory JSON file
- `-t, --traces` - Trace files or directory
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