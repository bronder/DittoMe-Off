# DittoMe-Off Build Script
# Usage: .\build.ps1 [-SkipBumpVersion] [--local-only]

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.4.0",
    [switch]$SkipBumpVersion
)

$ErrorActionPreference = "Stop"

$projectDir = $PSScriptRoot
$srcDir = Join-Path $projectDir "src\DittoMe-Off"
$csprojPath = Join-Path $srcDir "DittoMeOff.csproj"
$readmePath = Join-Path $projectDir "README.md"
$releasePath = Join-Path $projectDir "RELEASE.md"
$outputDir = Join-Path $projectDir "output"
$publishDir = Join-Path $srcDir "bin\$Configuration\net10.0-windows\publish"

# Function to bump version by 0.1
function Bump-Version {
    Write-Host "Bumping version..." -ForegroundColor Cyan
    
    # Read current version from csproj
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<Version>([\d.]+)</Version>') {
        $currentVersion = $Matches[1]
        Write-Host "Current version: $currentVersion" -ForegroundColor Yellow
        
        # Parse version and increment by 0.1
        $versionParts = $currentVersion -split '\.'
        $minor = [int]$versionParts[-1]
        $minor++
        $newVersion = ($versionParts[0..($versionParts.Length-2)] -join '.') + ".$minor"
        
        Write-Host "New version: $newVersion" -ForegroundColor Green
        
        # Update csproj
        $csprojContent = $csprojContent -replace "<Version>$currentVersion</Version>", "<Version>$newVersion</Version>"
        Set-Content -Path $csprojPath -Value $csprojContent
        Write-Host "Updated DittoMeOff.csproj" -ForegroundColor Green
        
        # Update build.ps1 version
        $scriptContent = Get-Content $PSCommandPath -Raw
        $scriptContent = $scriptContent -replace '(?<=Version = ")[\d.]+(?=")', $newVersion
        Set-Content -Path $PSCommandPath -Value $scriptContent
        Write-Host "Updated build.ps1" -ForegroundColor Green
        
        # Read current RELEASE.md content
        $releaseContent = Get-Content $releasePath -Raw
        
        # Create new release template
        $newReleaseTemplate = @"
# Release v$newVersion

## What's New

_(Add your release notes here)_

---

## Previous Release (v$currentVersion)

$releaseContent

---

**Full Changelog**: https://github.com/bronder/DittoMe-Off/commits
"@
        
        Set-Content -Path $releasePath -Value $newReleaseTemplate
        Write-Host "Updated RELEASE.md with new template and previous release archived" -ForegroundColor Green
        
        # Update README.md version reference if exists
        if (Test-Path $readmePath) {
            $readmeContent = Get-Content $readmePath -Raw
            if ($readmeContent -match 'v[\d.]+') {
                $readmeContent = $readmeContent -replace 'v[\d.]+', "v$newVersion"
                Set-Content -Path $readmePath -Value $readmeContent
                Write-Host "Updated README.md" -ForegroundColor Green
            }
        }
        
        Write-Host ""
        Write-Host "Version bumped to $newVersion" -ForegroundColor Green
        
        # Update the $Version variable for this run
        return $newVersion
    } else {
        Write-Host "Error: Could not find version in csproj" -ForegroundColor Red
        exit 1
    }
}

# Check if bumping version (default is to bump)
if (-not $SkipBumpVersion) {
    $newVer = Bump-Version
    $Version = $newVer
}

$zipName = "DittoMe-Off-v$Version-win-x64.zip"
$zipPath = Join-Path $projectDir $zipName

Write-Host "Building DittoMe-Off v$Version..." -ForegroundColor Cyan

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
        --title "DittoMe-Off v$Version" `
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
Write-Host "  Executable: $publishDir\DittoMe-Off.exe"
Write-Host "  Zip archive: $zipPath"
Write-Host "  Output dir: $outputDir"
if (-not $skipRelease) {
    Write-Host "  GitHub release: Published"
}
