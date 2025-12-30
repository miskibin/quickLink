---
title: Keyboard Shortcuts
nav_order: 6
---

# Keyboard Shortcuts

## Global Hotkey

| Shortcut | Action |
|----------|--------|
| <kbd>Ctrl+Shift+A</kbd> | Open/Close QuickLink |

**Customizable in [Configuration](configuration.md)**

## In Search Window

| Shortcut | Action |
|----------|--------|
| <kbd>↑</kbd> / <kbd>↓</kbd> | Navigate |
| <kbd>Enter</kbd> | Execute/Copy |
| <kbd>Escape</kbd> | Close |

## Item Behavior

- **URLs:** <kbd>Enter</kbd> → Opens in browser
- **Snippets:** <kbd>Enter</kbd> → Copies to clipboard
- **Commands** (`>`): <kbd>Enter</kbd> → Executes

Encrypted items are masked in the list, but behave the same when executed.

## Custom Global Hotkey

Change in [Configuration](configuration.md) - HotkeyModifiers & HotkeyKey

**Examples:**
- <kbd>Ctrl+Space</kbd>: `HotkeyModifiers: 2, HotkeyKey: 32`
- <kbd>Alt+Q</kbd>: `HotkeyModifiers: 8, HotkeyKey: 81`
- <kbd>Ctrl+Shift+T</kbd>: `HotkeyModifiers: 6, HotkeyKey: 84`

See [Virtual Key Codes](https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes) for complete key reference.
