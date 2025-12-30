# Features & Item Types

## 📍 URLs & Links

Store links and open them instantly.

**Examples:**
- `https://github.com/miskibin/quickLink`
- `https://jira.company.com/secure/RapidBoard.jspa`

## 📄 Text Snippets

Store reusable text and copy to clipboard with <kbd>Enter</kbd>.

**Examples:**
```
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080

Best regards,
John Doe
john@example.com

{"status": "success", "data": {}}
```

## ⚡ Shell Commands

Prefix with `>` to execute PowerShell commands. Runs in background unless Terminal option enabled.

**Examples:**
```
>notepad C:\path\to\file.txt
>explorer %USERPROFILE%\Downloads
>next
>prev
>playpause
>devmgmt.msc
>taskmgr
>shutdown /s /t 60
>shutdown /a
```

## 🔐 Passwords

Prefix with `$` to store encrypted passwords. Copied to clipboard on <kbd>Enter</kbd>.

**Security:** Encrypted using Windows DPAPI. Only accessible from your user account on this machine.

## 🎯 Dynamic Commands

Create interactive commands with prefixes (e.g., `/scripts`, `/docs`) that list files and execute templates.

See [User-Defined Commands](user-commands.md) for detailed guide with examples.

## 🔍 Web Search Fallback

Press <kbd>Enter</kbd> without selecting an item to search the web (default: ChatGPT).

**Configure in Settings:**
- `https://google.com/search?q={query}` (Google)
- `https://chat.openai.com/?q={query}` (ChatGPT)
- `https://claude.ai/new?q={query}` (Claude)
- `https://bing.com/search?q={query}` (Bing)
