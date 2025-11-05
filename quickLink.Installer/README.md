# QuickLink Installer

WiX Toolset v6 installer for QuickLink - A fast, elegant link, clipboard, and commands manager for Windows.

## Prerequisites

- **WiX Toolset v6+** - Install via `dotnet tool install --global wix`
- **.NET 8 SDK**
- **PowerShell** (for build scripts)

## Project Structure

```
quickLink.Installer/
├── Package.wxs          # Main installer definition (uses WiX v6 Files element)
├── license.rtf          # End-user license agreement
├── build.ps1            # Build automation script
└── .vscode/
    ├── tasks.json       # VS Code build tasks
    └── settings.json    # XML formatting settings
```

## Key Features

- **Automatic File Harvesting**: Uses WiX v6 `<Files>` element - no manual file listing needed
- **x64 Only**: Optimized for 64-bit Windows systems
- **Self-Contained**: Includes all .NET runtime dependencies
- **Full Directory Structure**: Automatically includes all subdirectories (language packs, resources, etc.)
- **Modern WiX v6**: Simplified authoring compared to older WiX versions

## Quick Start

### VS Code Tasks (Recommended)

Press **Ctrl+Shift+B** and select:
- `Build MSI Installer` - Quick build with default version
- `Build Versioned MSI` - Build with custom version
- `Full Build Process` - Clean, publish, and build

### PowerShell Script

```powershell
# Basic build (version 1.0.0.0)
.\build.ps1

# Build with specific version
.\build.ps1 -Version "1.0.3.0"

# Clean build
.\build.ps1 -Version "1.0.3.0" -Clean

# Skip app publishing (if already published)
.\build.ps1 -SkipPublish
```

### Manual Commands

```powershell
# 1. Publish the app
dotnet publish ../quickLink/quickLink.csproj -c Release --self-contained -r win-x64 -o ../publish -p:Platform=x64

# 2. Build installer
wix build Package.wxs -ext WixToolset.UI.wixext -arch x64 -bindpath bin=..\publish -d Version=1.0.0.0 -o bin/QuickLink.msi
```

## WiX v6 Files Element

This installer uses WiX v6's modern `<Files>` element for automatic directory harvesting:

```xml
<Files Include="!(bindpath.bin)**" Directory="INSTALLFOLDER" />
```

This single line replaces hundreds of manual `<Component>` and `<File>` elements, automatically:
- Includes all files from the publish directory
- Preserves full subdirectory structure
- Handles language packs and resource folders
- Updates automatically when files change

## Build Output

- MSI installers: `bin/QuickLink-{version}.msi`
- Typical size: ~67 MB (includes .NET runtime)
- Published files: `../publish/` (temporary)

## Installer Features

- **Architecture**: x64 only
- **Installation Path**: `C:\Program Files\miskibin\QuickLink`
- **Start Menu Shortcut**: ✓
- **Desktop Shortcut**: ✓
- **Auto-start with Windows**: ✓
- **Major Upgrade Support**: Automatic replacement of older versions
- **UI**: Full wizard with license agreement

## GitHub Actions CI

Automated MSI builds are triggered on Git tags. See `.github/workflows/build-installer.yml`.

To create a release:
```bash
git tag v1.0.3.0
git push origin v1.0.3.0
```

This will automatically build and upload the MSI as a GitHub release asset.

## Troubleshooting

### Common Issues

**Build fails with "bindpath not found"**:
- Run `.\build.ps1` to publish the app first
- Or manually publish: `dotnet publish ../quickLink/quickLink.csproj -r win-x64 -o ../publish`

**WiX not found**:
- Install: `dotnet tool install --global wix`
- Verify: `wix --version` should show v6.x

**App crashes after installation**:
- Ensure WiX v6 is used (not v4/v5)
- Verify the `<Files>` element includes `!(bindpath.bin)**`
- Check that subdirectories (language packs) are present in the MSI

## Customization

### Change Version
```powershell
.\build.ps1 -Version "2.0.0.0"
```

### Modify Features
Edit `Package.wxs`:
- Remove auto-start: Delete `StartupRegistry` component
- Remove desktop shortcut: Delete `DesktopShortcut` component
- Change install location: Modify `<Directory>` structure

### Update License
Edit `license.rtf` with your license text (keep RTF formatting).

## Architecture

**x64 Only Configuration**:
- Uses `ProgramFiles64Folder` standard directory
- WiX build uses `-arch x64` flag
- .NET publish targets `win-x64` runtime
- Will not install on 32-bit Windows

## Development Workflow

1. Make changes to QuickLink app
2. Press **Ctrl+Shift+B** → select build task
3. Test the generated MSI in `bin/`
4. Iterate as needed

## Learn More

- [WiX v6 Documentation](https://docs.firegiant.com/wix/)
- [Files Element Reference](https://docs.firegiant.com/wix/schema/wxs/files/)
- [WiX v6 What's New](https://docs.firegiant.com/wix/whatsnew/)
