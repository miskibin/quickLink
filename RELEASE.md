# Release Process

This document describes how to create a new QuickLink release with automatic MSI installer build.

## Creating a Release

### 1. Update Version (if needed)

Update version numbers in:
- `quickLink/quickLink.csproj` - `<Version>` element
- `quickLink/Package.appxmanifest` - `Version` attribute

### 2. Commit Changes

```bash
git add .
git commit -m "Release v1.0.3.0"
git push origin master
```

### 3. Create and Push Tag

```bash
# Create version tag (format: v1.0.3.0 or v1.0.3)
git tag v1.0.3.0

# Push tag to GitHub
git push origin v1.0.3.0
```

### 4. Automatic Build

GitHub Actions will automatically:
1. ✅ Build the QuickLink app (x64, self-contained)
2. ✅ Create MSI installer using WiX v6
3. ✅ Create GitHub Release
4. ✅ Upload MSI as release asset

### 5. Monitor Build

1. Go to **Actions** tab in GitHub
2. Watch the "Build MSI Installer" workflow
3. Build takes ~3-5 minutes

### 6. Verify Release

1. Go to **Releases** tab in GitHub
2. Download the MSI file
3. Test installation on a clean Windows machine

## Version Format

Use semantic versioning with 4 parts:
- `v1.0.0.0` - Major.Minor.Patch.Build
- `v1.0.3` - Also supported (adds .0 automatically)

## Troubleshooting

### Build Fails

**Check Actions log**:
1. Go to Actions tab
2. Click on failed workflow
3. Review error messages

**Common issues**:
- WiX compilation errors → Check `Package.wxs` syntax
- .NET publish errors → Check `quickLink.csproj` configuration
- Missing files → Ensure all files are committed

### Re-running a Build

If a build fails:
1. Fix the issue
2. Delete the tag locally and remotely:
   ```bash
   git tag -d v1.0.3.0
   git push origin :refs/tags/v1.0.3.0
   ```
3. Create and push the tag again

## Manual Build (Local)

If needed, you can build locally:

```powershell
cd quickLink.Installer
.\build.ps1 -Version "1.0.3.0" -Clean
```

MSI will be in `quickLink.Installer/bin/`

## Release Checklist

- [ ] Version updated in project files
- [ ] Changes committed and pushed
- [ ] Tag created and pushed
- [ ] GitHub Action completed successfully
- [ ] Release created on GitHub
- [ ] MSI file attached to release
- [ ] MSI tested on clean Windows machine
- [ ] Release notes reviewed
- [ ] Users notified (if applicable)
