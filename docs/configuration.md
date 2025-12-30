# Configuration

Settings stored at: `%APPDATA%\QuickLink\settings.json`

**Edit via:** Settings UI (search for "Settings") or directly in JSON file

## Settings Reference

### Global Settings

```json
{
  "HotkeyModifiers": 6,
  "HotkeyKey": 65,
  "HideFooter": true,
  "SearchUrl": "https://chat.openai.com/?q={query}",
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
- `https://chat.openai.com/?q={query}` - ChatGPT
- `https://claude.ai/new?q={query}` - Claude
- `https://bing.com/search?q={query}` - Bing
- `https://www.wikipedia.org/w/api.php?action=query&list=search&srsearch={query}` - Wikipedia

**Default:** `https://chat.openai.com/?q={query}`

#### ApiKey

Your xAI API key for the AI Assistant feature.

- Leave empty to disable AI features
- Required for using Grok AI integration

**Default:** Empty

### Items

All your saved items (URLs, snippets, commands, passwords) are stored under an `Items` array:

```json
{
  "Items": [
    {
      "Type": 0,
      "Name": "GitHub",
      "Value": "https://github.com"
    },
    {
      "Type": 1,
      "Name": "My Email",
      "Value": "john@example.com"
    }
  ]
}
```

#### Item Types

- `0` - URL / Link
- `1` - Text Snippet
- `2` - Shell Command
- `3` - Password (encrypted)
- `4` - User-Defined Command

### User-Defined Commands

User-defined commands have a more complex structure:

```json
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
```

#### Source Types

- `0` - Directory
- `1` - Static
- `2` - HTTP

## Best Practices

1. **Back up your settings** - Copy `settings.json` regularly
2. **Use the UI** - The UI validates your input better than manual editing
3. **Restart when editing directly** - QuickLink caches settings on startup
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
