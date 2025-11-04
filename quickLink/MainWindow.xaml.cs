using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using quickLink.Models;
using quickLink.Models.ListItems;
using quickLink.Services;
using quickLink.Constants;
using Windows.UI;
using WinRT.Interop;

namespace quickLink
{
    public sealed partial class MainWindow : Window, IExecutionContext
    {
        #region Constants
        private const int WM_HOTKEY = 0x0312;
        private const int GWLP_WNDPROC = -4;
        
        // Win32 modifier flags
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        
        // Window dimensions - 1.2x bigger
        private const int WINDOW_WIDTH = 720;  // 600 * 1.2
        private const int WINDOW_HEIGHT = 360; // 300 * 1.2
        
        // Default hotkey
        private static readonly Windows.System.VirtualKeyModifiers DefaultHotkeyModifiers = 
            Windows.System.VirtualKeyModifiers.Control;
        private static readonly Windows.System.VirtualKey DefaultHotkeyKey = 
            Windows.System.VirtualKey.Space;
        #endregion

        #region Fields
        private readonly DataService _dataService;
        private readonly ClipboardService _clipboardService;
        private readonly MediaControlService _mediaControlService;
        private readonly CommandService _commandService;
        private readonly DirectoryCommandProvider _directoryProvider;
        private readonly UsageTrackingService _usageTrackingService;
        private readonly ObservableCollection<IListItem> _allItems;
        private readonly ObservableCollection<IListItem> _filteredItems;
        private readonly List<InternalCommandItem> _internalCommands;
        private readonly SearchSuggestionItem _searchSuggestionItem;
        private List<UserCommand> _userCommands;
        
        private GlobalHotkeyService? _hotkeyService;
        private IntPtr _windowHandle;
        private IListItem? _editingItem;
        private bool _isEditing;
        private bool _isInSettings;
        private bool _hideFooter;
        private string _searchUrl = AppConstants.DefaultSettings.DefaultSearchUrl;
        private Windows.System.VirtualKeyModifiers _newHotkeyModifiers = DefaultHotkeyModifiers;
        private Windows.System.VirtualKey _newHotkeyKey = DefaultHotkeyKey;
        
        // Performance optimization: cache the last search to avoid redundant filtering
        private string _lastSearchText = string.Empty;
        
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
        #endregion

