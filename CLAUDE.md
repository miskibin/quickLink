# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

QuickLink is a Windows desktop launcher application built with .NET 8 and WinUI 3. It provides instant access to URLs, text snippets, shell commands, and AI assistance via a global hotkey (Ctrl+Shift+A). The app runs as a system tray application and displays a search overlay when activated.

**Key Technologies:**
- .NET 8.0 (Target: Windows 10 19041+)
- Windows App SDK 1.8 with WinUI 3
- Self-contained deployment (single-file executable)
- WiX MSI installer for distribution (per-user, autostart enabled by default)

## Build and Development

### Building the Project

```bash
# Build for development
dotnet build quickLink/quickLink.csproj

# Build self-contained release
dotnet publish quickLink/quickLink.csproj -c Release -r win-x64 -p:Platform=x64 -p:PublishSingleFile=true --self-contained
```

### Development Setup

1. **Open in Visual Studio**: Use `quickLink/quickLink.sln`
2. **Target Framework**: .NET 8.0 with Windows App SDK
3. **Platform**: x64 only (defined in project file)

### Release Process

Releases are automated via GitHub Actions (`.github/workflows/release.yml`):
- Triggered by version tags (e.g., `v1.2.3`)
- Builds self-contained executable
- Creates ZIP archive with checksums
- Generates release notes from commit history

## Architecture

### MVVM Pattern with Single Window

QuickLink follows an MVVM-inspired architecture with a single main window (`MainWindow.xaml`) that serves as the search overlay. The application starts minimized to system tray and shows the window only when activated via hotkey.

### Core Architectural Components

**1. Item Model System (`Models/ListItems/`)**

All items that appear in the search results implement `IListItem`:
- `LinkItem` - Opens URLs
- `TextItem` - Copies text to clipboard
- `CommandItem` - Executes shell commands
- `InternalCommandItem` - App commands (Settings, Add, Exit, etc.)
- `AiChatItem` - Starts AI conversations
- `SearchSuggestionItem` - Suggests web search
- `CommandSuggestionItem` - Suggests command creation
- `UserCommandResultItem` - Results from user-defined commands

Key interface: `IExecutionContext` - Provided to items for execution context (open URLs, copy to clipboard, execute commands, etc.)

**2. Service Layer (`Services/`)**

Core business logic is organized into services:
- `DataService` - Persists items to `%APPDATA%\QuickLink\data.json`
- `CommandService` - Handles user-defined and internal commands
- `GlobalHotkeyService` - Manages global keyboard shortcuts (Win32 interop)
- `ClipboardService` - Clipboard operations
- `EncryptionService` - Password encryption for secure items
- `GrokService` - AI assistant integration (xAI Grok API)
- `MediaControlService` - Media player controls
- `UsageTrackingService` - Tracks item usage statistics
- `DirectoryCommandProvider` - File system operations for user commands

**3. Dependency Injection Pattern**

Services are initialized in `MainWindow` constructor. No formal DI container - manual instantiation. Common configuration via `ServiceInitializer` helper class.

### Data Persistence

**Application Data Location**: `%APPDATA%\QuickLink\`

Key files:
- `data.json` - Saved items (links, text, commands)
- `settings.json` - App settings (hotkey, search URL, API key)
- `commands.json` - User-defined commands
- `crash.log` - Unhandled exception logs

Data is serialized using `System.Text.Json` with options defined in `ServiceInitializer.GetJsonSerializerOptions()`.

### Command System

**Internal Commands** (prefixed with `internal:`):
- `internal:add` - Quick add item
- `internal:addcommand` - Add user-defined command
- `internal:settings` - Open settings
- `internal:exit` - Quit application
- `internal:markdown` - Open last AI conversation
- `internal:openlastconversation` - Restore last chat

**User-Defined Commands** (prefixed with `/`):
- Configurable via `commands.json`
- Support file system globbing patterns
- Template-based command execution with placeholders:
  - `{item.path}` - Full file path
  - `{item.name}` - File name without extension
  - `{item.extension}` - File extension

**Media Commands** (prefixed with `>`):
- `>next` - Next track
- `>prev` - Previous track
- `>playpause` - Play/Pause

### Global Hotkey System

The app uses Win32 API for global hotkey registration:
- Registered via `RegisterHotKey` Win32 function
- Default: Ctrl+Shift+A (configurable in settings)
- Window subclassing used to intercept WM_HOTKEY messages
- See `MainWindow.xaml.cs` for Win32 P/Invoke declarations

### AI Assistant Integration

QuickLink integrates with xAI's Grok API:
- API key stored in settings (`AppSettings.ApiKey`)
- Streaming responses for real-time feedback
- Conversation history maintained in memory during app session
- Markdown rendering via `CommunityToolkit.WinUI.UI.Controls.Markdown`

## Important Implementation Details

### Window Management

- **Window State**: The main window starts hidden and is shown/hidden via global hotkey
- **Always On Top**: Uses `OverlappedPresenter.IsAlwaysOnTop` (note: there's a known crash issue that was fixed in commit 943e083)
- **Desktop Acrylic**: Backdrop material for modern Windows 11 look

### Search and Filtering

- **Debouncing**: 16ms debounce (~1 frame at 60fps) for search input
- **Caching**: Last search text cached to avoid redundant filtering
- **Fuzzy Matching**: Items implement `MatchesSearch()` for custom filtering logic

### Performance Optimizations

- Single-file executable with self-contained deployment
- Minimal dependencies (Windows App SDK, Community Toolkit)
- Lazy initialization of services
- Search debouncing to reduce UI updates

### Security Considerations

- Passwords stored with encryption (`EncryptionService`)
- API keys stored in settings (encrypted at rest)
- File operations use proper permission checks
- Crash logging to `%APPDATA%\QuickLink\crash.log`

## Common Development Tasks

### Adding a New Item Type

1. Create new class in `Models/ListItems/` implementing `IListItem`
2. Implement required properties: `DisplayTitle`, `DisplayValue`, `IconGlyph`, `IconColor`
3. Implement `ExecuteAsync()` with item-specific logic
4. Implement `MatchesSearch()` for search filtering
5. Add to `MainWindow._allItems` collection
6. Update `DataService` to persist/load the new item type

### Modifying User Commands

User commands are defined in `Models/UserCommand.cs` and loaded from `%APPDATA%\QuickLink\commands.json`. The `CommandService` handles execution, and `DirectoryCommandProvider` handles file system operations.

### Debugging Global Hotkeys

The hotkey system uses Win32 interop. Key files:
- `Services/GlobalHotkeyService.cs` - Hotkey registration
- `MainWindow.xaml.cs` - Window subclassing and WM_HOTKEY handling
- P/Invoke declarations at top of `MainWindow.xaml.cs`

### Testing Data Persistence

Data files are in `%APPDATA%\QuickLink\`. Manually edit these JSON files to test different states, or delete them to reset the app.

## File Structure Notes

- **Views/** directory is currently empty - all UI is in MainWindow.xaml
- **Converters/** - XAML value converters for UI binding
- **Helpers/** - Utility classes (see `ServiceInitializer`)
- **Constants/** - App-wide constants (file names, prefixes, defaults)

## Configuration

**Default Hotkey**: Ctrl+Shift+A
**Default Search URL**: https://chatgpt.com/?q={query}

Settings can be modified:
1. Via Settings UI (search for "Settings")
2. Directly in `%APPDATA%\QuickLink\settings.json`
