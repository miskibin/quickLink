using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using quickLink.Constants;
using quickLink.Helpers;
using quickLink.Models;
using quickLink.Models.ListItems;
using quickLink.Services;
using WinRT.Interop;

namespace quickLink
{
    public sealed partial class MainWindow : Window, IExecutionContext
    {
        #region Constants
        private const int WM_HOTKEY = 0x0312;
        private const int GWLP_WNDPROC = -4;

        // Window dimensions - 1.2x bigger
        private const int WINDOW_WIDTH = 720;  // 600 * 1.2
        private const int WINDOW_HEIGHT = 360; // 300 * 1.2
        #endregion

        #region Fields
        private readonly DataService _dataService;
        private readonly ClipboardService _clipboardService;
        private readonly MediaControlService _mediaControlService;
        private readonly CommandService _commandService;
        private readonly DirectoryCommandProvider _directoryProvider;
        private readonly UsageTrackingService _usageTrackingService;
        private readonly GrokService _grokService;
        private readonly ChatHistoryService _chatHistoryService;
        private readonly ObservableCollection<IListItem> _allItems;
        private readonly ObservableCollection<IListItem> _filteredItems;
        private readonly List<InternalCommandItem> _internalCommands;
        // Up-to-3 persisted chats surfaced as searchable launcher items; refreshed on change.
        private readonly List<ChatHistoryItem> _chatHistoryItems;
        private readonly SearchSuggestionItem _searchSuggestionItem;
        private List<UserCommand> _userCommands;

        private GlobalHotkeyService? _hotkeyService;
        private IntPtr _windowHandle;
        private IListItem? _editingItem;
        private bool _isEditing;
        private bool _hideFooter;
        private string _searchUrl = AppConstants.DefaultSettings.DefaultSearchUrl;

        // Settings are owned by SettingsService; MainWindow reacts to SettingsChanged.
        private SettingsService? _settingsService;

        // Tracks the currently registered global hotkey so we only re-register on change.
        private uint _registeredHotkeyModifiers;
        private uint _registeredHotkeyKey;

        // Performance optimization: cache the last search to avoid redundant filtering
        private string _lastSearchText = string.Empty;

        // Cached DPI scale (P/Invoke is expensive to call repeatedly)
        private double _cachedDpiScale;

        // Startup performance tracking
        private static readonly Stopwatch _startupStopwatch = Stopwatch.StartNew();

        // Search debouncing + in-flight filter cancellation (single CTS covers both)
        private CancellationTokenSource? _searchDebounceTokenSource;
        private const int SEARCH_DEBOUNCE_MS = 16; // ~1 frame at 60fps - minimal debounce

        // Tracks whether an exit animation is already running so we don't re-trigger it.
        private bool _isHidingAnimating;

        // Markdown streaming state
        private string _markdownContent = string.Empty;
        private string _apiKey = string.Empty;
        private string _lastAssistantMessage = string.Empty;

        // The conversation currently shown in the markdown panel (persisted per exchange).
        private ChatSession? _activeChat;
        // Session ids whose auto-title has already been requested (avoids duplicate calls).
        private readonly HashSet<string> _titleRequested = new HashSet<string>();

        // Window subclassing
        private WinProc? _newWndProc;
        private IntPtr _oldWndProc;

        public List<UserCommand> UserCommands
        {
            get => _userCommands;
            set
            {
                _userCommands = value;
            }
        }
        #endregion

        #region P/Invoke Declarations
        private delegate IntPtr WinProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        #region Constructor & Initialization
        public MainWindow()
        {
            try
            {
                LogPerf("MainWindow ctor start");

                _dataService = new DataService();
                _clipboardService = new ClipboardService();
                _mediaControlService = new MediaControlService();
                _commandService = new CommandService();
                _directoryProvider = new DirectoryCommandProvider();
                _usageTrackingService = new UsageTrackingService();
                _grokService = new GrokService();
                _chatHistoryService = new ChatHistoryService();
                _allItems = new ObservableCollection<IListItem>();
                _filteredItems = new ObservableCollection<IListItem>();
                _internalCommands = new List<InternalCommandItem>();
                _chatHistoryItems = new List<ChatHistoryItem>();
                _userCommands = new List<UserCommand>();
                _searchSuggestionItem = new SearchSuggestionItem();
                LogPerf("Services created");

                InitializeComponent();
                LogPerf("XAML initialized");

                InitializeInternalCommands();
                InitializeWindow();
                LogPerf("Window initialized");

                InitializeHotkey();

                _ = LoadDataAsync();
                // MediaControlService now uses lazy initialization - no need to init on startup
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL ERROR: {ex.Message}");
                throw;
            }
        }

        private void InitializeInternalCommands()
        {
            // Media control commands
            _internalCommands.Add(new InternalCommandItem("Next Track", AppConstants.MediaCommands.Next));
            _internalCommands.Add(new InternalCommandItem("Previous Track", AppConstants.MediaCommands.Previous));
            _internalCommands.Add(new InternalCommandItem("Play/Pause", AppConstants.MediaCommands.PlayPause));

            // App commands
            _internalCommands.Add(new InternalCommandItem("Add new item", AppConstants.CommandPrefixes.AddCommand));
            _internalCommands.Add(new InternalCommandItem("Add new command (advanced)", AppConstants.CommandPrefixes.AddCommandAdvanced));
            _internalCommands.Add(new InternalCommandItem("Settings", AppConstants.CommandPrefixes.SettingsCommand));
            _internalCommands.Add(new InternalCommandItem("Open last conversation", AppConstants.CommandPrefixes.OpenLastConversationCommand));
            _internalCommands.Add(new InternalCommandItem("Exit app", AppConstants.CommandPrefixes.ExitCommand));
        }

        private void InitializeWindow()
        {
            _windowHandle = WindowNative.GetWindowHandle(this);
            ItemsList.ItemsSource = _filteredItems;

            ConfigureWindowStyle();

            // Cache DPI scale once (avoids repeated P/Invoke calls)
            _cachedDpiScale = GetDpiScaleForWindow();
            var scaledWidth = (int)(WINDOW_WIDTH * _cachedDpiScale);
            var scaledHeight = (int)(WINDOW_HEIGHT * _cachedDpiScale);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));

            CenterWindow();
            SubclassWindow();
            ApplyBackdrop();

