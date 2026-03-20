# DittoMeOff Build Script
# Usage: .\build.ps1

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$projectDir = $PSScriptRoot
$srcDir = Join-Path $projectDir "src\DittoMeOff"
$outputDir = Join-Path $projectDir "output"
$publishDir = Join-Path $srcDir "bin\$Configuration\net10.0-windows\publish"
$zipName = "DittoMeOff-v$Version-win-x64.zip"
$zipPath = Join-Path $projectDir $zipName

Write-Host "Building DittoMeOff v$Version..." -ForegroundColor Cyan

# Clean output directory
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

# Build and publish
Write-Host "Building $Configuration..." -ForegroundColor Yellow
Push-Location $srcDir
try {
    dotnet publish -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
}
finally {
    Pop-Location
}

# Check if publish directory exists
if (-not (Test-Path $publishDir)) {
    Write-Host "Error: Publish directory not found at $publishDir" -ForegroundColor Red
    exit 1
}

# Create zip
Write-Host "Creating zip archive..." -ForegroundColor Yellow
Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

# Copy to output directory too
Copy-Item $zipPath $outputDir

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "  Executable: $publishDir\DittoMeOff.exe"
Write-Host "  Zip archive: $zipPath"
Write-Host "  Output dir: $outputDir"
