# 🚀 QuickLink

<img width="728" height="386" alt="image" src="https://github.com/user-attachments/assets/f3cc58c0-b12f-4453-bd9f-f72e67a50352" />


> A fast, elegant link, clipboard, and commands manager for Windows with global hotkey support.

![Demo Screenshot](https://github.com/user-attachments/assets/cc0d0edc-f90a-407b-b23c-133546bc0099)


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

### 🔍 Search Engine Queries
- No matching items? Press <kbd>Enter</kbd> to search using your configured search engine
- **Customizable:** Modify the search URL in Settings
- **Use `{query}` as a placeholder** for the search term
- **Examples:**
  - `https://google.com/search?q={query}` (Google)
  - `https://chat.openai.com/?q={query}` (ChatGPT - default)
  - `https://claude.ai/new?q={query}` (Claude)
  - `https://bing.com/search?q={query}` (Bing)


## Why not command palette from PowerToys?

It might seem like simmilar product, but it does not share any functionalities.

- QuickLink focuses on managing links, text snippets, and commands with a global hotkey.
- PowerToys command palette is more general-purpose and does not specialize in link/snippet management.
- QuickLink offers features like encrypted password storage and media controls, which are not available in PowerToys.
