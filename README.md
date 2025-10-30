# quickLink

A WinUI 3 desktop application for Windows.

## Building

This project uses .NET 8 and requires Windows to build.

### Prerequisites

- Visual Studio 2022 with the following workloads:
  - .NET Desktop Development
  - Universal Windows Platform development
- .NET 8 SDK
- Windows 10 SDK (10.0.19041.0 or later)

### Build Instructions

```powershell
# Restore dependencies
dotnet restore quickLink/quickLink.csproj

# Build the project
dotnet build quickLink/quickLink.csproj -c Release -p:Platform=x64

# Publish the project (creates MSIX package)
dotnet publish quickLink/quickLink.csproj -c Release -p:Platform=x64
```

## Continuous Deployment

This repository includes a GitHub Actions workflow (`.github/workflows/cd.yml`) that automatically builds and packages the application for all supported platforms (x64, x86, ARM64).

### Workflow Triggers

The CD workflow runs on:
- Push to the `master` branch
- Pull requests targeting the `master` branch

### Build Artifacts

After a successful build, the workflow uploads artifacts containing:
- Compiled application binaries
- MSIX packages (if MSIX packaging is enabled)
- All platform-specific files

Artifacts are named: `quickLink-{platform}-{configuration}`

### Code Signing (Optional)

To sign your MSIX packages, you need to configure two repository secrets:

1. **Base64_Encoded_Pfx**: Your code signing certificate encoded in Base64
2. **Pfx_Key**: The password for your certificate

#### How to Create and Configure Secrets

1. Generate or obtain a code signing certificate (.pfx file)

2. Encode the certificate to Base64:
   ```powershell
   $pfx_cert = Get-Content '.\YourCertificate.pfx' -Encoding Byte
   [System.Convert]::ToBase64String($pfx_cert) | Out-File 'EncodedCertificate.txt'
   ```

3. Add the secrets to your GitHub repository:
   - Go to Settings > Secrets and variables > Actions
   - Click "New repository secret"
   - Add `Base64_Encoded_Pfx` with the contents of `EncodedCertificate.txt`
   - Add `Pfx_Key` with your certificate password

The workflow will automatically detect these secrets and sign your packages if they are present.

## Platforms

This application supports the following platforms:
- x64 (64-bit Intel/AMD)
- x86 (32-bit Intel/AMD)
- ARM64 (ARM 64-bit)

## License

[Add your license information here]
