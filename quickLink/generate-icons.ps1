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

# Create ICO file with multiple sizes automatically
$icoPath = Join-Path $assetsPath "app.ico"

function Convert-PngsToIco {
    param(
        [string[]]$PngFiles,
        [string]$OutputIco
    )

    # Ensure we have at least one PNG
    $existing = $PngFiles | Where-Object { Test-Path $_ }
    if (-not $existing) { return $false }

    Add-Type -AssemblyName System.IO

    $images = @()
    foreach ($f in $existing) {
        try {
            $img = [System.Drawing.Image]::FromFile($f)
            $imgInfo = [PSCustomObject]@{
                Path = $f
                Width = $img.Width
                Height = $img.Height
                Bytes = [System.IO.File]::ReadAllBytes($f)
            }
            $images += $imgInfo
            $img.Dispose()
        }
        catch {
            Write-Host "Skipping invalid image: $f" -ForegroundColor Yellow
        }
    }

    if (-not $images) { return $false }

    # Build ICO file. ICO header: 6 bytes + 16 bytes per image entry
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # ICONDIR: reserved(2) type(2) count(2)
    $bw.Write([uint16]0)
    $bw.Write([uint16]1) # 1 for icons
    $bw.Write([uint16]$images.Count)

    $currentOffset = 6 + 16 * $images.Count

    foreach ($img in $images) {
        $w = $img.Width
        $h = $img.Height

        # Width/Height bytes: 0 means 256
        # For width/height values, 0 means 256 in ICO format
        $byteWidth = if ($w -ge 256) { 0 } else { [byte]$w }
        $byteHeight = if ($h -ge 256) { 0 } else { [byte]$h }
        $bw.Write([byte]$byteWidth)
        $bw.Write([byte]$byteHeight)

        # Color count & reserved
        $bw.Write([byte]0)
        $bw.Write([byte]0)

        # For PNG icons, planes and bit count are typically 0
        $bw.Write([uint16]0)
        $bw.Write([uint16]32)

        # size of image data
        $data = $img.Bytes
        $bw.Write([uint32]$data.Length)

        # offset to image data
        $bw.Write([uint32]$currentOffset)

        $currentOffset += $data.Length
    }

    # write image data
    foreach ($img in $images) {
        $bw.Write($img.Bytes)
    }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($OutputIco, $ms.ToArray())
    $bw.Dispose(); $ms.Dispose()

    return $true
}

# Candidate PNGs used for the icon resource (prefer exact targetsize images when present)
$icoPngs = @(
    (Join-Path $assetsPath "Square44x44Logo.targetsize-16_altform-unplated.png"),
    (Join-Path $assetsPath "Square44x44Logo.targetsize-32_altform-unplated.png"),
    (Join-Path $assetsPath "Square44x44Logo.targetsize-48_altform-unplated.png"),
    (Join-Path $assetsPath "Square44x44Logo.targetsize-256_altform-unplated.png")
)

if (Convert-PngsToIco -PngFiles $icoPngs -OutputIco $icoPath) {
    Write-Host "Created ICO: $icoPath" -ForegroundColor Green
} else {
    Write-Host "Could not create ICO automatically; you can create it manually with ImageMagick." -ForegroundColor Yellow
}

Write-Host "`n✓ Icon generation complete!" -ForegroundColor Green
Write-Host "All required PNG assets have been created in the Assets folder." -ForegroundColor Green
