# 🚀 QuickLink

<img width="728" height="386" alt="image" src="https://github.com/user-attachments/assets/f3cc58c0-b12f-4453-bd9f-f72e67a50352" />


> A fast, elegant link, clipboard, and commands manager for Windows with global hotkey support.


<div align="center">

**[⬇️ Download Latest Release](https://github.com/miskibin/quickLink/releases)** • **[🐛 Report Bug](https://github.com/miskibin/quickLink/issues)** • **[💡 Request Feature](https://github.com/miskibin/quickLink/issues)**

</div>

---

## 💡 Why QuickLink?

Tired of typing URLs in your browser and waiting for autocomplete? QuickLink lets you access links, snippets, and commands with a single hotkey press, making repetitive tasks faster than ever.

## 🎯 Use Cases

### 📱 Open URLs Instantly
Type <kbd>Ctrl+Space</kbd>, search by name, press <kbd>Enter</kbd>—your URL opens in a new tab.

**Examples:**
- `https://github.com/miskibin/quickLink/`
- `https://jiradc.ext.net.your-domain.com/secure/RapidBoard.jspa?rapidView=21728&quickFilter=135957`

### 📝 Store Text Snippets
Quick access to frequently used text. One hotkey press to copy to clipboard.

**Examples:**
- Configuration variables: `export HTTP_PROXY=http://proxy.company.com:8080 && export HTTPS_PROXY=http://proxy.company.com:8080`
- Email signatures, code templates, etc.

### ⚡ Execute Commands
Run shell commands or media controls instantly without switching windows.

**Examples:**
- File operations: `>notepad C:\Documents\notes.txt`
- Media control: `>next`, `>prev`, `>playpause`

### 🔐 Secure Password Manager
Store passwords with encryption and copy them to clipboard instantly. *(Encryption improvements in progress)*

## 🚀 Getting Started

### Quick Start

1. **Download & Launch** QuickLink from the [releases page](https://github.com/miskibin/quickLink/releases)
2. Press <kbd>Ctrl+Space</kbd> (default hotkey) to open the search window
3. Type to find your link, snippet, or command
4. Press <kbd>Enter</kbd> or click to execute
5. Press <kbd>Escape</kbd> to close

### Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Show/Hide Window | <kbd>Ctrl+Space</kbd> |
| Search Items | Just start typing |
| Navigate Results | <kbd>↑</kbd> / <kbd>↓</kbd> Arrow Keys |
| Execute Item | <kbd>Enter</kbd> |
| Close Window | <kbd>Escape</kbd> |

---

## 📖 Item Types

### 📄 Text Snippets
- Store frequently used text
- Click or press <kbd>Enter</kbd> to copy to clipboard
- **Use case:** Email signatures, config variables, code templates

### 🔗 URLs & Links
- Add any URL starting with `http://` or `https://`
- Automatically recognized and opened in your default browser
- **Use case:** Work portals, documentation, tools you use daily

### ⚡ Commands
- Prefix with `>` to create an executable command (e.g., `>next`, `>playlist`)
- Examples:
  - `>next` - Next media track
  - `>prev` - Previous media track
  - `>playpause` - Play/pause toggle
  - `>notepad C:\path\to\file.txt` - Open files instantly
- **Use case:** Media control, file management, workflow automation

### 🎯 User-Defined Commands (Advanced)
**NEW!** Create dynamic commands that list items and execute custom actions.

- **Trigger:** Prefix with `/` (e.g., `/docs`, `/scripts`)
- **Dynamic Sources:** List files from directories, static items, or HTTP endpoints
- **Template Execution:** Execute custom commands with placeholders like `{item.path}`, `{item.name}`, `{item.extension}`
- **Custom Icons:** Choose from 📁 Folder, 🌐 Web, ⚙️ Script, 📄 Document

**Example Use Cases:**
- `/docs` → List markdown files from your Documents folder → Opens selected file in VS Code
- `/scripts` → List PowerShell scripts → Executes selected script
- `/projects` → List project folders → Opens folder in File Explorer

**How to Add:**
1. Open Settings (click ⚙️ or search for "Settings")
2. Click "Add Command" in the User Commands section
3. Configure:
   - **Prefix:** The trigger text (e.g., `/docs`)
   - **Source Type:** Directory, Static, or HTTP
   - **Directory Config:** Path, file pattern (glob), recursive search
   - **Execute Template:** Command to run (e.g., `code "{item.path}"`)
   - **Icon:** Visual indicator for your command
4. Save and start using your command!

**Performance Note:** Commands are lazily loaded - results only appear when you type the prefix, keeping QuickLink fast.

### 🔍 Search Engine Queries
- No matching items? Press <kbd>Enter</kbd> to search using your configured search engine
- **Customizable:** Modify the search URL in Settings
- **Use `{query}` as a placeholder** for the search term
- **Examples:**
  - `https://google.com/search?q={query}` (Google)
  - `https://chat.openai.com/?q={query}` (ChatGPT - default)
  - `https://claude.ai/new?q={query}` (Claude)
  - `https://bing.com/search?q={query}` (Bing)

---

## 💎 Useful Commands to Add

Enhance your workflow with these ready-to-use commands. Simply add them as new items with the `>` prefix:

### 🌐 Network & Utilities
- **Get Your Public IP:** `>powershell -NoProfile -Command "Invoke-RestMethod 'https://api.ipify.org' | Set-Clipboard"` → Copies your public IP to clipboard
- **Open Device Manager:** `>devmgmt.msc`

### 📁 File & Folder Operations
- **Open Downloads Folder:** `>explorer %USERPROFILE%\Downloads`
- **Open Documents Folder:** `
- **Create New Folder Shortcut:** `>powershell -NoProfile -Command "$path = Read-Host 'Folder path'; New-Item -ItemType Directory -Path $path -Force"`
- **Clear Temp Files:** `>powershell -NoProfile -Command "Remove-Item -Path $env:TEMP\* -Force -Recurse -ErrorAction SilentlyContinue"`

### 🎯 Productivity
- **Lock Computer:** `>rundll32.exe user32.dll,LockWorkStation`
- **Shut Down in 60 seconds:** `>shutdown /s /t 60 /c "Computer will shut down soon"`
- **Cancel Shutdown:** `>shutdown /a`
- **Open Task Manager:** `>taskmgr`

### 🎨 Quick Clipboard Tools
- **Generate UUID:** `>powershell -NoProfile -Command "[guid]::NewGuid().ToString() | Set-Clipboard"`
- **Get Current Timestamp:** `>powershell -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd HH:mm:ss' | Set-Clipboard"`
- **Encode Text to Base64:** `>powershell -NoProfile -Command "$text = Read-Host 'Text to encode'; [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($text)) | Set-Clipboard"`

### 🔧 System Information
- **Show System Info:** `>msinfo32`
- **Open Event Viewer:** `>eventvwr.msc`
- **Check Disk Space:** `>powershell -NoProfile -Command "Get-Volume | Format-Table -AutoSize"`

**Tip:** You can modify these commands to suit your needs. Use `Set-Clipboard` to automatically copy results to your clipboard!


## Why not command palette from PowerToys?

It might seem like simmilar product, but it does not share any functionalities.

- QuickLink focuses on managing links, text snippets, and commands with a global hotkey.
- PowerToys command palette is more general-purpose and does not specialize in link/snippet management.
- QuickLink offers features like encrypted password storage and media controls, which are not available in PowerToys.





![Demo Screenshot](https://github.com/user-attachments/assets/cc0d0edc-f90a-407b-b23c-133546bc0099)
