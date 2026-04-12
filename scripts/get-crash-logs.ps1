# QuickLink: find crash logs and recent Application errors
$ErrorActionPreference = "Continue"

Write-Host "=== 1. AppData\QuickLink\crash.log ===" -ForegroundColor Cyan
$crashPath = Join-Path $env:APPDATA "QuickLink\crash.log"
if (Test-Path $crashPath) {
    Get-Content $crashPath
} else {
    Write-Host "(not found)"
}

Write-Host "`n=== 2. MSIX package folder (any .log files) ===" -ForegroundColor Cyan
$packages = Join-Path $env:LOCALAPPDATA "Packages"
$pkg = Get-ChildItem $packages -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -like "1b30b1de*" }
if ($pkg) {
    $logs = Get-ChildItem $pkg.FullName -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq ".log" }
    if ($logs) { $logs | ForEach-Object { Write-Host $_.FullName; Get-Content $_.FullName -ErrorAction SilentlyContinue } }
    else { Write-Host "(no .log files)" }
} else {
    Write-Host "(package folder not found)"
}

Write-Host "`n=== 3. Last 10 Application log errors (Error level) ===" -ForegroundColor Cyan
Get-WinEvent -LogName Application -MaxEvents 200 -ErrorAction SilentlyContinue |
    Where-Object { $_.Level -eq 2 } |
    Select-Object -First 10 |
    ForEach-Object {
        Write-Host "---"
        Write-Host "Time: $($_.TimeCreated) | Id: $($_.Id) | Provider: $($_.ProviderName)"
        Write-Host $_.Message
    }

Write-Host "`n=== 4. Recent .NET Runtime / Application Error events ===" -ForegroundColor Cyan
Get-WinEvent -LogName Application -MaxEvents 300 -ErrorAction SilentlyContinue |
    Where-Object { $_.ProviderName -match "\.NET|Application Error|Windows Error" } |
    Select-Object -First 5 |
    ForEach-Object {
        Write-Host "---"
        Write-Host "Time: $($_.TimeCreated) | Id: $($_.Id) | Provider: $($_.ProviderName)"
        Write-Host $_.Message
    }
