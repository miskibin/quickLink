# QuickLink MSI packaging script (WiX v5).
# Requires: .NET 8 SDK. WiX is installed automatically as a global tool if missing.
# Run from repository root:  .\build-msi.ps1 [version]

$ErrorActionPreference = "Stop"
$ProjectPath = "quickLink\quickLink.csproj"
$WxsPath = "installer\Package.wxs"

# Version from first arg, else default
$Version = $args[0]
if (-not $Version) { $Version = "1.0.0" }

$PublishDir = "quickLink\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish"
$OutputMsi = "QuickLink-$Version-x64.msi"

# Publish the self-contained, single-file build (same output the ZIP uses)
Write-Host "Publishing QuickLink (Release, win-x64, self-contained)..." -ForegroundColor Cyan
dotnet publish $ProjectPath -c Release -r win-x64 -p:Platform=x64 -p:PublishSingleFile=true --self-contained
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Ensure WiX is available
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    Write-Host "Installing WiX global tool..." -ForegroundColor Yellow
    dotnet tool install --global wix --version 5.0.2
    $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
}

# Build the MSI
Write-Host "Building MSI..." -ForegroundColor Cyan
$AbsPublishDir = (Resolve-Path $PublishDir).Path
wix build $WxsPath -arch x64 -d Version=$Version -d PublishDir="$AbsPublishDir" -out $OutputMsi
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Output: $OutputMsi" -ForegroundColor Green
Write-Host "Install by double-clicking the MSI (no admin required)." -ForegroundColor Yellow