        #region Constructor & Initialization
        public MainWindow()
        {
            try
            {
                _dataService = new DataService();
                _clipboardService = new ClipboardService();
                _mediaControlService = new MediaControlService();
                _commandService = new CommandService();
                _directoryProvider = new DirectoryCommandProvider();
                _usageTrackingService = new UsageTrackingService();
                _allItems = new ObservableCollection<IListItem>();
                _filteredItems = new ObservableCollection<IListItem>();
                _internalCommands = new List<InternalCommandItem>();
                _userCommands = new List<UserCommand>();
                _searchSuggestionItem = new SearchSuggestionItem();

                InitializeComponent();
                InitializeInternalCommands();
                InitializeWindow();
                InitializeHotkey();
                
                _ = LoadDataAsync();
                _ = InitializeMediaServiceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL ERROR: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeMediaServiceAsync()
        {
            try
            {
                await _mediaControlService.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Media service initialization failed: {ex.Message}");
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
            _internalCommands.Add(new InternalCommandItem("Exit app", AppConstants.CommandPrefixes.ExitCommand));
        }

        private void InitializeWindow()
        {
            _windowHandle = WindowNative.GetWindowHandle(this);
            ItemsList.ItemsSource = _filteredItems;
            
            ConfigureWindowStyle();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(WINDOW_WIDTH, WINDOW_HEIGHT));
            CenterWindow();
            SubclassWindow();
            ApplyGlassEffect();

            Activated += OnWindowActivated;
            AppWindow.Hide();
        }

        private void ApplyGlassEffect()
        {
            // The DesktopAcrylicBackdrop provides a nice glass effect
            // For additional customization, you could use Win2D effects, but
            // the backdrop combined with our semi-transparent overlays works well
            // for a dark theme glass appearance
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
            var displayArea = DisplayArea.GetFromWindowId(
                AppWindow.Id, DisplayAreaFallback.Primary);

            var centerX = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
            var centerY = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;

            AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            if (AppWindow.Presenter is not OverlappedPresenter presenter) return;
            
            // Stay on top only when focused
            presenter.IsAlwaysOnTop = args.WindowActivationState != WindowActivationState.Deactivated;
            
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                SearchBox.Focus(FocusState.Programmatic);
            }
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
                AppWindow.Show();
                SearchBox.Text = string.Empty;
                Activate();
                SetForegroundWindow(_windowHandle);
                
                // Trigger entrance animation
                WindowEnterAnimation.Begin();
                
                // Small delay to ensure window is fully shown before setting focus
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    SearchBox.Focus(FocusState.Programmatic);
                    SearchBox.SelectAll();
                });
            });
        }
        #endregion

        #region Data Loading
        private async Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            
            try
            {
                await _commandService.EnsureCommandsFileExistsAsync();
                
                // Load usage stats first
                await _usageTrackingService.LoadAsync();
                
                var items = await _dataService.LoadItemsAsync();
                _allItems.Clear();
                foreach (var item in items)
                {
                    _allItems.Add(item);
                }
                
                // Load user commands
                _userCommands = await _commandService.LoadCommandsAsync();
                
                // Load footer setting
                var settings = await _dataService.LoadSettingsAsync();
                _hideFooter = settings.HideFooter;
                _searchUrl = settings.SearchUrl;
                UpdateFooterVisibility();
                
                FilterItems();
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
                var settings = await _dataService.LoadSettingsAsync();
                (_newHotkeyModifiers, _newHotkeyKey) = ConvertFromWin32Modifiers(
                    settings.HotkeyModifiers, settings.HotkeyKey);
                
                _hotkeyService?.RegisterHotkey(_windowHandle, settings.HotkeyModifiers, settings.HotkeyKey);
            }
            catch
            {
                // If loading fails, use defaults
                _hotkeyService?.RegisterHotkey(_windowHandle);
            }
        }
        #endregion

        #region Filtering & Search
        private async void FilterItems()
        {
            var searchText = SearchBox.Text?.ToLowerInvariant() ?? string.Empty;
            var isEmpty = string.IsNullOrWhiteSpace(searchText);
            
            // Check if it's a user command
            if (!isEmpty && searchText.StartsWith(AppConstants.CommandPrefixes.UserCommandPrefix))
            {
                // Show command suggestions if just "/" typed or partial command
                if (searchText == AppConstants.CommandPrefixes.UserCommandPrefix || !_userCommands.Any(c => c.Prefix.Equals(searchText.Split(' ')[0], StringComparison.OrdinalIgnoreCase)))
                {
                    ShowCommandSuggestions(searchText);
                    return;
                }
                
                await HandleUserCommandAsync(searchText);
                return;
            }
            
            // Pre-calculate whether to include internal commands
            var includeInternalCommands = _hideFooter || !isEmpty;
            
            // Optimize: avoid multiple enumerations
            List<IListItem> newItems;
            
            if (isEmpty)
            {
                // No search - take first 6 items sorted by usage, plus internal commands if needed
                _filteredItems.Clear();
                
                newItems = new List<IListItem>(7); // Pre-allocate capacity
                
                // Sort all items by usage score (descending)
                var sortedItems = _allItems
                    .Select(item => new { Item = item, Score = _usageTrackingService.GetUsageScore(item) })
                    .OrderByDescending(x => x.Score)
                    .Take(6)
                    .Select(x => x.Item);
                
                foreach (var item in sortedItems)
                {
                    _filteredItems.Add(item);
                }
                
                if (includeInternalCommands)
                {
                    foreach (var cmd in _internalCommands)
                    {
                        if (_filteredItems.Count >= 6) break;
                        _filteredItems.Add(cmd);
                    }
                }
                
                // Auto-select first item
                if (_filteredItems.Count > 0)
                {
                    ItemsList.SelectedIndex = 0;
                }
                return; // Early exit for empty search
            }
            else
            {
                // With search - filter, calculate combined score (text match + usage), and sort
                var capacity = Math.Min(_allItems.Count + (includeInternalCommands ? _internalCommands.Count : 0), 7);
                var scoredItems = new List<(IListItem Item, double Score)>(capacity);
                
                // Search user items with scoring
                foreach (var item in _allItems)
                {
                    if (item.MatchesSearch(searchText))
                    {
                        // Calculate text match score (1.0 for exact match, 0.5 for partial)
                        var textScore = item.DisplayValue?.ToLowerInvariant().StartsWith(searchText) == true ? 1.0 : 0.5;
                        
                        // Add usage score boost
                        var usageScore = _usageTrackingService.GetUsageScore(item);
                        var combinedScore = textScore + usageScore;
                        
                        scoredItems.Add((item, combinedScore));
                    }
                }
                
                // Search internal commands if needed
                if (includeInternalCommands)
                {
                    foreach (var cmd in _internalCommands)
                    {
                        if (cmd.MatchesSearch(searchText))
                        {
                            // Internal commands get text match score only (no usage tracking)
                            var textScore = cmd.DisplayValue?.ToLowerInvariant().StartsWith(searchText) == true ? 1.0 : 0.5;
                            scoredItems.Add((cmd, textScore));
                        }
                    }
                }
                
                // Sort by score descending and take top 6
                newItems = scoredItems
                    .OrderByDescending(x => x.Score)
                    .Take(6)
                    .Select(x => x.Item)
                    .ToList();
                
                // If no results and not a command, show search suggestion
                if (newItems.Count == 0)
                {
                    if (searchText.StartsWith(AppConstants.CommandPrefixes.CommandPrefix))
                    {
                        // Show execute command suggestion - create a simple command item
                        var executeCmd = new CommandItem("Execute command", searchText);
                        newItems.Add(executeCmd);
                    }
                    else
                    {
                        // Show search suggestion
                        _searchSuggestionItem.SearchQuery = searchText;
                        _searchSuggestionItem.SearchUrl = _searchUrl;
                        newItems.Add(_searchSuggestionItem);
                    }
                }
            }

            // Update filtered items
            UpdateFilteredItems(newItems);

            // Auto-select first item
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
            
            UpdateFilteredItems(resultItems.Cast<IListItem>().ToList());
            
            if (_filteredItems.Count > 0)
            {
                ItemsList.SelectedIndex = 0;
            }
        }
        
        private void UpdateFilteredItems(List<IListItem> newItems)
        {
            // Remove items from the end that are no longer needed
            while (_filteredItems.Count > newItems.Count)
            {
                _filteredItems.RemoveAt(_filteredItems.Count - 1);
            }
            
            // Update existing items or add new ones
            for (int i = 0; i < newItems.Count; i++)
            {
                if (i < _filteredItems.Count)
                {
                    // Update existing slot if different
                    if (!ReferenceEquals(_filteredItems[i], newItems[i]))
                    {
                        _filteredItems[i] = newItems[i];
                    }
                }
                else
                {
                    // Add new item
                    _filteredItems.Add(newItems[i]);
                }
            }
        }

        private static bool ItemsAreEqual(List<IListItem> newItems, ObservableCollection<IListItem> currentItems)
        {
            if (newItems.Count != currentItems.Count)
                return false;

            for (int i = 0; i < newItems.Count; i++)
            {
                if (!ReferenceEquals(newItems[i], currentItems[i]))
                    return false;
            }

            return true;
        }

        private static bool ItemMatchesSearch(IListItem item, string searchText)
        {
            return item.MatchesSearch(searchText);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Instant filtering for maximum responsiveness
            FilterItems();
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
            AppWindow.Hide();
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
            ShowSettingsPanelInternal();
        }

        public void ExitApplication()
        {
            _hotkeyService?.Dispose();
            Application.Current.Exit();
        }
        #endregion

        #region Keyboard Accelerators
        private void OnEscapePressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_isInSettings)
                HideSettingsPanel();
            else if (_isEditing)
                HideEditPanel();
            else
                AppWindow.Hide();
                
            args.Handled = true;
        }

        private void OnToggleVisibility(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppWindow.Hide();
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
                AppWindow.Hide();
            }
            else
            {
                var query = Uri.EscapeDataString(searchText);
                var url = _searchUrl.Replace(AppConstants.DefaultSettings.QueryPlaceholder, query);
                await _clipboardService.OpenUrlAsync(url);
                AppWindow.Hide();
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
        }        private void OnCancelEdit(object sender, RoutedEventArgs e) => HideEditPanel();

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
                CommandExecuteTemplate.Text = "code \"{item.path}\"";
                CommandIconCombo.SelectedIndex = 0;
                CommandOpenInTerminal.IsChecked = false;
                StaticItemsList.Items.Clear();
            }
            
            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            CommandPanel.Visibility = Visibility.Visible;
            CommandPrefix.Focus(FocusState.Programmatic);
        }

        private void HideCommandPanel()
        {
            _editingCommand = null;
            SearchBox.Visibility = Visibility.Visible;
            CommandPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            UpdateFooterVisibility();
            SearchBox.Focus(FocusState.Programmatic);
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
            AppWindow.Hide();
        }
        #endregion

        #region Settings Panel
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => ShowSettingsPanelInternal();

        private void ShowSettingsPanelInternal()
        {
            _isInSettings = true;
            LoadSettings();
            ApplyHotkeyButton.IsEnabled = false;
            
            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
            
            // Set focus to the first interactive element
            StartWithSystemCheckBox.Focus(FocusState.Programmatic);
        }

        private void HideSettingsPanel()
        {
            _isInSettings = false;
            SearchBox.Visibility = Visibility.Visible;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            UpdateFooterVisibility();
            SearchBox.Focus(FocusState.Programmatic);
        }

        private async void LoadSettings()
        {
            await LoadStartupSettingAsync();
            await LoadFooterSettingAsync();
            await LoadSearchUrlSettingAsync();
            UpdateHotkeyDisplay();
        }

        private async Task LoadSearchUrlSettingAsync()
        {
            try
            {
                var settings = await _dataService.LoadSettingsAsync();
                _searchUrl = settings.SearchUrl;
                SearchUrlTextBox.Text = settings.SearchUrl;
            }
            catch
            {
                _searchUrl = "https://chatgpt.com/?q={query}";
                SearchUrlTextBox.Text = _searchUrl;
            }
        }

        private async void OnSearchUrlChanged(object sender, TextChangedEventArgs e)
        {
            var newUrl = SearchUrlTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newUrl) || !newUrl.Contains("{query}"))
                return;

            _searchUrl = newUrl;
            
            // Save setting
            var settings = await _dataService.LoadSettingsAsync();
            settings.SearchUrl = _searchUrl;
            await _dataService.SaveSettingsAsync(settings);
        }

        private async Task LoadFooterSettingAsync()
        {
            try
            {
                var settings = await _dataService.LoadSettingsAsync();
                _hideFooter = settings.HideFooter;
                HideFooterCheckBox.IsChecked = settings.HideFooter;
                UpdateFooterVisibility();
            }
            catch
            {
                _hideFooter = true; // Default to hidden
                HideFooterCheckBox.IsChecked = true;
            }
        }

        private async Task LoadStartupSettingAsync()
        {
            try
            {
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("QuickLinkStartup");
                StartWithSystemCheckBox.IsChecked = 
                    startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled;
            }
            catch
            {
                StartWithSystemCheckBox.IsChecked = false;
                StartWithSystemCheckBox.IsEnabled = false;
            }
        }

        private async void OnStartWithSystemChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("QuickLinkStartup");
                
                if (StartWithSystemCheckBox.IsChecked == true)
                {
                    var state = await startupTask.RequestEnableAsync();
                    if (state != Windows.ApplicationModel.StartupTaskState.Enabled)
                    {
                        StartWithSystemCheckBox.IsChecked = false;
                    }
                }
                else
                {
                    startupTask.Disable();
                }
            }
            catch
            {
                StartWithSystemCheckBox.IsChecked = false;
            }
        }

        private async void OnHideFooterChanged(object sender, RoutedEventArgs e)
        {
            _hideFooter = HideFooterCheckBox.IsChecked ?? false;
            UpdateFooterVisibility();
            
            // Save setting
            var settings = await _dataService.LoadSettingsAsync();
            settings.HideFooter = _hideFooter;
            await _dataService.SaveSettingsAsync(settings);
            
            // Refresh filtered items to include/exclude internal commands
            FilterItems();
        }

        private void UpdateFooterVisibility()
        {
            FooterPanel.Visibility = _hideFooter ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnCloseSettings(object sender, RoutedEventArgs e) => HideSettingsPanel();

        private void OnCloseSettingsKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            HideSettingsPanel();
            args.Handled = true;
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            _hotkeyService?.Dispose();
            Application.Current.Exit();
        }
        #endregion

        #region Hotkey Configuration
        private void OnHotkeyKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Allow Tab key to navigate away
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                e.Handled = false;
                return;
            }
            
            e.Handled = true;
            var key = e.Key;
            
            if (IsModifierKey(key))
            {
                UpdateHotkeyDisplay();
                HotkeyStatusText.Text = "Add a regular key (letters, numbers, F-keys, etc.)";
                ApplyHotkeyButton.IsEnabled = false;
                return;
            }
            
            var modifiers = GetCurrentModifiers();
            
            if (modifiers == Windows.System.VirtualKeyModifiers.None)
            {
                HotkeyStatusText.Text = "⚠ Must include at least one modifier (Ctrl, Shift, or Alt)";
                ApplyHotkeyButton.IsEnabled = false;
                return;
            }
            
            _newHotkeyModifiers = modifiers;
            _newHotkeyKey = key;
            
            UpdateHotkeyDisplay();
            HotkeyStatusText.Text = "✓ Ready to apply — click the Apply button";
            ApplyHotkeyButton.IsEnabled = true;
        }

        private void OnHotkeyTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Reset the new hotkey values when losing focus without applying
            // This ensures that if user tabs away, they don't accidentally keep pending changes
            if (!ApplyHotkeyButton.IsEnabled)
            {
                _newHotkeyModifiers = Windows.System.VirtualKeyModifiers.None;
                _newHotkeyKey = Windows.System.VirtualKey.None;
            }
        }

        private static bool IsModifierKey(Windows.System.VirtualKey key)
        {
            return key is Windows.System.VirtualKey.Control or
                   Windows.System.VirtualKey.Shift or
                   Windows.System.VirtualKey.Menu or
                   Windows.System.VirtualKey.LeftControl or
                   Windows.System.VirtualKey.RightControl or
                   Windows.System.VirtualKey.LeftShift or
                   Windows.System.VirtualKey.RightShift or
                   Windows.System.VirtualKey.LeftMenu or
                   Windows.System.VirtualKey.RightMenu;
        }

        private static Windows.System.VirtualKeyModifiers GetCurrentModifiers()
        {
            var modifiers = Windows.System.VirtualKeyModifiers.None;
            
            try
            {
                if (IsKeyDown(Windows.System.VirtualKey.Control))
                    modifiers |= Windows.System.VirtualKeyModifiers.Control;
                    
                if (IsKeyDown(Windows.System.VirtualKey.Shift))
                    modifiers |= Windows.System.VirtualKeyModifiers.Shift;
                    
                if (IsKeyDown(Windows.System.VirtualKey.Menu))
                    modifiers |= Windows.System.VirtualKeyModifiers.Menu;
            }
            catch
            {
                // If we can't get key states, return None
            }
            
            return modifiers;
        }

        private static bool IsKeyDown(Windows.System.VirtualKey key)
        {
            var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(key);
            return (state & Windows.UI.Core.CoreVirtualKeyStates.Down) == 
                   Windows.UI.Core.CoreVirtualKeyStates.Down;
        }

        private void UpdateHotkeyDisplay()
        {
            var parts = new List<string>();
            
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
                parts.Add("Ctrl");
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
                parts.Add("Shift");
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Menu))
                parts.Add("Alt");
            
            if (_newHotkeyKey != Windows.System.VirtualKey.None)
                parts.Add(_newHotkeyKey.ToString());
            
            HotkeyTextBox.Text = parts.Count > 0 
                ? string.Join(" + ", parts) 
                : "Ctrl + Space (default)";
        }

        private void OnResetHotkey(object sender, RoutedEventArgs e)
        {
            _newHotkeyModifiers = DefaultHotkeyModifiers;
            _newHotkeyKey = DefaultHotkeyKey;
            UpdateHotkeyDisplay();
            ApplyHotkeyChange();
            HotkeyStatusText.Text = "Reset to default (Ctrl + Space) and applied";
            ApplyHotkeyButton.IsEnabled = false;
        }

        private void OnApplyHotkey(object sender, RoutedEventArgs e)
        {
            ApplyHotkeyChange();
            ApplyHotkeyButton.IsEnabled = false;
        }

        private async void ApplyHotkeyChange()
        {
            try
            {
                var (modifiers, vKey) = ConvertToWin32Modifiers(_newHotkeyModifiers, _newHotkeyKey);
                
                _hotkeyService?.RegisterHotkey(_windowHandle, modifiers, vKey);
                
                var settings = new AppSettings
                {
                    HotkeyModifiers = modifiers,
                    HotkeyKey = vKey
                };
                await _dataService.SaveSettingsAsync(settings);
                
                HotkeyStatusText.Text = "Hotkey updated and saved!";
            }
            catch (Exception ex)
            {
                HotkeyStatusText.Text = $"Failed to update hotkey: {ex.Message}";
            }
        }
        #endregion

        #region Hotkey Conversion Helpers
        private static (uint modifiers, uint key) ConvertToWin32Modifiers(
            Windows.System.VirtualKeyModifiers modifiers, 
            Windows.System.VirtualKey key)
        {
            uint win32Modifiers = 0;
            
            if (modifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
                win32Modifiers |= MOD_CONTROL;
            if (modifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
                win32Modifiers |= MOD_SHIFT;
            if (modifiers.HasFlag(Windows.System.VirtualKeyModifiers.Menu))
                win32Modifiers |= MOD_ALT;
            
            return (win32Modifiers, (uint)key);
        }

        private static (Windows.System.VirtualKeyModifiers modifiers, Windows.System.VirtualKey key) 
            ConvertFromWin32Modifiers(uint win32Modifiers, uint vKey)
        {
            var modifiers = Windows.System.VirtualKeyModifiers.None;
            
            if ((win32Modifiers & MOD_CONTROL) != 0)
                modifiers |= Windows.System.VirtualKeyModifiers.Control;
            if ((win32Modifiers & MOD_SHIFT) != 0)
                modifiers |= Windows.System.VirtualKeyModifiers.Shift;
            if ((win32Modifiers & MOD_ALT) != 0)
                modifiers |= Windows.System.VirtualKeyModifiers.Menu;
            
            return (modifiers, (Windows.System.VirtualKey)vKey);
        }
        #endregion

        #region Public Methods
        public bool HasNoCommands(int count) => count == 0;
        
        public int GetCommandCount() => _userCommands?.Count ?? 0;
        #endregion

        #region Command Management
        private async Task ShowAddCommandDialogAsync()
        {
            ShowCommandPanelInternal(null);
        }

        private async void OnAddCommandClicked(object sender, RoutedEventArgs e)
        {
            await ShowAddCommandDialogAsync();
        }

        private async void OnEditCommandClicked(object sender, RoutedEventArgs e)
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
