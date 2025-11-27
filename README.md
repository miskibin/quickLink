# 🚀 QuickLink

<img width="772" height="411" alt="image" src="https://github.com/user-attachments/assets/4cdc4f7d-812b-414b-87a6-807896c177c3" />


> A fast, elegant link, clipboard, and commands manager for Windows with global hotkey support.

<div align="center">

**[⬇️ Download Latest Release](https://github.com/miskibin/quickLink/releases)** • **[🐛 Report Bug](https://github.com/miskibin/quickLink/issues)** • **[💡 Request Feature](https://github.com/miskibin/quickLink/issues)**

</div>

---

## 💡 Why QuickLink?

Stop context-switching for repetitive tasks. Press **Ctrl+Space** anywhere to instantly access URLs, snippets, commands, and files—making your workflow uninterrupted and efficient.

## 🎯 Use Cases

- **📱 URLs:** Open frequently visited links instantly (Jira boards, dashboards, documentation)
- **📝 Snippets:** Copy text to clipboard with one keystroke (config variables, email signatures, templates)
- **⚡ Commands:** Execute shell commands or media controls without switching windows
- **🎯 Dynamic Commands:** List files/folders and execute custom actions (open projects, run scripts, browse docs)
- **🔐 Passwords:** Store encrypted passwords for quick clipboard access

## 🚀 Getting Started

### Installation

<details>
<summary><b>Portable Version</b></summary>

1. Download the portable ZIP from [releases page](https://github.com/miskibin/quickLink/releases)
2. Extract and run `QuickLink.exe`

</details>

### Quick Start

1. **Launch** QuickLink (appears in system tray)
2. Press <kbd>Ctrl+Space</kbd> to open the search window
3. Type to find your link, snippet, or command
4. Press <kbd>Enter</kbd> to execute
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

### � URLs & Links
Add any URL starting with `http://` or `https://`. Opens in your default browser.
- **Example:** `https://jiradc.ext.net.your-domain.com/secure/RapidBoard.jspa?rapidView=21728`

### �📄 Text Snippets
Store frequently used text. Click or press <kbd>Enter</kbd> to copy to clipboard.
- **Example:** `export HTTP_PROXY=http://proxy.company.com:8080 && export HTTPS_PROXY=http://proxy.company.com:8080`

### ⚡ Shell Commands
Prefix with `>` to create executable commands.
- **Examples:**
  - `>notepad C:\path\to\file.txt` - Open file instantly
  - `>next` / `>prev` / `>playpause` - Media controls

### 🎯 User-Defined Commands

**Create dynamic commands that list items and execute custom actions.**

#### Configuration
- **Trigger:** Prefix with `/` (e.g., `/docs`, `/scripts`)
- **Source Type:** Directory, Static items, or HTTP endpoints
- **Execute Template:** Command with placeholders:
  - `{item.path}` - Full file path
  - `{item.name}` - File name without extension
  - `{item.extension}` - File extension (e.g., `.md`)
  - `{query}` - The search query text (URL-encoded for web URLs)
- **Terminal Option:** Run command in visible terminal or silently

<img width="811" height="409" alt="image" src="https://github.com/user-attachments/assets/e450b686-75ce-4cd3-91e1-6d661b57c561" />


#### Real-World Examples

<details>
<summary><b>📁 Browse Documentation Files</b></summary>

**Prefix:** `/docs`  
**Source:** Directory  
**Path:** `C:\Users\YourName\Documents\Documentation`  
**Glob Pattern:** `*.md`  
**Recursive:** Yes  
**Execute Template:** `code "{item.path}"`  
**Terminal:** No

Opens markdown files in VS Code.

</details>

<details>
<summary><b>⚙️ Run PowerShell Scripts</b></summary>

**Prefix:** `/scripts`  
**Source:** Directory  
**Path:** `C:\Scripts`  
**Glob Pattern:** `*.ps1`  
**Recursive:** Yes  
**Execute Template:** `powershell -ExecutionPolicy Bypass -File "{item.path}"`  
**Terminal:** Yes

Executes PowerShell scripts in a visible terminal.

</details>

<details>
<summary><b>📂 Open Project Folders</b></summary>

**Prefix:** `/projects`  
**Source:** Directory  
**Path:** `C:\Dev\Projects`  
**Glob Pattern:** `*.*`  
**Recursive:** No (only top-level folders)  
**Execute Template:** `explorer "{item.path}"`  
**Terminal:** No

Opens project folders in File Explorer.

</details>

<details>
<summary><b>🌐 Search GitHub Repos</b></summary>

**Prefix:** `/gh`  
**Source:** Directory (or Static with predefined repo names)  
**Execute Template:** `https://github.com/search?q={query}&type=repositories`  
**Terminal:** No

Searches GitHub for repositories matching your query. The `{query}` placeholder gets replaced with what you type after `/gh `.

</details>

<details>
<summary><b>🎨 Open Design Files in Figma</b></summary>

**Prefix:** `/design`  
**Source:** Static  
**Items:** `Landing Page`, `Dashboard UI`, `Mobile App`  
**Execute Template:** `https://figma.com/file/your-file-id?search={query}`  
**Terminal:** No

Opens Figma designs based on name search.

</details>

<details>
<summary><b>🐍 Run Python Scripts with Arguments</b></summary>

**Prefix:** `/py`  
**Source:** Directory  
**Path:** `C:\Scripts\Python`  
**Glob Pattern:** `*.py`  
**Execute Template:** `python "{item.path}"`  
**Terminal:** Yes

Executes Python scripts in a terminal window.

</details>

**How to Add:**
2. Type "Add Command" in UI

**Performance Note:** Commands are lazily loaded—results only appear when you type the prefix.

### 🔍 Search Engine Queries

No matching items? Press <kbd>Enter</kbd> to search using your configured search engine.

**Customizable in Settings:**
- `https://google.com/search?q={query}` (Google)
- `https://chat.openai.com/?q={query}` (ChatGPT - default)
- `https://claude.ai/new?q={query}` (Claude)
- `https://bing.com/search?q={query}` (Bing)

---

<details>
<summary><h2>💎 Useful Commands to Add</h2></summary>

Enhance your workflow with these ready-to-use commands. Add them as new items with the `>` prefix:

### 🌐 Network & Utilities
- **Get Your Public IP:** `>powershell -NoProfile -Command "Invoke-RestMethod 'https://api.ipify.org' | Set-Clipboard"`
- **Open Device Manager:** `>devmgmt.msc`

### 📁 File & Folder Operations
- **Open Downloads Folder:** `>explorer %USERPROFILE%\Downloads`
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

</details>

---

## Why QuickLink vs PowerToys Run?

PowerToys Run excels at app launching and file search, but QuickLink specializes in developer workflow:

| Feature | QuickLink | PowerToys Run |
|---------|-----------|---------------|
| **URL Management** | ✅ Named shortcuts for long URLs | ❌ |
| **Text Snippets** | ✅ Instant clipboard access | ❌ |
| **Dynamic Commands** | ✅ List files & execute templates | ❌ |
| **Media Controls** | ✅ Built-in | ❌ |
| **Password Storage** | ✅ Encrypted | ❌ |
| **App Launching** | ❌ | ✅ |
| **System-wide File Search** | ❌ | ✅ |

QuickLink focuses on repetitive tasks developers do constantly: accessing links, pasting snippets, and running custom file-based commands without breaking flow.

---

## 🤖 AI Assistant

QuickLink includes an integrated AI chat powered by **Grok 4.1 fast (xAI)** for quick answers without leaving your workflow.

### Setup

1. Open **Settings** (search for "Settings" or press <kbd>Ctrl+Space</kbd>)
2. Get your API key from [xAI Console](https://console.x.ai/)
3. Paste the key in the **API Key** field
4. Save settings

### Usage

Just type your question in the QuickLink search bar and press <kbd>Enter</kbd> to get instant answers.

- Use internal command: "Open last conversation" to restore your previous chat

**Features:**
- **Streaming responses:** See answers as they're generated
- **Conversation history:** Continue multi-turn conversations
- **Direct answers:** No filler phrases—just the information you need
- **Short responses:** Limited to 500 tokens for quick, focused answers
- **Persistent sessions:** Your last conversation is saved and can be restored

**Tip:** The AI is configured to answer directly without unnecessary greetings or filler text, perfect for quick lookups during coding.

---

AI interface: 

<img width="880" height="402" alt="image" src="https://github.com/user-attachments/assets/e81c08bf-3910-4612-80ab-0b914b70161f" />


<img width="746" height="396" alt="image" src="https://github.com/user-attachments/assets/b49ae5e4-9144-4a7f-9e0c-37ada8c3a5ab" />

<img width="785" height="401" alt="image" src="https://github.com/user-attachments/assets/b14cb750-9aef-4ae0-8491-f364ae746b09" />



![Demo Screenshot](https://github.com/user-attachments/assets/cc0d0edc-f90a-407b-b23c-133546bc0099)
