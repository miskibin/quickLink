param(
    [string]$Version = "1.0.0.0",
    [string]$Configuration = "Release",
    [switch]$Clean = $false,
    [switch]$SkipPublish = $false
)

Write-Host "QuickLink Installer Build Script (x64 Only)" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green

# Step 1: Clean if requested
if ($Clean) {
    Write-Host "`nStep 1: Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "bin") { 
        Remove-Item "bin" -Recurse -Force 
        Write-Host "  ✓ Cleaned bin folder" -ForegroundColor Green
    }
    if (Test-Path "obj") { 
        Remove-Item "obj" -Recurse -Force 
        Write-Host "  ✓ Cleaned obj folder" -ForegroundColor Green
    }
    if (Test-Path "../publish") { 
        Remove-Item "../publish" -Recurse -Force 
        Write-Host "  ✓ Cleaned publish folder" -ForegroundColor Green
    }
}

# Step 2: Publish QuickLink app (x64)
if (-not $SkipPublish) {
    Write-Host "`nStep 2: Publishing QuickLink app (x64)..." -ForegroundColor Yellow
    $publishArgs = @(
        "publish", 
        "../QuickLink/QuickLink.csproj",
        "-c", $Configuration,
        "--self-contained", "true",
        "-r", "win-x64",
        "-o", "../publish",
        "-p:PublishSingleFile=false",
        "-p:PublishReadyToRun=false",
        "-p:Platform=x64"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ Failed to publish QuickLink app" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "  ✓ QuickLink app published successfully (x64)" -ForegroundColor Green
}

# Step 3: Check published files
Write-Host "`nStep 3: Verifying published files..." -ForegroundColor Yellow
if (-not (Test-Path "../publish/QuickLink.exe")) {
    Write-Host "  ✗ QuickLink.exe not found in publish folder" -ForegroundColor Red
    Write-Host "  Available files:" -ForegroundColor Yellow
    if (Test-Path "../publish") {
        Get-ChildItem "../publish" | ForEach-Object { Write-Host "    $($_.Name)" -ForegroundColor Gray }
    } else {
        Write-Host "    Publish folder does not exist!" -ForegroundColor Red
    }
    exit 1
}
Write-Host "  ✓ QuickLink.exe found" -ForegroundColor Green

# Step 4: Build MSI installer (x64)
Write-Host "`nStep 4: Building MSI installer (x64)..." -ForegroundColor Yellow

# Create bin directory if it doesn't exist
if (-not (Test-Path "bin")) {
    New-Item -ItemType Directory -Path "bin" | Out-Null
}

$buildArgs = @(
    "build",
    "Package.wxs",
    "-ext", 
    "WixToolset.UI.wixext",
    "-arch",
    "x64",
    "-d", 
    "Version=$Version",
    "-bindpath",
    "bin=..\publish",
    "-o", 
    "bin/QuickLink-$Version.msi"
)

& wix @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Failed to build MSI installer" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "  ✓ MSI installer built successfully (x64)" -ForegroundColor Green
Write-Host "`nBuild completed!" -ForegroundColor Green
Write-Host "Output: $(Get-Location)\bin\QuickLink-$Version.msi" -ForegroundColor Cyan

# Step 5: Show file info
$msiPath = "bin/QuickLink-$Version.msi"
if (Test-Path $msiPath) {
    $fileInfo = Get-Item $msiPath
    Write-Host "`nInstaller Details:" -ForegroundColor Yellow
    Write-Host "  File: $($fileInfo.Name)" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor White
    Write-Host "  Created: $($fileInfo.CreationTime)" -ForegroundColor White
    Write-Host "  Architecture: x64 only" -ForegroundColor White
}
