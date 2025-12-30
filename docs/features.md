---
title: Features & Item Types
nav_order: 3
---

# Features & Item Types

QuickLink stores items in `data.json` and infers type from the value.

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

Prefix with `>` to execute a shell command. Commands run silently in the background.

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

## 🔐 Encrypted Items

Any item (link, text, or command) can be marked as encrypted in the UI.

- Encrypted items are masked in the list (shown as `••••••••`).
- Values are stored encrypted on disk.

Note: the current implementation uses a built-in AES key (obfuscation, not strong security).

## 🎯 Dynamic Commands

Create interactive commands with prefixes (e.g., `/scripts`, `/docs`) that list files and execute templates.

See [User-Defined Commands](user-commands.md) for detailed guide with examples.

## 🔍 Web Search Fallback

If nothing matches, pressing <kbd>Enter</kbd> opens your configured search URL.

**Configure in Settings:**
- `https://google.com/search?q={query}` (Google)
- `https://chatgpt.com/?q={query}` (ChatGPT)
- `https://claude.ai/new?q={query}` (Claude)
- `https://bing.com/search?q={query}` (Bing)
