# Configuration

QuickLink stores data in `%APPDATA%\QuickLink\`:

- `settings.json` (app settings)
- `data.json` (your saved items)
- `commands.json` (user-defined commands)

Edit via the Settings UI (search for "Settings") or by editing the JSON files.

## Settings Reference

### Global Settings

```json
{
  "HotkeyModifiers": 6,
  "HotkeyKey": 65,
  "HideFooter": true,
  "SearchUrl": "https://chatgpt.com/?q={query}",
  "ApiKey": ""
}
```

#### HotkeyModifiers

The keyboard modifiers for the global hotkey.

- `0` - None
- `2` - Ctrl
- `4` - Shift
- `6` - Ctrl + Shift
- `8` - Alt
- `10` - Ctrl + Alt
- `12` - Shift + Alt
- `14` - Ctrl + Shift + Alt

**Default:** `6` (Ctrl + Shift)

#### HotkeyKey

The key code for the global hotkey.

**Common Keys:**
- `65` - A
- `66` - B
- `67` - C
- `32` - Space
- `13` - Enter

You can find complete VK codes in [Windows Virtual Key documentation](https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).

**Default:** `65` (A)

**Current Default:** Ctrl + Shift + A

#### HideFooter

Whether to hide the footer in the search window.

- `true` - Hidden
- `false` - Visible

**Default:** `true`

#### SearchUrl

The URL to open when you press Enter without selecting an item.

**Supported Placeholders:**
- `{query}` - Your search text (URL-encoded)

**Popular Options:**
- `https://google.com/search?q={query}` - Google
- `https://chatgpt.com/?q={query}` - ChatGPT
- `https://claude.ai/new?q={query}` - Claude
- `https://bing.com/search?q={query}` - Bing
- `https://www.wikipedia.org/w/api.php?action=query&list=search&srsearch={query}` - Wikipedia

**Default:** `https://chatgpt.com/?q={query}`

#### ApiKey

Your xAI API key for the AI Assistant feature.

- Leave empty to disable AI features
- Required for using Grok AI integration

**Default:** Empty

## Items (data.json)

Items are stored in `data.json` as a list of objects:

```json
[
  {
    "Title": "GitHub",
    "Value": "https://github.com",
    "IsEncrypted": false
  },
  {
    "Title": "Build",
    "Value": ">dotnet build",
    "IsEncrypted": false
  }
]
```

Item type is inferred from `Value`:

- `https://...` / `http://...` → link
- `>...` → shell command
- anything else → text/snippet (copied to clipboard)

If `IsEncrypted` is `true`, the UI masks the value and it is stored encrypted.

## User-Defined Commands (commands.json)

User-defined commands are stored in `commands.json`:

```json
[
  {
    "Prefix": "/scripts",
    "Source": "Directory",
    "SourceConfig": {
      "Path": "C:\\Scripts",
      "Recursive": true,
      "Glob": "*.ps1",
      "Items": []
    },
    "ExecuteTemplate": "powershell -ExecutionPolicy Bypass -File \"{item.path}\"",
    "Icon": "Script",
    "OpenInTerminal": true
  }
]
```

## Best Practices

1. **Back up** - Copy `%APPDATA%\QuickLink\` regularly
2. **Use the UI** - Avoid JSON syntax mistakes
3. **Restart after edits** - Changes are loaded on startup
4. **Escape backslashes** - In JSON, use `\\` for Windows paths
5. **Use quotes** - All string values must be quoted in JSON

## Common Changes

### Change Global Hotkey

Edit `HotkeyModifiers` and `HotkeyKey`:

```json
{
  "HotkeyModifiers": 6,
  "HotkeyKey": 32
}
```

This sets the hotkey to **Ctrl + Shift + Space**.

### Change Search Engine

Update `SearchUrl`:

```json
{
  "SearchUrl": "https://google.com/search?q={query}"
}
```

### Add AI API Key

```json
{
  "ApiKey": "your-xai-api-key-here"
}
```

## Troubleshooting

**Settings won't save in UI:**
- Check that `settings.json` exists
- Verify QuickLink has write permissions to `%APPDATA%\QuickLink\`
- Try editing directly

**Settings revert after restart:**
- Close QuickLink completely
- Edit `settings.json`
- Restart QuickLink

**JSON is corrupted:**
- Restore from backup
- Use a JSON validator to check syntax
- Restart QuickLink

## Full Example

Here's a complete `settings.json` file:

```json
{
  "HotkeyModifiers": 6,
  "HotkeyKey": 65,
  "HideFooter": true,
  "SearchUrl": "https://chat.openai.com/?q={query}",
  "ApiKey": "xai-api-key-here",
  "Items": [
    {
      "Type": 0,
      "Name": "GitHub",
      "Value": "https://github.com"
    },
    {
      "Type": 1,
      "Name": "Email Signature",
      "Value": "Best regards,\nJohn Doe\njohn@example.com"
    },
    {
      "Type": 2,
      "Name": "Lock PC",
      "Value": ">rundll32.exe user32.dll,LockWorkStation"
    },
    {
      "Type": 4,
      "Name": "Scripts",
      "Trigger": "/scripts",
      "SourceType": 0,
      "Path": "C:\\Scripts",
      "GlobPattern": "*.ps1",
      "Recursive": true,
      "ExecuteTemplate": "powershell -ExecutionPolicy Bypass -File \"{item.path}\"",
      "Terminal": true
    }
  ]
}
```
