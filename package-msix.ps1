# QuickLink MSIX packaging script
# Requires: winapp CLI (winget install Microsoft.WinAppCli)
# Run from repository root.

$ErrorActionPreference = "Stop"
$ProjectPath = "quickLink\quickLink.csproj"
$ManifestPath = "quickLink\Package.appxmanifest"
$CertPath = "quickLink\devcert.pfx"
$CertPassword = "password"

# Optional: set version from first argument or read from manifest
$Version = $args[0]
if (-not $Version) {
    $manifestXml = [xml](Get-Content $ManifestPath -Raw)
    $Version = $manifestXml.Package.Identity.Version
}
if (-not $Version) { $Version = "1.0.0.0" }

$OutputDir = "AppPackages"
$OutputMsix = "QuickLink-$Version-x64.msix"

# Use MSBuild native MSIX generation for proper XAML/resources.pri compilation
Write-Host "Building QuickLink MSIX (Release, win-x64, self-contained)..." -ForegroundColor Cyan
dotnet build $ProjectPath -c Release -r win-x64 `
    -p:Platform=x64 `
    -p:PublishSingleFile=false `
    -p:WindowsPackageType=MSIX `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageDir="$PWD\$OutputDir\" `
    -p:AppxBundle=Never `
    -p:AppxPackageSigningEnabled=false `
    --self-contained
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Find the generated MSIX
$GeneratedMsix = Get-ChildItem "$OutputDir\*\*.msix" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $GeneratedMsix) {
    Write-Error "No MSIX found in $OutputDir"
    exit 1
}

# Generate certificate if missing
if (-not (Test-Path $CertPath)) {
    Write-Host "Development certificate not found. Generating..." -ForegroundColor Yellow
    Push-Location quickLink
    winapp cert generate --manifest Package.appxmanifest --output devcert.pfx --if-exists Skip
    Pop-Location
    Write-Host "Run 'winapp cert install quickLink\devcert.pfx' as Administrator to trust the certificate." -ForegroundColor Yellow
}

# Sign the package
Write-Host "Signing MSIX..." -ForegroundColor Cyan
winapp sign $GeneratedMsix.FullName $CertPath --password $CertPassword
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Copy to repo root with clean name
Copy-Item $GeneratedMsix.FullName -Destination $OutputMsix -Force
Write-Host "Done. Output: $OutputMsix" -ForegroundColor Green
Write-Host "Install with: Add-AppPackage -Path $OutputMsix" -ForegroundColor Yellow
