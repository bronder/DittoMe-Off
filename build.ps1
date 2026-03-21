# DittoMeOff Build Script
# Usage: .\build.ps1

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.2.0"
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

# Check if user wants to skip GitHub release (use --local-only flag)
$skipRelease = $false
if ($args -contains "--local-only") {
    $skipRelease = $true
}

if (-not $skipRelease) {
    Write-Host ""
    Write-Host "Creating GitHub release..." -ForegroundColor Cyan
    
    # Check if gh CLI is installed
    $ghAvailable = $null -ne (Get-Command gh -ErrorAction SilentlyContinue)
    if (-not $ghAvailable) {
        Write-Host "Error: GitHub CLI (gh) is not installed." -ForegroundColor Red
        Write-Host "Install from: https://cli.github.com/" -ForegroundColor Yellow
        exit 1
    }
    
    # Check if authenticated
    $ghAuth = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Not authenticated with GitHub. Run 'gh auth login' first." -ForegroundColor Red
        exit 1
    }
    
    # Get repo info
    $repo = gh repo view --json nameWithOwner -q .nameWithOwner 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Could not determine repository." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Uploading release to $repo..." -ForegroundColor Yellow
    
    # Create GitHub release (not draft - publishes immediately)
    gh release create "v$Version" `
        --title "DittoMeOff v$Version" `
        --notes-file "$projectDir\RELEASE.md" `
        "$zipPath"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "GitHub release published successfully!" -ForegroundColor Green
        Write-Host "View at: https://github.com/$repo/releases/tag/v$Version"
    } else {
        Write-Host "Error creating GitHub release." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "  Executable: $publishDir\DittoMeOff.exe"
Write-Host "  Zip archive: $zipPath"
Write-Host "  Output dir: $outputDir"
if (-not $skipRelease) {
    Write-Host "  GitHub release: Published"
}
