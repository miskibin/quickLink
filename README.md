# 🚀 QuickLink

> A fast, elegant clipboard manager for Windows with global hotkey support.

![2025-10-30 20-00-10](https://github.com/user-attachments/assets/cc0d0edc-f90a-407b-b23c-133546bc0099)

## 📥 Download

<div align="center">

**[⬇️ Get Latest Release](https://github.com/miskibin/quickLink/releases)**

Download the **x64** version for Windows 10/11 (64-bit).

</div>

## ✨ Features

### 🎯 Core Functionality
- **Global Hotkey Access** - Press your custom hotkey (default: <kbd>Ctrl+Space</kbd>) to instantly open QuickLink from anywhere
- **Real-Time Search** - Type to filter through your saved items instantly
- **One-Click Copy** - Click any item to copy it to your clipboard
- **Quick Item Execution** - Press <kbd>Enter</kbd> to execute/open the selected item

### 📋 Item Management
- **Text Snippets** - Save frequently used text for quick access
- **URL Shortcuts** - Save links and open them directly in your browser with a single click
- **Commands** - Save shell commands (prefix with `>`) that execute when triggered
- **🔐 Encryption** - Optionally encrypt sensitive values for added security
- **✏️ Add & Edit** - Easily add new items or edit existing ones with title and value fields
- **🗑️ Delete Items** - Remove items you no longer need
- **🤖 Auto-Detection** - Automatically recognizes URLs (starting with http/https) and commands (starting with >)

### 🔍 Search & Navigation
- **Smart Filtering** - Shows top 4 matching results as you type
- **⌨️ Keyboard Navigation** - Use arrow keys to navigate through results, <kbd>Enter</kbd> to select
- **Search Suggestions** - Unmatched searches can be sent to your configured search engine

### 🎵 Media Control
- **Media Playback Control** - Control music/media playback with simple commands:
  - `>next` or `>media next` - Skip to next track
  - `>prev` or `>media prev` - Go to previous track
  - `>playpause` or `>play` or `>pause` - Toggle play/pause
  
### ⚙️ Customization & Settings
- **Custom Hotkeys** - Configure your own hotkey combination with multiple modifier support (Ctrl, Shift, Alt)
- **🚀 Startup Launch** - Option to launch QuickLink on system startup
- **🔎 Custom Search URL** - Set your preferred search engine URL for searches (supports placeholders like `{query}`)
- **👁️ Footer Toggle** - Hide the footer to make Add/Settings/Exit searchable items
- **💾 Persistent Configuration** - All settings are saved and restored on app launch

### 🎨 User Interface
- **Modern Glass Effect** - Desktop Acrylic backdrop for a modern Windows 11 aesthetic
- **Visual Icons** - Distinct icons for different item types:
  - 🔗 Links (blue)
  - 🔒 Encrypted content (gold)
  - ⚡ Commands (green)
  - 📄 Text snippets (gray)
- **Smooth Animations** - PowerToys-style entrance and transition animations
- **🌙 Dark Theme** - Optimized dark interface for comfortable viewing
- **📱 Responsive Design** - Fixed window size with keyboard-first navigation

## 🎮 How to Use

### 📖 Basic Usage
1. **Launch** QuickLink
2. Press your hotkey (default: <kbd>Ctrl+Space</kbd>) to show/hide the window
3. **Type** to search through your saved items
4. Use **arrow keys** to navigate results
5. Press <kbd>Enter</kbd> or click an item to execute it
6. Press <kbd>Escape</kbd> to hide the window

### 🏷️ Item Types & Execution

**📄 Text Snippets**
- Simply add a text value
- Click or press <kbd>Enter</kbd> to copy to clipboard

**🔗 URLs/Links**
- Add any URL starting with `http://` or `https://`
- Items are automatically recognized as links
- Click or press <kbd>Enter</kbd> to open in your default browser

**⚡ Commands** 
- Prefix commands with `>` (e.g., `>next` for next media track)
- Commands will execute when triggered
- Examples: `>next`, `>prev`, `>playpause` for media control
- Custom shell commands are also supported

**🔍 Search Engine Queries**
- If a search doesn't match any items, press <kbd>Enter</kbd> to search using your configured search engine
- Configure your preferred search URL in Settings
- Default: ChatGPT - change to Google, Claude, or any engine you prefer

### ⚙️ Settings Configuration

**🎹 Hotkey Settings**
- Click the **Settings** button (⚙️ icon) or use the built-in "Settings" command
- Click the hotkey box and press your desired key combination (e.g., <kbd>Ctrl+Shift+Q</kbd>)
- Must include at least one modifier (Ctrl, Shift, or Alt) and one regular key
- Click **Apply** to save your new hotkey

**🚀 Startup Options**
- Enable "Launch on system startup" to have QuickLink start automatically with Windows

**🔎 Search Engine**
- Customize your search URL in Settings
- Use `{query}` as a placeholder for your search term
- Examples: `https://google.com/search?q={query}` or `https://claude.ai/new?q={query}`

**🎨 UI Preferences**
- Toggle "Hide footer" to make Add/Settings/Exit searchable (appear in search results)
- This is useful if you use the hotkey and want faster access to these functions

## 💻 System Requirements

- Windows 10 (19041 or later)
- .NET 8 Runtime

## 🔄 Continuous Deployment

This repository includes a GitHub Actions workflow (`.github/workflows/cd.yml`) that automatically builds and packages the application for all supported platforms (x64, x86, ARM64).

### 🎯 Workflow Triggers

The CD workflow runs on:
- Push to the `master` branch
- Pull requests targeting the `master` branch

### 📦 Build Artifacts

After a successful build, the workflow uploads artifacts containing:
- Compiled application binaries
- MSIX packages (if MSIX packaging is enabled)
- All platform-specific files

Artifacts are named: `quickLink-{platform}-{configuration}`

### 🔐 Code Signing (Optional)

To sign your MSIX packages, you need to configure two repository secrets:

1. **Base64_Encoded_Pfx**: Your code signing certificate encoded in Base64
2. **Pfx_Key**: The password for your certificate

#### 📝 How to Create and Configure Secrets

1. Generate or obtain a code signing certificate (.pfx file)

2. Encode the certificate to Base64:
   ```powershell
   $pfx_cert = Get-Content '.\YourCertificate.pfx' -Encoding Byte
   [System.Convert]::ToBase64String($pfx_cert) | Out-File 'EncodedCertificate.txt'
   ```

3. Add the secrets to your GitHub repository:
   - Go to **Settings** > **Secrets and variables** > **Actions**
   - Click **"New repository secret"**
   - Add `Base64_Encoded_Pfx` with the contents of `EncodedCertificate.txt`
   - Add `Pfx_Key` with your certificate password

The workflow will automatically detect these secrets and sign your packages if they are present.

## 🖥️ Platforms

This application supports the following platforms:
- **x64** (64-bit Intel/AMD)

---

<div align="center">

**[⬇️ Download Latest Release](https://github.com/miskibin/quickLink/releases)** • [🐛 Report Bug](https://github.com/miskibin/quickLink/issues) • [💡 Request Feature](https://github.com/miskibin/quickLink/issues)

</div>
