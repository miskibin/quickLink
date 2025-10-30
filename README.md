# QuickLink

A fast clipboard manager for Windows with global hotkey support.

<img width="1204" height="661" alt="image" src="https://github.com/user-attachments/assets/e77721f2-d155-478d-9589-591631a05c15" />

![2025-10-30 20-00-10](https://github.com/user-attachments/assets/cc0d0edc-f90a-407b-b23c-133546bc0099)

## Download

Get the latest release from the [Releases page](https://github.com/miskibin/quickLink/releases).

Download the **x64** version for Windows 10/11 (64-bit).

## Features

- **Global Hotkey Access** - Press your custom hotkey (default: Ctrl+Space) to instantly open QuickLink from anywhere
- **Quick Search** - Type to filter through your saved items in real-time
- **Clipboard Management** - Save frequently used text, links, and snippets for instant access
- **One-Click Copy** - Click any item to automatically copy it to your clipboard
- **URL Launcher** - Double-click URLs to open them directly in your browser
- **Customizable Shortcuts** - Configure your own hotkey combination (supports modifiers like Ctrl, Shift, Alt)
- **Persistent Storage** - Your items are saved locally and available every time you open the app
- **Add & Edit** - Easily add new items or edit existing ones with name and value fields
- **Encryption Support** - Sensitive values can be encrypted for security
- **Clean Interface** - Simple, focused design with WinUI 3 modern aesthetics

## How to Use

1. Launch QuickLink
2. Press your hotkey (default: Ctrl+Space) to show/hide the window
3. Type to search through your saved items
4. Click an item to copy it to clipboard
5. Double-click a link to open it in your browser
6. Use the "+" button to add new items
7. Configure your hotkey in Settings

## System Requirements

- Windows 10 (19041 or later)
- .NET 8 Runtime

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
