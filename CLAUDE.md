# QuickLink

Windows productivity launcher — global hotkey brings up search, instantly execute URLs, text snippets, shell commands, dynamic commands, AI chat.

## Tech Stack
- C# / .NET 8.0 / WinUI 3 (Windows App SDK 1.8)
- MSIX packaged, self-contained, x64
- Target: Windows 10.0.19041.0+

## Build & Run
```bash
dotnet build quickLink/quickLink.csproj
dotnet run --project quickLink/quickLink.csproj
```

## Project Structure
- `quickLink/MainWindow.xaml.cs` — Primary UI + window management (~1973 lines)
- `quickLink/Models/ListItems/IListItem.cs` — Core polymorphic interface for all item types
- `quickLink/Services/` — DataService, CommandService, GlobalHotkeyService, ClipboardService, EncryptionService, GrokService, MediaControlService, DirectoryCommandProvider, UsageTrackingService
- `quickLink/Constants/AppConstants.cs` — Centralized config
- `quickLink/Models/` — AppSettings, UserCommand, ItemUsageStats, 9 IListItem implementations
- `docs/` — User-facing documentation (Jekyll)

## Key Dependencies
- CommunityToolkit.WinUI.UI.Controls.Markdown
- Microsoft.Extensions.FileSystemGlobbing
- Microsoft.WindowsAppSDK 1.8

## Data Storage
- `%APPDATA%\QuickLink\data.json` — Saved items
- `%APPDATA%\QuickLink\commands.json` — User-defined commands
- `%APPDATA%\QuickLink\settings.json` — Preferences

## Development Priorities
- **Snappiness and performance are paramount** — every millisecond matters
- Debounced search (16ms), cached items in memory, background filtering
- Startup time is instrumented and tracked

## Architecture Notes
- Win32 P/Invoke for global hotkey (WM_HOTKEY) and window subclassing
- Multi-monitor aware (centers on cursor's monitor)
- Always-on-top when focused, acrylic backdrop
- JSON persistence with SemaphoreSlim file locking
- Items ranked by text match + usage frequency
