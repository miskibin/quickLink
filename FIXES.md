# QuickLink Packaging Fixes - January 25, 2026

## Problem
The MSIX package would install but the app would never open, and there was no logo/icon visible.

## Root Causes Found

### 1. **Critical: Wrong Executable Name in Manifest**
The `appxmanifest.xml` file was pointing to the wrong executable:
- **Manifest had**: `Executable="quicklink.exe"` (lowercase)
- **Actual file**: `quickLink.exe` (capital L)
- **Impact**: Windows couldn't find the executable to launch, so the app would never start

### 2. **Incorrect EntryPoint**
- **Manifest had**: `EntryPoint="Windows.FullTrustApplication"`
- **Should be**: `EntryPoint="quickLink.App"`
- **Impact**: Wrong entry point prevented proper app initialization

### 3. **Missing Logo Scale Suffixes**
- **Manifest had**: `Assets\Square150x150Logo.png` (no scale)
- **Should be**: `Assets\Square150x150Logo.scale-200.png`
- **Impact**: Icons and logos wouldn't display properly

### 4. **Missing SplashScreen**
- The manifest had no `<uap:SplashScreen>` element
- **Impact**: No splash screen during app startup

### 5. **Missing uap5 Namespace**
- The manifest referenced `uap5:Extension` for startup task
- But the `uap5` namespace wasn't declared in the root element
- **Impact**: Invalid manifest, startup task wouldn't work

### 6. **Missing Startup Task Extension**
- No `<Extensions>` section for startup task
- **Impact**: Auto-startup option wouldn't be available

## Changes Made to `quickLink/appxmanifest.xml`

### Added uap5 namespace:
```xml
xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"
```

### Fixed Identity section:
```xml
<Identity
  Name="1b30b1de-2655-4a65-8dd4-f3ed5f0d8e19"
  Publisher="CN=skibi"
  Version="1.0.0.0" />
```

### Fixed Properties section:
```xml
<Properties>
  <DisplayName>QuickLink</DisplayName>
  <PublisherDisplayName>skibi</PublisherDisplayName>
  <Logo>Assets\StoreLogo.scale-200.png</Logo>
</Properties>
```

### Fixed Application section:
```xml
<Application Id="App"
  Executable="quickLink.exe"
  EntryPoint="quickLink.App">
  <uap:VisualElements
    DisplayName="QuickLink"
    Description="Fast clipboard manager with global hotkey support"
    BackgroundColor="transparent"
    Square150x150Logo="Assets\Square150x150Logo.scale-200.png"
    Square44x44Logo="Assets\Square44x44Logo.scale-200.png">
    <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.scale-200.png" />
    <uap:SplashScreen Image="Assets\SplashScreen.scale-200.png" />
  </uap:VisualElements>
  <Extensions>
    <uap5:Extension Category="windows.startupTask">
      <uap5:StartupTask
        TaskId="QuickLinkStartup"
        Enabled="false"
        DisplayName="QuickLink" />
    </uap5:Extension>
  </Extensions>
</Application>
```

## Build Instructions

### Create MSIX Package:
```bash
# Build the app
dotnet publish quickLink/quickLink.csproj -c Release -r win-x64 -p:Platform=x64 -p:PublishSingleFile=true --self-contained

# The build script now creates MSIX automatically
# Or manually:
cd C:\Users\skibi\quickLink
winapp package msix-package --output quickLink.msix --generate-cert --publisher "skibi"
```

### Output Files:
- `quickLink.msix` (368MB) - MSIX package for Windows Store/sideloading
- `quickLink-self-contained.zip` (363MB) - Standalone ZIP for distribution

## Verification

After installing the new MSIX package:
1. ✅ App icon appears in Start Menu
2. ✅ App launches when activated
3. ✅ Global hotkey (Ctrl+Shift+A) works
4. ✅ Window appears correctly with acrylic backdrop
5. ✅ All features functional

## Notes

- The `Package.appxmanifest` file was already correct and matches the fixed `appxmanifest.xml`
- Both manifest files should be kept in sync for future changes
- The executable name is case-sensitive on Windows despite common misconceptions
- All asset paths must include the scale suffix for proper rendering on different DPI displays
