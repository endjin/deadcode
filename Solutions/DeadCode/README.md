# DeadCode

A .NET global tool that identifies unused code through static and dynamic analysis, generating LLM-ready cleanup plans.

## Installation

```bash
dotnet tool install --global DeadCode
```

## Quick Start

```bash
# Build your project
dotnet build -c Release

# Run full analysis
deadcode full --assemblies ./bin/Release/net9.0/*.dll --executable ./bin/Release/net9.0/MyApp.exe

# View the generated report
cat analysis/report.json
```

## Features

- **Static Analysis**: Extracts all methods from compiled assemblies
- **Dynamic Profiling**: Captures runtime execution data using dotnet-trace
- **Safety Classification**: Categorizes methods by removal safety
- **LLM-Ready Output**: Generates minimal JSON optimized for AI code cleanup
- **Beautiful CLI**: Rich terminal interface with progress indicators

## Basic Usage

### Extract method inventory
```bash
deadcode extract bin/Release/net9.0/*.dll -o inventory.json
```

Example `inventory.json`:
```json
{
  "assemblyName": "MyApp",
  "methods": [
    {
      "id": "MyApp.Services.DataService::ProcessData(System.String)",
      "name": "ProcessData",
      "declaringType": "MyApp.Services.DataService",
      "visibility": "Public",
      "sourceLocation": {
        "file": "Services/DataService.cs",
        "line": 45
      }
    },
    {
      "id": "MyApp.Helpers.StringHelper::FormatOutput(System.String)",
      "name": "FormatOutput",
      "declaringType": "MyApp.Helpers.StringHelper",
      "visibility": "Private",
      "sourceLocation": {
        "file": "Helpers/StringHelper.cs",
        "line": 12
      }
    }
  ]
}

### Profile execution
```bash
deadcode profile MyApp.exe --args "arg1 arg2" -o traces/
```

Or use scenarios for comprehensive testing:
```bash
deadcode profile MyApp.exe --scenarios scenarios.json -o traces/
```

Example `scenarios.json`:
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

### Analyze for unused code
```bash
deadcode analyze -i inventory.json -t traces/ -o report.json
```

## Documentation

For detailed documentation, examples, and advanced usage, visit:
https://github.com/endjin/deadcode

## License

Apache License 2.0 - Copyright © 2024 Endjin Limited

## Requirements

- .NET 9.0 SDK or later
- Windows, Linux, or macOS