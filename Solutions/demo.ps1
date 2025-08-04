# DeadCode End-to-End Demo Script (PowerShell)
# This script demonstrates the complete workflow of using DeadCode to identify unused code

Write-Host "=== DeadCode End-to-End Demo ===" -ForegroundColor Cyan
Write-Host ""

# Demo directory
$DEMO_DIR = "demo_output"
$SAMPLE_APP = "Samples/SampleAppWithDeadCode"

# Build DeadCode tool first
Write-Host "Building DeadCode tool..." -ForegroundColor Blue
dotnet build DeadCode/DeadCode.csproj -c Release | Out-Null
Write-Host "✓ DeadCode tool built" -ForegroundColor Green
# Clean up previous demo
Write-Host "Cleaning up previous demo output..." -ForegroundColor Blue
if (Test-Path $DEMO_DIR) {
    Remove-Item -Recurse -Force $DEMO_DIR
}
New-Item -ItemType Directory -Path $DEMO_DIR | Out-Null

# Step 1: Build the sample application
Write-Host "`nStep 1: Building sample application with intentional dead code" -ForegroundColor Green
Write-Host "The sample app contains various patterns of unused code for testing"
Push-Location $SAMPLE_APP
dotnet build -c Release
Pop-Location
Write-Host "✓ Build complete" -ForegroundColor Green

# Step 2: Extract method inventory
Write-Host "`nStep 2: Extracting method inventory through static analysis" -ForegroundColor Green
Write-Host "This identifies all methods in the compiled assemblies..."
dotnet DeadCode/bin/Release/net9.0/DeadCode.dll extract `
    "$SAMPLE_APP/bin/Release/net9.0/*.dll" `
    -o "$DEMO_DIR/inventory.json"
Write-Host "✓ Method inventory extracted" -ForegroundColor Green
$methodCount = (Get-Content "$DEMO_DIR/inventory.json" | ConvertFrom-Json).methods.Count
Write-Host "Found methods: $methodCount"

# Step 3: Profile application execution
Write-Host "`nStep 3: Profiling application execution with scenarios" -ForegroundColor Green
Write-Host "This captures which methods are actually called at runtime..."
dotnet DeadCode/bin/Release/net9.0/DeadCode.dll profile `
    "$SAMPLE_APP/bin/Release/net9.0/SampleAppWithDeadCode" `
    --scenarios "$SAMPLE_APP/scenarios.json" `
    -o "$DEMO_DIR/traces"
Write-Host "✓ Profiling complete" -ForegroundColor Green

# Step 4: Analyze to find unused code
Write-Host "`nStep 4: Analyzing to identify unused code" -ForegroundColor Green
Write-Host "Comparing static inventory against runtime execution..."
dotnet DeadCode/bin/Release/net9.0/DeadCode.dll analyze `
    -i "$DEMO_DIR/inventory.json" `
    -t "$DEMO_DIR/traces" `
    -o "$DEMO_DIR/report.json" `
    --min-confidence medium
Write-Host "✓ Analysis complete" -ForegroundColor Green

# Step 5: Display results
Write-Host "`nStep 5: Results Summary" -ForegroundColor Green
$report = Get-Content "$DEMO_DIR/report.json" | ConvertFrom-Json

Write-Host "`nHigh Confidence Unused Methods:" -ForegroundColor Yellow
if ($report.highConfidence) {
    foreach ($method in $report.highConfidence) {
        Write-Host "  - $($method.file):$($method.line) - $($method.method)"
    }
} else {
    Write-Host "  None found"
}

Write-Host "`nMedium Confidence Unused Methods:" -ForegroundColor Yellow
if ($report.mediumConfidence) {
    foreach ($method in $report.mediumConfidence) {
        Write-Host "  - $($method.file):$($method.line) - $($method.method)"
    }
} else {
    Write-Host "  None found"
}

Write-Host "`nStatistics:" -ForegroundColor Yellow
$inventory = Get-Content "$DEMO_DIR/inventory.json" | ConvertFrom-Json
$totalMethods = $inventory.methods.Count
$highConf = if ($report.highConfidence) { $report.highConfidence.Count } else { 0 }
$medConf = if ($report.mediumConfidence) { $report.mediumConfidence.Count } else { 0 }
$lowConf = if ($report.lowConfidence) { $report.lowConfidence.Count } else { 0 }

Write-Host "  Total methods analyzed: $totalMethods"
Write-Host "  High confidence dead code: $highConf"
Write-Host "  Medium confidence dead code: $medConf"
Write-Host "  Low confidence dead code: $lowConf"

# Alternative: Run full pipeline in one command
Write-Host "`nAlternative: Run complete pipeline with one command" -ForegroundColor Green
Write-Host "You can also run the entire analysis with:"
Write-Host "dotnet DeadCode/bin/Release/net9.0/DeadCode.dll full --assemblies $SAMPLE_APP/bin/Release/net9.0/*.dll --executable $SAMPLE_APP/bin/Release/net9.0/SampleAppWithDeadCode" -ForegroundColor Blue

Write-Host "`nDemo complete!" -ForegroundColor Green
Write-Host "Check the $DEMO_DIR directory for all generated files:"
Get-ChildItem $DEMO_DIR

Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Review the report.json file for detailed dead code analysis"
Write-Host "2. Use the file:line references to navigate to unused code"
Write-Host "3. Feed the report to an LLM for automated cleanup suggestions"
Write-Host "4. Run on your own projects to find unused code!"