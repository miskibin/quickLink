using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using quickLink.Models;
using quickLink.Services;
using WinRT.Interop;

namespace quickLink
{
    public sealed partial class MainWindow : Window
    {
        #region Constants
        private const int WM_HOTKEY = 0x0312;
        private const int GWLP_WNDPROC = -4;
        
        // Win32 modifier flags
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        
        // Window dimensions
        private const int WINDOW_WIDTH = 600;
        private const int WINDOW_HEIGHT = 300;
        
        // Default hotkey
        private static readonly Windows.System.VirtualKeyModifiers DefaultHotkeyModifiers = 
            Windows.System.VirtualKeyModifiers.Control;
        private static readonly Windows.System.VirtualKey DefaultHotkeyKey = 
            Windows.System.VirtualKey.Space;
        #endregion

        #region Fields
        private readonly DataService _dataService;
        private readonly ClipboardService _clipboardService;
        private readonly ObservableCollection<ClipboardItem> _allItems;
        private readonly ObservableCollection<ClipboardItem> _filteredItems;
        
        private GlobalHotkeyService? _hotkeyService;
        private IntPtr _windowHandle;
        private ClipboardItem? _editingItem;
        private bool _isEditing;
        private bool _isInSettings;
        private Windows.System.VirtualKeyModifiers _newHotkeyModifiers = DefaultHotkeyModifiers;
        private Windows.System.VirtualKey _newHotkeyKey = DefaultHotkeyKey;
        
        // Window subclassing
        private WinProc? _newWndProc;
        private IntPtr _oldWndProc;
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
            _dataService = new DataService();
            _clipboardService = new ClipboardService();
            _allItems = new ObservableCollection<ClipboardItem>();
            _filteredItems = new ObservableCollection<ClipboardItem>();

            InitializeComponent();
            InitializeWindow();
            InitializeHotkey();
            
            _ = LoadDataAsync();
        }

        private void InitializeWindow()
        {
            _windowHandle = WindowNative.GetWindowHandle(this);
            ItemsList.ItemsSource = _filteredItems;
            
            ConfigureWindowStyle();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(WINDOW_WIDTH, WINDOW_HEIGHT));
            CenterWindow();
            SubclassWindow();

            Activated += OnWindowActivated;
            AppWindow.Hide();
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
                var items = await _dataService.LoadItemsAsync();
                _allItems.Clear();
                
                foreach (var item in items)
                {
                    _allItems.Add(item);
                }
                
                FilterItems();
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
        private void FilterItems()
        {
            _filteredItems.Clear();
            var searchText = SearchBox.Text?.ToLowerInvariant() ?? string.Empty;
            
            var query = string.IsNullOrWhiteSpace(searchText)
                ? _allItems
                : _allItems.Where(item => ItemMatchesSearch(item, searchText));

            foreach (var item in query)
            {
                _filteredItems.Add(item);
            }

            // Auto-select first item
            if (_filteredItems.Count > 0)
            {
                ItemsList.SelectedIndex = 0;
            }
        }

        private static bool ItemMatchesSearch(ClipboardItem item, string searchText)
        {
            return (!string.IsNullOrWhiteSpace(item.Title) && 
                    item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrWhiteSpace(item.Value) && 
                    item.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => FilterItems();

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
            if (ItemsList.SelectedItem is ClipboardItem selectedItem)
            {
                _ = ExecuteItemAsync(selectedItem);
            }
            else if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                _ = HandleNoMatchAsync(SearchBox.Text.Trim());
            }
            
            args.Handled = true;
        }

        private async Task ExecuteItemAsync(ClipboardItem item)
        {
            if (item.IsCommand)
            {
                var command = item.Value.TrimStart('>').Trim();
                _ = ExecutePowerShellCommandAsync(command);
                AppWindow.Hide();
            }
            else if (item.IsLink)
            {
                await _clipboardService.OpenUrlAsync(item.Value);
                AppWindow.Hide();
            }
            else
            {
                _clipboardService.CopyToClipboard(item.Value);
                AppWindow.Hide();
            }
        }

        private async Task HandleNoMatchAsync(string searchText)
        {
            if (searchText.StartsWith(">"))
            {
                var command = searchText.TrimStart('>').Trim();
                _ = ExecutePowerShellCommandAsync(command);
                AppWindow.Hide();
            }
            else
            {
                var query = Uri.EscapeDataString(searchText);
                await _clipboardService.OpenUrlAsync($"https://chatgpt.com/?q={query}");
                AppWindow.Hide();
            }
        }

        private static Task ExecutePowerShellCommandAsync(string command)
        {
            return Task.Run(async () =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\"\"")}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                    }
                }
                catch
                {
                    // Silently fail - command execution errors
                }
            });
        }
        #endregion

        #region Edit Panel
        private void OnAddNewTapped(object sender, RoutedEventArgs e) => ShowEditPanel(null);

        private void OnEditClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: ClipboardItem item })
            {
                ShowEditPanel(item);
            }
        }

        private void ShowEditPanel(ClipboardItem? item)
        {
            _isEditing = true;
            _editingItem = item;
            
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
            FooterPanel.Visibility = Visibility.Visible;
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

            var item = new ClipboardItem
            {
                Title = EditTitle.Text,
                Value = EditValue.Text,
                IsEncrypted = EditEncrypt.IsChecked ?? false
            };

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
            if (sender is Button { Tag: ClipboardItem item })
            {
                _allItems.Remove(item);
                await _dataService.DeleteItemAsync(item, _allItems.ToList());
                FilterItems();
            }
        }
        #endregion

        #region Settings Panel
        private void OnSettingsClicked(object sender, RoutedEventArgs e) => ShowSettingsPanel();

        private void ShowSettingsPanel()
        {
            _isInSettings = true;
            LoadSettings();
            ApplyHotkeyButton.IsEnabled = false;
            
            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            FooterPanel.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void HideSettingsPanel()
        {
            _isInSettings = false;
            SearchBox.Visibility = Visibility.Visible;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            FooterPanel.Visibility = Visibility.Visible;
            SearchBox.Focus(FocusState.Programmatic);
        }

        private async void LoadSettings()
        {
            await LoadStartupSettingAsync();
            UpdateHotkeyDisplay();
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
        public void HideWindow() => AppWindow.Hide();
        #endregion
    }
}
