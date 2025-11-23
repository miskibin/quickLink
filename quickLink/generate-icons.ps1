# Generate all required icon sizes from quicklink.png (falls back to logo.png)
# This script prefers the quicklink.png file in the Assets folder; logo.png is used as a fallback for older setups

$assetsPath = Join-Path $PSScriptRoot "Assets"
$preferredLogo = "quicklink.png"
$fallbackLogo = "logo.png"

# Prefer quicklink.png, but fall back to logo.png for compatibility
$sourceCandidates = @((Join-Path $assetsPath $preferredLogo), (Join-Path $assetsPath $fallbackLogo))
$sourceLogo = $null
foreach ($candidate in $sourceCandidates) {
    if (Test-Path $candidate) { $sourceLogo = Resolve-Path $candidate; break }
}

if (-not $sourceLogo) {
    Write-Error "Neither '$preferredLogo' nor '$fallbackLogo' found in the Assets folder. Please add '$preferredLogo' to $assetsPath and re-run the script."
    exit 1
}

Write-Host "Generating icons from '$preferredLogo' (or '$fallbackLogo' fallback) ..." -ForegroundColor Green

# Load System.Drawing assembly for image processing
Add-Type -AssemblyName System.Drawing

# Load the source image
$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $sourceLogo))

function Resize-Image {
    param(
        [System.Drawing.Image]$Image,
        [int]$Width,
        [int]$Height,
        [string]$OutputPath
    )
    
    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # High quality resize
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    
    $graphics.DrawImage($Image, 0, 0, $Width, $Height)
    
    # Ensure directory exists
    $dir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    
    $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $graphics.Dispose()
    
    Write-Host "  Created: $OutputPath" -ForegroundColor Cyan
}

# Generate all required sizes
Write-Host "`nGenerating UWP/WinUI assets..." -ForegroundColor Yellow

# Square logos
Resize-Image -Image $sourceImage -Width 44 -Height 44 -OutputPath (Join-Path $assetsPath "Square44x44Logo.scale-100.png")
Resize-Image -Image $sourceImage -Width 88 -Height 88 -OutputPath (Join-Path $assetsPath "Square44x44Logo.scale-200.png")
Resize-Image -Image $sourceImage -Width 24 -Height 24 -OutputPath (Join-Path $assetsPath "Square44x44Logo.targetsize-24_altform-unplated.png")
Resize-Image -Image $sourceImage -Width 16 -Height 16 -OutputPath (Join-Path $assetsPath "Square44x44Logo.targetsize-16_altform-unplated.png")
Resize-Image -Image $sourceImage -Width 32 -Height 32 -OutputPath (Join-Path $assetsPath "Square44x44Logo.targetsize-32_altform-unplated.png")
Resize-Image -Image $sourceImage -Width 48 -Height 48 -OutputPath (Join-Path $assetsPath "Square44x44Logo.targetsize-48_altform-unplated.png")
Resize-Image -Image $sourceImage -Width 256 -Height 256 -OutputPath (Join-Path $assetsPath "Square44x44Logo.targetsize-256_altform-unplated.png")

# Medium tile
Resize-Image -Image $sourceImage -Width 150 -Height 150 -OutputPath (Join-Path $assetsPath "Square150x150Logo.scale-100.png")
Resize-Image -Image $sourceImage -Width 300 -Height 300 -OutputPath (Join-Path $assetsPath "Square150x150Logo.scale-200.png")

# Wide tile
Resize-Image -Image $sourceImage -Width 310 -Height 150 -OutputPath (Join-Path $assetsPath "Wide310x150Logo.scale-100.png")
Resize-Image -Image $sourceImage -Width 620 -Height 300 -OutputPath (Join-Path $assetsPath "Wide310x150Logo.scale-200.png")

# Store logo
Resize-Image -Image $sourceImage -Width 50 -Height 50 -OutputPath (Join-Path $assetsPath "StoreLogo.scale-100.png")
Resize-Image -Image $sourceImage -Width 100 -Height 100 -OutputPath (Join-Path $assetsPath "StoreLogo.scale-200.png")

# Splash screen (can be adjusted to include padding if needed)
Resize-Image -Image $sourceImage -Width 620 -Height 300 -OutputPath (Join-Path $assetsPath "SplashScreen.scale-100.png")
Resize-Image -Image $sourceImage -Width 1240 -Height 600 -OutputPath (Join-Path $assetsPath "SplashScreen.scale-200.png")

# Lock screen logo
Resize-Image -Image $sourceImage -Width 24 -Height 24 -OutputPath (Join-Path $assetsPath "LockScreenLogo.scale-100.png")
Resize-Image -Image $sourceImage -Width 48 -Height 48 -OutputPath (Join-Path $assetsPath "LockScreenLogo.scale-200.png")

# Application icon (for exe)
Resize-Image -Image $sourceImage -Width 256 -Height 256 -OutputPath (Join-Path $assetsPath "app.ico.png")

$sourceImage.Dispose()

Write-Host "`nNow converting app.ico.png to app.ico..." -ForegroundColor Yellow

# Create ICO file with multiple sizes
$icoSizes = @(16, 32, 48, 256)
$icoPath = Join-Path $assetsPath "app.ico"

# For ICO creation, we'll use a simple approach - create individual PNGs and mention manual conversion
Write-Host "`nGenerated PNG files. To create app.ico with multiple sizes:" -ForegroundColor Yellow
Write-Host "You can use an online tool or ImageMagick with this command:" -ForegroundColor Cyan
Write-Host "magick convert Assets\Square44x44Logo.targetsize-16_altform-unplated.png Assets\Square44x44Logo.targetsize-32_altform-unplated.png Assets\Square44x44Logo.targetsize-48_altform-unplated.png Assets\Square44x44Logo.targetsize-256_altform-unplated.png Assets\app.ico" -ForegroundColor Gray

Write-Host "`n✓ Icon generation complete!" -ForegroundColor Green
Write-Host "All required PNG assets have been created in the Assets folder." -ForegroundColor Green