            Activated += OnWindowActivated;
            AppWindow.Hide();
        }

        private double GetDpiScaleForWindow()
        {
            try
            {
                var dpi = GetDpiForWindow(_windowHandle);
                return dpi / 96.0; // 96 DPI is the baseline (100% scaling)
            }
            catch
            {
                return 1.0; // Fallback to no scaling
            }
        }

        private void ApplyBackdrop()
        {
            // Prefer Mica (matches Windows 11 system surfaces, less drift than Acrylic when the window moves).
            // Fall back to Desktop Acrylic on systems where Mica isn't supported (e.g., older Win10 builds).
            try
            {
                if (MicaController.IsSupported())
                {
                    SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
                    return;
                }
            }
            catch
            {
                // Fall through to Acrylic.
            }

            try
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            catch
            {
                // Last resort: leave default.
            }
        }

        private void InitializeHotkey()
        {
            _hotkeyService = new GlobalHotkeyService();
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _ = LoadAndRegisterHotkeyAsync();
        }
        #endregion

        #region Window Configuration
        private void ConfigureWindowStyle()
        {
            if (AppWindow.TitleBar != null)
            {
                AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonForegroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.Transparent;
                AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            }

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }

        private void CenterWindow()
        {
            CenterWindowOnCurrentMonitor();
        }

        private void CenterWindowOnCurrentMonitor()
        {
            // Get cursor position to determine which monitor to use
            GetCursorPos(out POINT cursorPos);

            // Get display area from cursor position (uses the monitor containing the cursor)
            var displayArea = DisplayArea.GetFromPoint(
                new Windows.Graphics.PointInt32(cursorPos.X, cursorPos.Y),
                DisplayAreaFallback.Nearest);

            // Calculate window size for this monitor (auto-scales for smaller screens)
            var (scaledWidth, scaledHeight) = CalculateWindowSize(displayArea);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));

            // Center on the monitor's work area (accounts for taskbar)
            var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - scaledWidth) / 2;
            var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - scaledHeight) / 2;

            AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
        }

        private (int width, int height) CalculateWindowSize(DisplayArea displayArea)
        {
            var dpiScale = _cachedDpiScale > 0 ? _cachedDpiScale : GetDpiScaleForWindow();
            var workArea = displayArea.WorkArea;

            // Calculate effective screen size (without DPI scaling)
            var effectiveScreenWidth = workArea.Width / dpiScale;

            int targetWidth, targetHeight;

            if (effectiveScreenWidth < 1600) // Laptop or smaller screen
            {
                // Use 50% of screen width for smaller screens, maintaining 2:1 aspect ratio
                targetWidth = (int)(effectiveScreenWidth * 0.50);
                targetHeight = targetWidth / 2;

                // Ensure minimum size
                targetWidth = Math.Max(targetWidth, 400);
                targetHeight = Math.Max(targetHeight, 200);
            }
            else
            {
                // Use base dimensions for larger screens
                targetWidth = WINDOW_WIDTH;
                targetHeight = WINDOW_HEIGHT;
            }

            // Apply DPI scaling for the final pixel size
            return ((int)(targetWidth * dpiScale), (int)(targetHeight * dpiScale));
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (AppWindow.Presenter is not OverlappedPresenter presenter) return;

            // Stay on top only when focused
            try
            {
                presenter.IsAlwaysOnTop = args.WindowActivationState != WindowActivationState.Deactivated;
            }
            catch (Exception ex)
            {
                // Ignore exceptions when setting IsAlwaysOnTop - this is not critical functionality
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to set IsAlwaysOnTop: {ex.Message}");
            }

            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                // Reset markdown state when window loses focus
                if (MarkdownPanel.Visibility == Visibility.Visible)
                {
                    HideMarkdownPanel();
                }
                AppWindow.Hide();
                return;
            }

            SearchBox.Focus(FocusState.Programmatic);
        }
        #endregion

        #region Window Subclassing & Hotkey Handling
        private void SubclassWindow()
        {
            _newWndProc = NewWindowProc;
            _oldWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                _hotkeyService?.OnHotkeyMessage(wParam.ToInt32());
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void OnGlobalHotkeyPressed(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                // Reposition window on the monitor where the cursor is located.
                CenterWindowOnCurrentMonitor();

                // DWM-cloak the window before Show() so the user never sees the white
                // pre-XAML frame. We uncloak after the first low-priority dispatcher tick,
                // by which point WinUI has rendered the actual content.
                try { DwmInterop.Cloak(_windowHandle); } catch { /* non-fatal */ }

                AppWindow.Show();
                Activate();
                SetForegroundWindow(_windowHandle);

                // Reset state for animation. Stop any in-flight exit storyboard so it
                // doesn't keep animating Opacity towards 0 while we fade in.
                try { WindowExitAnimation.Stop(); } catch { }
                _isHidingAnimating = false;
                RootGrid.Opacity = 0;
                SearchBox.Text = string.Empty;

                // Focus immediately so keystrokes register the moment the window appears.
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.SelectAll();

                // Uncloak + animate after the current render batch. This used to run at
                // DispatcherQueuePriority.Low, which gets starved by show-time layout/filter
                // work — leaving the window cloaked and unfocused for ~1s, so typing didn't
                // register on almost every open. Normal priority can't be starved that way.
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    try { DwmInterop.Uncloak(_windowHandle); } catch { /* non-fatal */ }
                    WindowEnterAnimation.Begin();
                    // Re-focus in case the immediate focus above lost the race with activation.
                    SearchBox.Focus(FocusState.Programmatic);
                    SearchBox.SelectAll();
                });
            });
        }
        #endregion

        #region Data Loading
        private async Task LoadDataAsync()
        {
            LogPerf("LoadDataAsync start");
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                // Ensure commands file exists first (required before loading commands)
                await _commandService.EnsureCommandsFileExistsAsync();

                // Load independent data sources in parallel for faster startup
                var usageTask = _usageTrackingService.LoadAsync();
                var itemsTask = _dataService.LoadItemsAsync();
                var commandsTask = _commandService.LoadCommandsAsync();
                var settingsTask = _dataService.LoadSettingsAsync();
                var chatsTask = _chatHistoryService.LoadAsync();

                await Task.WhenAll(usageTask, itemsTask, commandsTask, settingsTask, chatsTask);
                LogPerf("All data loaded (parallel)");

                // Process results
                var items = await itemsTask;
                _allItems.Clear();
                foreach (var item in items)
                {
                    _allItems.Add(item);
                }

                _userCommands = await commandsTask;

                RefreshChatHistoryItems();

                // Own the loaded settings via SettingsService and configure dependent state.
                var settings = await settingsTask;
                _hideFooter = settings.HideFooter;
                _searchUrl = settings.SearchUrl;
                EnsureSettingsService(settings);
                ConfigureGrokFromSettings(settings);
                UpdateFooterVisibility();

                FilterItems();
                LogPerf("LoadDataAsync complete - app ready");
                WriteStartupLog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataAsync ERROR: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadAndRegisterHotkeyAsync()
        {
            try
            {
                var settings = _settingsService?.Current ?? await _dataService.LoadSettingsAsync();
                TryRegisterHotkey(settings.HotkeyModifiers, settings.HotkeyKey);
            }
            catch
            {
                // If loading fails, use defaults
                _hotkeyService?.RegisterHotkey(_windowHandle);
            }
        }
        #endregion

        #region Filtering & Search
        private sealed class UsageSnapshot : IUsageScoreProvider
        {
            private readonly Dictionary<IListItem, double> _scores;
            public UsageSnapshot(IEnumerable<IListItem> items, UsageTrackingService service)
            {
                _scores = new Dictionary<IListItem, double>();
                foreach (var item in items) _scores[item] = service.GetUsageScore(item);
            }
            public double GetUsageScore(IListItem item) => _scores.TryGetValue(item, out var s) ? s : 0;
        }

        // Sync entrypoint for callers that don't drive a typing loop (LoadDataAsync,
        // OnSaveEdit, OnDeleteClicked, OnHideFooterChanged, etc.). They just want a fresh
        // filter run; cancellation isn't meaningful for them.
        private async void FilterItems() => await FilterItemsCore(CancellationToken.None);

        private async Task FilterItemsCore(CancellationToken ct)
        {
            var rawText = SearchBox.Text ?? string.Empty;
            var lowerText = rawText.ToLowerInvariant();
            var trimmed = rawText.Trim();
            var isEmpty = string.IsNullOrWhiteSpace(rawText);

            // User-command path is unchanged: it streams its own results via DirectoryCommandProvider.
            if (!isEmpty && lowerText.StartsWith(AppConstants.CommandPrefixes.UserCommandPrefix))
            {
                if (lowerText == AppConstants.CommandPrefixes.UserCommandPrefix ||
                    !_userCommands.Any(c => c.Prefix.Equals(lowerText.Split(' ')[0], StringComparison.OrdinalIgnoreCase)))
                {
                    ShowCommandSuggestions(lowerText);
                    return;
                }

                await HandleUserCommandAsync(lowerText);
                return;
            }

            var includeInternalCommands = _hideFooter || !isEmpty;

            // Snapshot collections + usage scores on the UI thread before going to background.
            // Chat history items ride along with internal commands so they behave identically
            // (findable by typing part of their title; hidden in the empty-query state).
            var itemsSnapshot = _allItems.ToList();
            var internalSnapshot = _internalCommands.Cast<IListItem>().Concat(_chatHistoryItems).ToList();
            var allForUsage = itemsSnapshot.Concat(internalSnapshot);
            var usage = new UsageSnapshot(allForUsage, _usageTrackingService);

            var options = new FilterOptions
            {
                MaxResults = 6,
                IncludeInternalCommands = includeInternalCommands,
                UsageWeight = 1.0,
                FuzzyWeight = 1.0
            };

            List<IListItem> newItems;
            try
            {
                newItems = await Task.Run(
                    () => ItemFilter.Filter(itemsSnapshot, internalSnapshot, trimmed, usage, options, ct),
                    ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Bail out if a newer keystroke superseded us between Task.Run finishing and
            // the UI thread picking us back up.
            if (ct.IsCancellationRequested) return;

            // Empty-result fallback: command-execute suggestion or web/AI search suggestion.
            if (newItems.Count == 0 && !isEmpty)
            {
                if (lowerText.StartsWith(AppConstants.CommandPrefixes.CommandPrefix))
                {
                    newItems.Add(new CommandItem("Execute command", lowerText));
                }
                else
                {
                    _searchSuggestionItem.SearchQuery = rawText;
                    _searchSuggestionItem.SearchUrl = _searchUrl;
                    newItems.Add(_searchSuggestionItem);
                }
            }

            UpdateFilteredItems(newItems);

            if (_filteredItems.Count > 0)
            {
                ItemsList.SelectedIndex = 0;
            }
        }

        private void ShowCommandSuggestions(string searchText)
        {
            var query = searchText.TrimStart('/').ToLowerInvariant();

            _filteredItems.Clear();

            // Filter commands by prefix
            var matchingCommands = _userCommands
                .Where(c => string.IsNullOrEmpty(query) || c.Prefix.ToLowerInvariant().Contains(query))
                .Take(6);

            foreach (var cmd in matchingCommands)
            {
                _filteredItems.Add(new CommandSuggestionItem(cmd));
            }

            if (_filteredItems.Count > 0)
            {
                ItemsList.SelectedIndex = 0;
            }
        }

        private async Task HandleUserCommandAsync(string searchText)
        {
            // Extract command prefix and search query
            var parts = searchText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var commandPrefix = parts.Length > 0 ? parts[0] : searchText;
            var query = parts.Length > 1 ? parts[1] : string.Empty;

            // Find matching command
            var command = _userCommands.FirstOrDefault(c =>
                c.Prefix.Equals(commandPrefix, StringComparison.OrdinalIgnoreCase));

            if (command == null)
            {
                _filteredItems.Clear();
                return;
            }

            // Get items from provider based on source type
            List<UserCommandResultItem> resultItems;

            switch (command.Source)
            {
                case CommandSourceType.Directory:
                    resultItems = string.IsNullOrWhiteSpace(query)
                        ? await _directoryProvider.GetItemsAsync(command.SourceConfig, command.ExecuteTemplate, command.OpenInTerminal, 6)
                        : await _directoryProvider.SearchItemsAsync(command.SourceConfig, command.ExecuteTemplate, command.OpenInTerminal, query, 6);
                    break;

                case CommandSourceType.Static:
                    resultItems = command.SourceConfig.Items
                        .Where(item => string.IsNullOrWhiteSpace(query) ||
                                     item.Contains(query, StringComparison.OrdinalIgnoreCase))
                        .Take(6)
                        .Select(item => new UserCommandResultItem(
                            name: item,
                            path: item,
                            extension: string.Empty,
                            displayName: item,
                            icon: command.IconDisplay,
                            executeTemplate: command.ExecuteTemplate,
                            openInTerminal: command.OpenInTerminal
                        ))
                        .ToList();
                    break;

                default:
                    resultItems = new List<UserCommandResultItem>();
                    break;
            }

            // If no results found but a query was provided, create a fallback item
            // that executes the template with the query text directly
            if (resultItems.Count == 0 && !string.IsNullOrWhiteSpace(query))
            {
                resultItems.Add(new UserCommandResultItem(
                    name: query,
                    path: query,
                    extension: string.Empty,
                    displayName: $"{command.Prefix} {query}",
                    icon: command.IconDisplay,
                    executeTemplate: command.ExecuteTemplate,
                    openInTerminal: command.OpenInTerminal
                ));
            }

            UpdateFilteredItems(resultItems.Cast<IListItem>().ToList());

            if (_filteredItems.Count > 0)
            {
                ItemsList.SelectedIndex = 0;
            }
        }

        private void UpdateFilteredItems(List<IListItem> newItems)
        {
            // Always re-assign each slot so ItemsControl re-evaluates bindings
            // (TitleHighlights changes per query and IListItem doesn't raise INotifyPropertyChanged).

            // Remove items from the end that are no longer needed
            while (_filteredItems.Count > newItems.Count)
            {
                _filteredItems.RemoveAt(_filteredItems.Count - 1);
            }

            for (int i = 0; i < newItems.Count; i++)
            {
                if (i < _filteredItems.Count)
                {
                    _filteredItems[i] = newItems[i];
                }
                else
                {
                    _filteredItems.Add(newItems[i]);
                }
            }
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Swap CTS atomically so concurrent keystrokes always see consistent state, then
            // cancel + dispose the previous one to avoid accumulating CancellationTokenSource
            // instances (each registers a kernel timer when used with Task.Delay).
            var oldCts = _searchDebounceTokenSource;
            _searchDebounceTokenSource = new CancellationTokenSource();
            var token = _searchDebounceTokenSource.Token;

            if (oldCts != null)
            {
                try { oldCts.Cancel(); } catch (ObjectDisposedException) { }
                oldCts.Dispose();
            }

            try
            {
                await Task.Delay(SEARCH_DEBOUNCE_MS, token);
                if (token.IsCancellationRequested) return;

                // Pass the captured token through explicitly so that even if
                // _searchDebounceTokenSource is replaced again before FilterItemsCore reads it,
                // this run is still cancelable.
                await FilterItemsCore(token);
            }
            catch (TaskCanceledException)
            {
                // Expected when typing quickly.
            }
            catch (OperationCanceledException)
            {
                // Expected when typing quickly.
            }
        }

        private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isEditing || SearchBox.FocusState.HasFlag(FocusState.Keyboard)) return;

            var key = e.Key;
            if (IsAlphanumericKey(key))
            {
                SearchBox.Focus(FocusState.Keyboard);
            }
        }

        private static bool IsAlphanumericKey(Windows.System.VirtualKey key)
        {
            return (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z) ||
                   (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9) ||
                   key == Windows.System.VirtualKey.Space;
        }

        #region Performance Logging
        private static readonly List<(long ElapsedMs, string Label)> _perfLog = new();

        private static void LogPerf(string label)
        {
            var elapsed = _startupStopwatch.ElapsedMilliseconds;
            _perfLog.Add((elapsed, label));
            System.Diagnostics.Debug.WriteLine($"[PERF] {elapsed,6}ms  {label}");
        }

        private static void WriteStartupLog()
        {
            try
            {
                var logPath = Path.Combine(
                    Services.Helpers.ServiceInitializer.AppDataFolderPath,
                    "startup-perf.log");
                var lines = new List<string>
                {
                    $"=== Startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==="
                };
                long prev = 0;
                foreach (var (elapsed, label) in _perfLog)
                {
                    lines.Add($"  {elapsed,6}ms (+{elapsed - prev,4}ms)  {label}");
                    prev = elapsed;
                }
                lines.Add($"  Total: {_perfLog[^1].ElapsedMs}ms");
                lines.Add("");
                File.AppendAllLines(logPath, lines);
            }
            catch { }
        }
        #endregion
        #endregion

        #region IExecutionContext Implementation
        public async Task OpenUrlAsync(string url)
        {
            await _clipboardService.OpenUrlAsync(url);
        }

        public void CopyToClipboard(string text)
        {
            _clipboardService.CopyToClipboard(text);
        }

        public async Task ExecuteCommandAsync(string command)
        {
            await ExecuteCommandInternalAsync(command);
        }

        public async Task ExecuteCommandInTerminalAsync(string command)
        {
            // Execute command in a visible terminal window
            System.Diagnostics.Debug.WriteLine($"ExecuteCommandInTerminalAsync: Opening terminal with command: {command}");

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "pwsh.exe",
                        Arguments = $"-NoExit -Command \"{command}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    var process = Process.Start(psi);
                    System.Diagnostics.Debug.WriteLine($"ExecuteCommandInTerminalAsync: Process started: {process != null}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExecuteCommandInTerminalAsync ERROR: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"ExecuteCommandInTerminalAsync STACK: {ex.StackTrace}");
                }
            });
        }

        public async Task ExecuteMediaCommandAsync(string command)
        {
            await TryExecuteMediaCommandAsync(command);
        }

        public void HideWindow()
        {
            // Reset markdown state when hiding
            if (MarkdownPanel.Visibility == Visibility.Visible)
            {
                HideMarkdownPanel();
            }

            HideWithExitAnimation();
        }

        private void HideWithExitAnimation()
        {
            if (_isHidingAnimating) return;

            // If the window isn't visible (or storyboard is unavailable), just hide.
            if (WindowExitAnimation == null || !AppWindow.IsVisible)
            {
                SearchBox.Text = string.Empty;
                AppWindow.Hide();
                return;
            }

            _isHidingAnimating = true;

            void OnCompleted(object? sender, object e)
            {
                WindowExitAnimation.Completed -= OnCompleted;
                if (!_isHidingAnimating) return; // user re-opened window mid-exit
                SearchBox.Text = string.Empty;
                AppWindow.Hide();
                _isHidingAnimating = false;
            }

            WindowExitAnimation.Completed += OnCompleted;
            WindowExitAnimation.Begin();
        }

        void IExecutionContext.HideWindow()
        {
            HideWindow();
        }

        void IExecutionContext.ShowEditPanel(IEditableItem? item)
        {
            ShowEditPanelInternal(item);
        }

        void IExecutionContext.ShowCommandPanel(UserCommand? command)
        {
            ShowCommandPanelInternal(command);
        }

        void IExecutionContext.ShowSettingsPanel()
        {
            OpenSettingsWindow();
        }

        void IExecutionContext.ShowMarkdownPanel()
        {
            ShowMarkdownPanelInternal();
        }

        void IExecutionContext.ShowMarkdownPanelWithQuery(string query)
        {
            ShowMarkdownPanelInternal();
            _ = SendInitialQueryAsync(query);
        }

        void IExecutionContext.RestoreLastConversation()
        {
            RestoreLastConversationInternal();
        }

        void IExecutionContext.OpenChatSession(string sessionId)
        {
            OpenChatSessionInternal(sessionId);
        }

        public bool HasApiKey() => !string.IsNullOrEmpty(_apiKey);

        public void ExitApplication()
        {
            _hotkeyService?.Dispose();
            Application.Current.Exit();
        }
        #endregion

        #region Keyboard Accelerators
        private void OnEscapePressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_isEditing)
                HideEditPanel();
            else
                HideWindow(); // route through exit animation

            args.Handled = true;
        }

        private void OnToggleVisibility(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            HideWindow(); // route through exit animation
            args.Handled = true;
        }

        private void OnDownArrow(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_filteredItems.Count > 0 && ItemsList.SelectedIndex < _filteredItems.Count - 1)
            {
                ItemsList.SelectedIndex++;
                ItemsList.Focus(FocusState.Keyboard);
            }
            args.Handled = true;
        }

        private void OnUpArrow(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_filteredItems.Count > 0 && ItemsList.SelectedIndex > 0)
            {
                ItemsList.SelectedIndex--;
                ItemsList.Focus(FocusState.Keyboard);
            }
            args.Handled = true;
        }

        private void OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ItemsList.SelectedItem is IListItem selectedItem)
            {
                _ = ExecuteItemAsync(selectedItem);
            }
            else if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                _ = HandleNoMatchAsync(SearchBox.Text.Trim());
            }

            args.Handled = true;
        }

        private async Task ExecuteItemAsync(IListItem item)
        {
            // Handle autocomplete for command suggestions
            if (item.SupportsAutocomplete)
            {
                SearchBox.Text = item.AutocompleteText;
                SearchBox.SelectionStart = SearchBox.Text.Length;
                SearchBox.Focus(FocusState.Programmatic);
                return;
            }

            // Record usage for ranking (before execution)
            await _usageTrackingService.RecordUsageAsync(item);

            // Delegate to the item's execute method
            await item.ExecuteAsync(this);
        }

        private async Task HandleNoMatchAsync(string searchText)
        {
            if (searchText.StartsWith(AppConstants.CommandPrefixes.CommandPrefix))
            {
                var command = searchText.TrimStart(AppConstants.CommandPrefixes.CommandPrefix[0]).Trim();
                await ExecuteCommandInternalAsync(command);
                HideWindow();
            }
            else
            {
                var query = Uri.EscapeDataString(searchText);
                var url = _searchUrl.Replace(AppConstants.DefaultSettings.QueryPlaceholder, query);
                await _clipboardService.OpenUrlAsync(url);
                HideWindow();
            }
        }

        private async Task ExecuteCommandInternalAsync(string command)
        {
            // Check if it's a media control command
            if (await TryExecuteMediaCommandAsync(command))
                return;

            // Execute user-defined command silently in background
            // Note: Commands are user-created and stored locally in the app's data.
            // The user is intentionally executing their own commands, so command injection
            // from untrusted sources is not a concern. All commands originate from the user.
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = command,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(psi);
                }
                catch
                {
                    // If direct execution fails, try with cmd.exe
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {command}",
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        Process.Start(psi);
                    }
                    catch
                    {
                        // Silently fail - command execution errors
                    }
                }
            });
        }

        private async Task<bool> TryExecuteMediaCommandAsync(string command)
        {
            switch (command.ToLowerInvariant())
            {
                case "next":
                case "media next":
                    await _mediaControlService.SkipToNextAsync();
                    return true;

                case "prev":
                case "previous":
                case "media prev":
                    await _mediaControlService.SkipToPreviousAsync();
                    return true;

                case "playpause":
                case "play":
                case "pause":
                case "media playpause":
                    await _mediaControlService.PlayPauseAsync();
                    return true;

                default:
                    return false;
            }
        }
        #endregion

        #region Edit Panel
        private void OnAddNewTapped(object sender, RoutedEventArgs e) => ShowEditPanelInternal(null);

        private void OnEditClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: IListItem item })
            {
                // If it's a command suggestion, find and edit the actual command
                if (item is CommandSuggestionItem cmdSuggestion && cmdSuggestion.RelatedCommand != null)
                {
                    ShowCommandPanelInternal(cmdSuggestion.RelatedCommand);
                    return;
                }

                // If it's an editable item, show edit panel
                if (item is IEditableItem editableItem)
                {
                    ShowEditPanelInternal(editableItem);
                }
            }
        }

        private void ShowEditPanelInternal(IEditableItem? item)
        {
            _isEditing = true;
            _editingItem = item as IListItem;

            if (item != null)
            {
                EditTitle.Text = item.Title;
                EditValue.Text = item.Value;
                EditEncrypt.IsChecked = item.IsEncrypted;
            }
            else
            {
                EditTitle.Text = string.Empty;
                EditValue.Text = string.Empty;
                EditEncrypt.IsChecked = false;
            }

            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
            EditTitle.Focus(FocusState.Programmatic);
        }

        private void HideEditPanel()
        {
            _isEditing = false;
            _editingItem = null;
            SearchBox.Visibility = Visibility.Visible;
            EditPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            UpdateFooterVisibility();
            SearchBox.Focus(FocusState.Programmatic);
        }
        private void OnCancelEdit(object sender, RoutedEventArgs e) => HideEditPanel();

        private void OnCancelEditKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            HideEditPanel();
            args.Handled = true;
        }

        private async void OnSaveEdit(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditValue.Text)) return;

            IEditableItem item;
            var value = EditValue.Text;
            var title = EditTitle.Text;
            var isEncrypted = EditEncrypt.IsChecked ?? false;

            // Determine type based on value
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                item = new LinkItem
                {
                    Title = title,
                    Value = value,
                    IsEncrypted = isEncrypted
                };
            }
            else if (value.StartsWith(">"))
            {
                item = new CommandItem
                {
                    Title = title,
                    Value = value,
                    IsEncrypted = isEncrypted
                };
            }
            else
            {
                item = new TextItem
                {
                    Title = title,
                    Value = value,
                    IsEncrypted = isEncrypted
                };
            }

            if (_editingItem != null)
            {
                await _dataService.UpdateItemAsync(_editingItem, item, _allItems.ToList());
                var index = _allItems.IndexOf(_editingItem);
                if (index >= 0)
                {
                    _allItems[index] = item;
                }
            }
            else
            {
                await _dataService.AddItemAsync(item, _allItems.ToList());
                _allItems.Add(item);
            }

            FilterItems();
            HideEditPanel();
        }

        private async void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: IListItem item })
            {
                // If it's a command suggestion, delete the actual command
                if (item is CommandSuggestionItem suggestionItem && suggestionItem.CommandPrefix.StartsWith(AppConstants.CommandPrefixes.UserCommandPrefix))
                {
                    var command = _userCommands.FirstOrDefault(c =>
                        c.Prefix.Equals(suggestionItem.CommandPrefix, StringComparison.OrdinalIgnoreCase));
                    if (command != null)
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Delete Command",
                            Content = $"Are you sure you want to delete the command '{command.Prefix}'?",
                            PrimaryButtonText = "Delete",
                            CloseButtonText = "Cancel",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = this.Content.XamlRoot
                        };

                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            await _commandService.DeleteCommandAsync(command, _userCommands);
                            _userCommands = await _commandService.LoadCommandsAsync();

                            // Refresh the command suggestions
                            ShowCommandSuggestions(SearchBox.Text);
                        }
                        return;
                    }
                }

                _allItems.Remove(item);
                await _dataService.DeleteItemAsync(item, _allItems.ToList());
                FilterItems();
            }
        }
        #endregion

        #region Command Panel
        private UserCommand? _editingCommand;

        private void ShowCommandPanelInternal(UserCommand? command = null)
        {
            _editingCommand = command;

            if (command != null)
            {
                // Edit mode
                CommandPrefix.Text = command.Prefix;
                CommandSourceCombo.SelectedIndex = command.Source == Models.CommandSourceType.Directory ? 0 : 1;
                CommandPath.Text = command.SourceConfig.Path;
                CommandGlob.Text = command.SourceConfig.Glob;
                CommandRecursive.IsChecked = command.SourceConfig.Recursive;
                CommandExecuteTemplate.Text = command.ExecuteTemplate;
                CommandIconCombo.SelectedIndex = (int)command.Icon;
                CommandOpenInTerminal.IsChecked = command.OpenInTerminal;

                // Populate static items list
                StaticItemsList.Items.Clear();
                foreach (var item in command.SourceConfig.Items)
                {
                    StaticItemsList.Items.Add(item);
                }
            }
            else
            {
                // Add mode - set defaults
                CommandPrefix.Text = "/";
                CommandSourceCombo.SelectedIndex = 0;
                CommandPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                CommandGlob.Text = "*.md";
                CommandRecursive.IsChecked = true;
                CommandExecuteTemplate.Text = string.Empty; // Empty by default
                CommandIconCombo.SelectedIndex = 0;
                CommandOpenInTerminal.IsChecked = false;
                StaticItemsList.Items.Clear();
            }

            // Wire up text changed events for live preview
            CommandExecuteTemplate.TextChanged -= OnCommandTemplateChanged;
            CommandExecuteTemplate.TextChanged += OnCommandTemplateChanged;
            CommandPath.TextChanged -= OnCommandPathChanged;
            CommandPath.TextChanged += OnCommandPathChanged;

            // Update preview
            UpdateCommandPreview();

            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            CommandPanel.Visibility = Visibility.Visible;
            CommandPrefix.Focus(FocusState.Programmatic);
        }

        private void HideCommandPanel()
        {
            _editingCommand = null;

            // Unsubscribe from text changed events
            CommandExecuteTemplate.TextChanged -= OnCommandTemplateChanged;
            CommandPath.TextChanged -= OnCommandPathChanged;

            SearchBox.Visibility = Visibility.Visible;
            CommandPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            UpdateFooterVisibility();
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void OnCommandTemplateChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCommandPreview();
        }

        private void OnCommandPathChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCommandPreview();
        }

        private void UpdateCommandPreview()
        {
            var template = CommandExecuteTemplate.Text ?? string.Empty;
            var path = CommandPath.Text ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Create a sample file path for preview
            var sampleFileName = "example.md";
            var samplePath = System.IO.Path.Combine(path, sampleFileName);

            // Replace placeholders
            var preview = template
                .Replace("{item.path}", samplePath)
                .Replace("{item.name}", sampleFileName)
                .Replace("{item.extension}", ".md")
                .Replace("{query}", "your search query");

            // Show placeholder if empty
            if (string.IsNullOrWhiteSpace(preview))
            {
                preview = "Enter a command template above to see preview";
                CommandPreviewText.Opacity = 0.5;
            }
            else
            {
                CommandPreviewText.Opacity = 0.8;
            }

            CommandPreviewText.Text = preview;
        }

        private void OnCancelCommandEdit(object sender, RoutedEventArgs e) => HideCommandPanel();

        private void OnCancelCommandEditKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            HideCommandPanel();
            args.Handled = true;
        }

        private void OnCommandSourceTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            // Null check - this can be called during XAML initialization before elements are ready
            if (DirectoryConfigPanel == null || StaticItemsConfigPanel == null)
                return;

            if (CommandSourceCombo.SelectedIndex == 0)
            {
                // Directory
                DirectoryConfigPanel.Visibility = Visibility.Visible;
                StaticItemsConfigPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Static
                DirectoryConfigPanel.Visibility = Visibility.Collapsed;
                StaticItemsConfigPanel.Visibility = Visibility.Visible;
            }
        }

        private void OnAddStaticItem(object sender, RoutedEventArgs e)
        {
            var newItem = NewStaticItemTextBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(newItem))
            {
                StaticItemsList.Items.Add(newItem);
                NewStaticItemTextBox.Text = string.Empty;
                NewStaticItemTextBox.Focus(FocusState.Programmatic);
            }
        }

        private void OnRemoveStaticItem(object sender, RoutedEventArgs e)
        {
            if (StaticItemsList.SelectedItem != null)
            {
                StaticItemsList.Items.Remove(StaticItemsList.SelectedItem);
            }
        }

        private void OnStaticItemTextBoxKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                OnAddStaticItem(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private async void OnSaveCommandEdit(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommandPrefix.Text) ||
                string.IsNullOrWhiteSpace(CommandExecuteTemplate.Text))
            {
                return;
            }

            var sourceType = CommandSourceCombo.SelectedIndex == 0
                ? Models.CommandSourceType.Directory
                : Models.CommandSourceType.Static;

            var iconType = CommandIconCombo.SelectedIndex switch
            {
                0 => Models.CommandIcon.Folder,
                1 => Models.CommandIcon.Web,
                2 => Models.CommandIcon.Script,
                3 => Models.CommandIcon.Document,
                _ => Models.CommandIcon.Folder
            };

            // Collect static items from the list
            var staticItems = new List<string>();
            foreach (var item in StaticItemsList.Items)
            {
                if (item is string str)
                    staticItems.Add(str);
            }

            var newCommand = new UserCommand
            {
                Prefix = CommandPrefix.Text,
                Source = sourceType,
                SourceConfig = new SourceConfig
                {
                    Path = CommandPath.Text ?? string.Empty,
                    Recursive = CommandRecursive.IsChecked ?? true,
                    Glob = CommandGlob.Text ?? "*.*",
                    Items = staticItems
                },
                ExecuteTemplate = CommandExecuteTemplate.Text,
                Icon = iconType,
                OpenInTerminal = CommandOpenInTerminal.IsChecked ?? false
            };

            if (_editingCommand != null)
            {
                await _commandService.UpdateCommandAsync(_editingCommand, newCommand, _userCommands);
            }
            else
            {
                await _commandService.AddCommandAsync(newCommand, _userCommands);
            }

            // Reload commands to refresh the list
            _userCommands = await _commandService.LoadCommandsAsync();

            HideCommandPanel();
            HideWindow();
        }
        #endregion

        #region Settings Panel
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => OpenSettingsWindow();

        private void EnsureSettingsService(AppSettings settings)
        {
            if (_settingsService != null) return;

            _settingsService = new SettingsService(_dataService, settings);
            _settingsService.HotkeyReapplyCallback = TryRegisterHotkey;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        private void ConfigureGrokFromSettings(AppSettings settings)
        {
            var config = settings.GetActiveProviderConfig();
            _grokService.Configure(settings.AiProvider, config);
            _apiKey = config.ApiKey ?? string.Empty;
        }

        private async void OpenSettingsWindow()
        {
            // Settings normally load during startup; ensure they exist if the window is
            // opened before LoadDataAsync completes.
            if (_settingsService == null)
            {
                var settings = await _dataService.LoadSettingsAsync();
                EnsureSettingsService(settings);
                ConfigureGrokFromSettings(settings);
            }

            HideWindow(); // hide the overlay for determinism (deactivation would hide it anyway)
            SettingsWindow.ShowOrActivate(_settingsService!);
        }

        // Invoked by SettingsService (and startup) to (re-)register the global hotkey.
        // Returns whether registration succeeded so the settings UI can report status.
        private bool TryRegisterHotkey(uint modifiers, uint key)
        {
            var ok = _hotkeyService?.RegisterHotkey(_windowHandle, modifiers, key) ?? false;
            if (ok)
            {
                _registeredHotkeyModifiers = modifiers;
                _registeredHotkeyKey = key;
            }
            return ok;
        }

        // Runs on the UI thread whenever the settings window saves a change.
        private void OnSettingsChanged()
        {
            if (_settingsService == null) return;

            var settings = _settingsService.Current;
            _hideFooter = settings.HideFooter;
            _searchUrl = settings.SearchUrl;
            UpdateFooterVisibility();
            FilterItems();
            ConfigureGrokFromSettings(settings);

            // Re-register the hotkey only when it actually changed. The Apply path already
            // registers it directly through TryApplyHotkey, so this is usually a no-op.
            if (settings.HotkeyModifiers != _registeredHotkeyModifiers ||
                settings.HotkeyKey != _registeredHotkeyKey)
            {
                TryRegisterHotkey(settings.HotkeyModifiers, settings.HotkeyKey);
            }
        }

        private void UpdateFooterVisibility()
        {
            FooterPanel.Visibility = _hideFooter ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            _hotkeyService?.Dispose();
            Application.Current.Exit();
        }
        #endregion

        #region Markdown Panel
        private void ShowMarkdownPanelInternal()
        {
            _markdownContent = string.Empty;
            _lastAssistantMessage = string.Empty;
            _grokService.ClearHistory();
            _activeChat = _chatHistoryService.StartNew();
            HideAllPanels();
            MarkdownPanel.Visibility = Visibility.Visible;
            MarkdownTextBlock.Text = "Ask me anything...";
            MarkdownInput.Focus(FocusState.Programmatic);
        }

        private async Task SendInitialQueryAsync(string query)
        {
            _markdownContent = $"> {query}\n\n";
            MarkdownTextBlock.Text = _markdownContent;
            await SimulateResponseAsync(query);
        }

        private void HideAllPanels()
        {
            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            CommandPanel.Visibility = Visibility.Collapsed;
            MarkdownPanel.Visibility = Visibility.Collapsed;
        }

        private void HideMarkdownPanel()
        {
            // Persist the active conversation before hiding.
            PersistActiveChat();

            MarkdownPanel.Visibility = Visibility.Collapsed;
            SearchBox.Visibility = Visibility.Visible;
            ItemsList.Visibility = Visibility.Visible;
            UpdateFooterVisibility();
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void RestoreLastConversationInternal()
        {
            // Reopen the most recent persisted session, or start fresh if there is none.
            var latest = _chatHistoryService.Sessions.FirstOrDefault();
            if (latest == null)
            {
                ShowMarkdownPanelInternal();
                return;
            }

            OpenChatSessionInternal(latest.Id);
        }

        private void OpenChatSessionInternal(string sessionId)
        {
            var session = _chatHistoryService.GetById(sessionId);
            if (session == null)
            {
                ShowMarkdownPanelInternal();
                return;
            }

            _activeChat = session;

            // Restore the model's conversation history (incl. system prompt) and rebuild the
            // rendered markdown in the exact "> user \n\n assistant" format the panel produces.
            _grokService.RestoreHistory(session.Messages.Select(m => (m.Role, m.Content)).ToList());
            _markdownContent = BuildMarkdownFromMessages(session.Messages);
            _lastAssistantMessage = session.Messages.LastOrDefault(m => m.Role == "assistant")?.Content ?? string.Empty;

            HideAllPanels();
            MarkdownPanel.Visibility = Visibility.Visible;
            MarkdownTextBlock.Text = _markdownContent;
            MarkdownScrollViewer.ChangeView(null, MarkdownScrollViewer.ScrollableHeight, null, false);
            MarkdownInput.Focus(FocusState.Programmatic);
        }

        // Reconstructs the panel markdown from stored messages, matching the format produced
        // by SendInitialQueryAsync / OnSendMarkdownInput / SimulateResponseAsync.
        private static string BuildMarkdownFromMessages(System.Collections.Generic.List<ChatMessageRecord> messages)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var m in messages)
            {
                if (m.Role == "system") continue;

                if (m.Role == "user")
                    sb.Append(sb.Length == 0 ? $"> {m.Content}\n\n" : $"\n\n> {m.Content}\n\n");
                else
                    sb.Append(m.Content);
            }
            return sb.ToString();
        }

        private async void OnSendMarkdownInput(object sender, RoutedEventArgs e)
        {
            var input = MarkdownInput.Text?.Trim();
            if (string.IsNullOrEmpty(input)) return;

            _markdownContent += $"\n\n> {input}\n\n";
            MarkdownTextBlock.Text = _markdownContent;
            MarkdownInput.Text = string.Empty;
            
            await SimulateResponseAsync(input);
        }

        private void OnSendMarkdownInputKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            OnSendMarkdownInput(sender, new RoutedEventArgs());
            args.Handled = true;
        }

        private async Task SimulateResponseAsync(string question)
        {
            _lastAssistantMessage = string.Empty;

            // Setting MarkdownTextBlock.Text re-parses and rebuilds the entire document,
            // which is O(n^2) if done on every streamed token. Throttle to ~10fps and
            // always flush a final render so the complete response is shown.
            var sinceRender = System.Diagnostics.Stopwatch.StartNew();
            bool pendingRender = false;

            await _grokService.StreamResponseAsync(question, chunk =>
            {
                _lastAssistantMessage += chunk;
                _markdownContent += chunk;
                pendingRender = true;

                if (sinceRender.ElapsedMilliseconds >= 100)
                {
                    MarkdownTextBlock.Text = _markdownContent;
                    MarkdownScrollViewer.ChangeView(null, MarkdownScrollViewer.ScrollableHeight, null, false);
                    sinceRender.Restart();
                    pendingRender = false;
                }
                return Task.CompletedTask;
            });

            if (pendingRender)
            {
                MarkdownTextBlock.Text = _markdownContent;
                MarkdownScrollViewer.ChangeView(null, MarkdownScrollViewer.ScrollableHeight, null, false);
            }

            MarkdownInput.Focus(FocusState.Programmatic);

            // Persist the completed exchange (and auto-title on the first reply).
            PersistActiveChat();
        }

        // Syncs the active session from the model's conversation history, saves it, and (once
        // per session) kicks off background auto-titling. Fire-and-forget, UI-thread safe.
        private void PersistActiveChat()
        {
            if (_activeChat == null) return;

            var messages = _grokService.GetConversationHistory()
                .Select(m => new ChatMessageRecord { Role = m.role, Content = m.content })
                .ToList();

            if (messages.Count == 0) return;

            _activeChat.Messages = messages;
            _activeChat.UpdatedUtc = DateTime.UtcNow;

            var session = _activeChat;
            bool generateTitle = string.IsNullOrWhiteSpace(session.Title) && _titleRequested.Add(session.Id);

            _ = SaveSessionAndRefreshAsync(session, generateTitle);
        }

        private async Task SaveSessionAndRefreshAsync(ChatSession session, bool generateTitle)
        {
            try
            {
                await _chatHistoryService.SaveSessionAsync(session);
                RefreshChatHistoryItems();

                if (generateTitle)
                {
                    await GenerateAndApplyTitleAsync(session);
                }
            }
            catch
            {
                // Never surface persistence/titling failures to the UI.
            }
        }

        private async Task GenerateAndApplyTitleAsync(ChatSession session)
        {
            try
            {
                var userMessage = session.Messages.FirstOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
                var assistantReply = session.Messages.FirstOrDefault(m => m.Role == "assistant")?.Content ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userMessage)) return;

                string? title = null;
                try
                {
                    title = await _grokService.GenerateTitleAsync(userMessage, assistantReply);
                }
                catch
                {
                    // Fall through to the truncated-message fallback.
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = BuildFallbackTitle(userMessage);
                }

                await _chatHistoryService.SetTitleAsync(session.Id, title);
                RefreshChatHistoryItems();
            }
            catch
            {
                // Titling is best-effort; ignore failures.
            }
        }

        private static string BuildFallbackTitle(string message)
        {
            var trimmed = message.Trim();
            if (trimmed.Length <= 40) return trimmed;

            var slice = trimmed.Substring(0, 40);
            var lastSpace = slice.LastIndexOf(' ');
            if (lastSpace > 0) slice = slice.Substring(0, lastSpace);
            return slice.TrimEnd() + "…";
        }

        private void RefreshChatHistoryItems()
        {
            _chatHistoryItems.Clear();
            foreach (var session in _chatHistoryService.Sessions)
            {
                _chatHistoryItems.Add(new ChatHistoryItem(session));
            }
        }

        private void OnCopyPlainText(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastAssistantMessage))
            {
                var plainText = System.Text.RegularExpressions.Regex.Replace(_lastAssistantMessage, @"[*_`#>\[\]()\\-]|```[\s\S]*?```", "");
                plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\n\s*\n", "\n");
                _clipboardService.CopyToClipboard(plainText.Trim());
            }
        }
        #endregion

        #region Public Methods
        public bool HasNoCommands(int count) => count == 0;

        public int GetCommandCount() => _userCommands?.Count ?? 0;
        #endregion

        #region Command Management
        private void ShowAddCommandDialog()
        {
            ShowCommandPanelInternal(null);
        }

        private void OnAddCommandClicked(object sender, RoutedEventArgs e)
        {
            ShowAddCommandDialog();
        }

        private void OnEditCommandClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: UserCommand command })
            {
                ShowCommandPanelInternal(command);
            }
        }

        private async void OnDeleteCommandClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: UserCommand command })
            {
                var dialog = new ContentDialog
                {
                    Title = "Delete Command",
                    Content = $"Are you sure you want to delete the command '{command.Prefix}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await _commandService.DeleteCommandAsync(command, _userCommands);
                    _userCommands = await _commandService.LoadCommandsAsync();
                }
            }
        }
        #endregion
    }
}
