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

#### 2. Profile Application Execution

```bash
deadcode profile MyApp.exe --scenarios scenarios.json -o traces/
```

**Note**: Profiling uses dotnet-trace with JIT event tracking for deterministic method detection. See [LIMITATIONS.md](LIMITATIONS.md) for details on remaining edge cases.

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
      "name": "full-workflow",
      "arguments": ["process", "--input", "data.csv"],
      "description": "Complete processing workflow"
    }
  ]
}
```

## Output Format

The tool generates LLM-ready JSON:

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

## Safety Classification

| Level | Criteria | Recommendation |
|-------|----------|----------------|
| **HighConfidence** | Private methods, no special attributes | Safe to remove |
| **MediumConfidence** | Protected/Virtual methods, DI services | Review carefully |
| **LowConfidence** | Public methods, test methods | Likely false positives |
| **DoNotRemove** | Framework attributes, security code | Never remove |

## Commands

### `extract`
Extract method inventory from assemblies through static analysis.

**Options:**
- `--assemblies` - Assembly paths (supports wildcards)
- `-o, --output` - Output path for inventory JSON
- `--include-generated` - Include compiler-generated methods

### `profile`
Profile application execution to collect runtime trace data.

**Options:**
- `<EXECUTABLE>` - Path to executable to profile
- `--scenarios` - Path to scenarios JSON file
- `--args` - Arguments for executable
- `-o, --output` - Output directory for trace files

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
DeadCode/
├── Core/           # Domain models and interfaces
├── Infrastructure/ # External concerns (I/O, profiling, reflection)
└── CLI/           # User interface layer
```

## Requirements

- .NET 9.0 SDK
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
git clone <repository>
cd DeadCode
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Installing Locally

```bash
dotnet pack
dotnet tool install --global --add-source ./bin/Release DeadCode
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

MIT License - see LICENSE file for details.

## Example Workflow

1. **Build Project**: `dotnet build -c Release`
2. **Run Analysis**: `deadcode full --assemblies bin/Release/net9.0/*.dll --executable MyApp.exe`
3. **Review Report**: Check `analysis/report.json` for unused methods
4. **LLM Cleanup**: Feed report to Claude/GPT for automated cleanup tasks

The tool provides file:line references perfect for LLM-driven code refactoring!