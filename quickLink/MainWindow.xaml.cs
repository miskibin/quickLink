using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using quickLink.Models;
using quickLink.Services;
using WinRT.Interop;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace quickLink
{
    public sealed partial class MainWindow : Window
    {
        private readonly DataService _dataService;
        private readonly ClipboardService _clipboardService;
        private ObservableCollection<ClipboardItem> _allItems;
        private ObservableCollection<ClipboardItem> _filteredItems;
        private GlobalHotkeyService _hotkeyService;
        private IntPtr _windowHandle;
        private const int WM_HOTKEY = 0x0312;
        private ClipboardItem? _editingItem;
        private bool _isEditing;
        private bool _isInSettings;
        private Windows.System.VirtualKeyModifiers _newHotkeyModifiers;
        private Windows.System.VirtualKey _newHotkeyKey;

        public MainWindow()
        {
            _dataService = new DataService();
            _clipboardService = new ClipboardService();
            _allItems = new ObservableCollection<ClipboardItem>();
            _filteredItems = new ObservableCollection<ClipboardItem>();
            
            // Initialize default hotkey
            _newHotkeyModifiers = Windows.System.VirtualKeyModifiers.Control;
            _newHotkeyKey = Windows.System.VirtualKey.Space;

            InitializeComponent();

            ItemsList.ItemsSource = _filteredItems;

            _windowHandle = WindowNative.GetWindowHandle(this);

            // Configure window for transparency and rounded corners
            ConfigureWindowStyle();

            // Compact modern window size
            AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 300));

            CenterWindow();

            _hotkeyService = new GlobalHotkeyService();
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _hotkeyService.RegisterHotkey(_windowHandle);

            // Subscribe to window messages
            SubclassWindow();
            _ = LoadDataAsync();

            Activated += (s, e) => 
            {
                SearchBox.Focus(FocusState.Programmatic);
                // Set always on top when activated
                var presenter = AppWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = true;
                }
            };

            // Remove always on top when deactivated
            this.Activated += OnWindowActivated;

            // Hide window initially (it will be shown via hotkey)
            AppWindow.Hide();
        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                // Only stay on top when focused
                presenter.IsAlwaysOnTop = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Auto-focus search box when typing (if not already editing)
            if (!_isEditing && !SearchBox.FocusState.HasFlag(FocusState.Keyboard))
            {
                var key = e.Key;
                // Check if it's a printable character
                if ((key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z) ||
                    (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9) ||
                    key == Windows.System.VirtualKey.Space)
                {
                    SearchBox.Focus(FocusState.Keyboard);
                }
            }
        }

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            var items = await _dataService.LoadItemsAsync();
            _allItems.Clear();
            foreach (var item in items)
            {
                _allItems.Add(item);
            }
            FilterItems();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private void FilterItems()
        {
            _filteredItems.Clear();
            var searchText = SearchBox.Text.ToLowerInvariant();
            var query = string.IsNullOrWhiteSpace(searchText)
                ? _allItems
                : _allItems.Where(item =>
                  (!string.IsNullOrWhiteSpace(item.Title) && item.Title.ToLowerInvariant().Contains(searchText)) ||
                  (!string.IsNullOrWhiteSpace(item.Value) && item.Value.ToLowerInvariant().Contains(searchText)));

            foreach (var item in query)
            {
                _filteredItems.Add(item);
            }

            // Auto-select first item
            if (_filteredItems.Any())
            {
                ItemsList.SelectedIndex = 0;
            }
        }

        private void CenterWindow()
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                this.AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

            var centerX = (displayArea.WorkArea.Width - AppWindow.Size.Width) / 2;
            var centerY = (displayArea.WorkArea.Height - AppWindow.Size.Height) / 2;

            AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
        }

        private delegate IntPtr WinProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WinProc? _newWndProc;
        private IntPtr _oldWndProc;

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4;

        private void SubclassWindow()
        {
            _newWndProc = new WinProc(NewWindowProc);
            _oldWndProc = SetWindowLongPtr(_windowHandle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY)
            {
                _hotkeyService.OnHotkeyMessage(wParam.ToInt32());
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void OnGlobalHotkeyPressed(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                AppWindow.Show();
                this.Activate();
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.SelectAll();
            });
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems();
        }

        private void OnEscapePressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (_isInSettings)
            {
                HideSettingsPanel();
            }
            else if (_isEditing)
            {
                HideEditPanel();
            }
            else
            {
                AppWindow.Hide();
            }
            args.Handled = true;
        }

        private void OnToggleVisibility(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppWindow.Hide();
            args.Handled = true;
        }

        private void OnDownArrow(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Move selection down
            if (_filteredItems.Any() && ItemsList.SelectedIndex < _filteredItems.Count - 1)
            {
                ItemsList.SelectedIndex++;
                ItemsList.Focus(FocusState.Keyboard);
            }
            args.Handled = true;
        }

        private void OnUpArrow(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Move selection up
            if (_filteredItems.Any() && ItemsList.SelectedIndex > 0)
            {
                ItemsList.SelectedIndex--;
                ItemsList.Focus(FocusState.Keyboard);
            }
            args.Handled = true;
        }

        private async void OnEnterPressed(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Execute the selected item
            if (ItemsList.SelectedItem is ClipboardItem selectedItem)
            {
                if (selectedItem.IsLink)
                {
                    await _clipboardService.OpenUrlAsync(selectedItem.Value);
                }
                else
                {
                    _clipboardService.CopyToClipboard(selectedItem.Value);
                }
                AppWindow.Hide();
            }
            // If no match found and user has typed something, pass to ChatGPT
            else if (!string.IsNullOrWhiteSpace(SearchBox.Text) && !_filteredItems.Any())
            {
                var query = Uri.EscapeDataString(SearchBox.Text);
                var chatGptUrl = $"https://chatgpt.com/?q={query}";
                await _clipboardService.OpenUrlAsync(chatGptUrl);
                AppWindow.Hide();
            }
            args.Handled = true;
        }

        private void OnAddNewTapped(object sender, RoutedEventArgs e)
        {
            ShowEditPanel(null);
        }

        private void OnEditClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClipboardItem item)
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
            SearchBox.Focus(FocusState.Programmatic);
        }

        private void OnCancelEdit(object sender, RoutedEventArgs e)
        {
            HideEditPanel();
        }

        private async void OnSaveEdit(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EditValue.Text))
            {
                return;
            }

            if (_editingItem != null)
            {
                // Update existing item
                var updatedItem = new ClipboardItem
                {
                    Title = EditTitle.Text,
                    Value = EditValue.Text,
                    IsEncrypted = EditEncrypt.IsChecked ?? false
                };
                
                await _dataService.UpdateItemAsync(_editingItem, updatedItem, _allItems.ToList());
                var index = _allItems.IndexOf(_editingItem);
                if (index >= 0)
                {
                    _allItems[index] = updatedItem;
                }
            }
            else
            {
                // Add new item
                var newItem = new ClipboardItem
                {
                    Title = EditTitle.Text,
                    Value = EditValue.Text,
                    IsEncrypted = EditEncrypt.IsChecked ?? false
                };
                
                await _dataService.AddItemAsync(newItem, _allItems.ToList());
                _allItems.Add(newItem);
            }

            FilterItems();
            HideEditPanel();
        }

        private async void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClipboardItem item)
            {
                _allItems.Remove(item);
                await _dataService.DeleteItemAsync(item, _allItems.ToList());
                FilterItems();
            }
        }

        public void HideWindow()
        {
            AppWindow.Hide();
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            ShowSettingsPanel();
        }

        private void ShowSettingsPanel()
        {
            _isInSettings = true;
            
            // Load current settings
            LoadSettings();
            
            SearchBox.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void HideSettingsPanel()
        {
            _isInSettings = false;
            SearchBox.Visibility = Visibility.Visible;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ItemsList.Visibility = Visibility.Visible;
            SearchBox.Focus(FocusState.Programmatic);
        }

        private async void LoadSettings()
        {
            // Load startup setting
            try
            {
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("QuickLinkStartup");
                StartWithSystemCheckBox.IsChecked = startupTask.State == Windows.ApplicationModel.StartupTaskState.Enabled;
            }
            catch
            {
                // Startup task not available
                StartWithSystemCheckBox.IsChecked = false;
                StartWithSystemCheckBox.IsEnabled = false;
            }
            
            // Display current hotkey
            UpdateHotkeyDisplay();
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
                        // Could show a message that startup was disabled by user/policy
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

        private void OnHotkeyKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            
            var key = e.Key;
            
            // Check for Enter to apply the current hotkey
            if (key == Windows.System.VirtualKey.Enter && _newHotkeyKey != Windows.System.VirtualKey.None)
            {
                ApplyHotkeyChange();
                return;
            }
            
            var modifiers = Windows.System.VirtualKeyModifiers.None;
            
            // Use InputKeyboardSource to get key states
            try
            {
                var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                if ((ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                    modifiers |= Windows.System.VirtualKeyModifiers.Control;
                    
                var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
                if ((shiftState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                    modifiers |= Windows.System.VirtualKeyModifiers.Shift;
                    
                var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);
                if ((altState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down)
                    modifiers |= Windows.System.VirtualKeyModifiers.Menu;
            }
            catch
            {
                // If we can't get key states, just use what we have
            }
            
            // Ignore modifier-only keys
            if (key == Windows.System.VirtualKey.Control || 
                key == Windows.System.VirtualKey.Shift || 
                key == Windows.System.VirtualKey.Menu ||
                key == Windows.System.VirtualKey.LeftControl ||
                key == Windows.System.VirtualKey.RightControl ||
                key == Windows.System.VirtualKey.LeftShift ||
                key == Windows.System.VirtualKey.RightShift ||
                key == Windows.System.VirtualKey.LeftMenu ||
                key == Windows.System.VirtualKey.RightMenu)
            {
                return;
            }
            
            // Require at least one modifier
            if (modifiers == Windows.System.VirtualKeyModifiers.None)
            {
                HotkeyStatusText.Text = "Please use at least one modifier key (Ctrl, Shift, Alt)";
                return;
            }
            
            _newHotkeyModifiers = modifiers;
            _newHotkeyKey = key;
            
            UpdateHotkeyDisplay();
            HotkeyStatusText.Text = "Press Enter to apply the new hotkey";
        }

        private void UpdateHotkeyDisplay()
        {
            var parts = new System.Collections.Generic.List<string>();
            
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
                parts.Add("Ctrl");
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
                parts.Add("Shift");
            if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Menu))
                parts.Add("Alt");
            
            if (_newHotkeyKey != Windows.System.VirtualKey.None)
                parts.Add(_newHotkeyKey.ToString());
            
            HotkeyTextBox.Text = parts.Any() ? string.Join(" + ", parts) : "Ctrl + Space (default)";
        }

        private void OnResetHotkey(object sender, RoutedEventArgs e)
        {
            _newHotkeyModifiers = Windows.System.VirtualKeyModifiers.Control;
            _newHotkeyKey = Windows.System.VirtualKey.Space;
            UpdateHotkeyDisplay();
            ApplyHotkeyChange();
            HotkeyStatusText.Text = "Reset to default (Ctrl + Space) and applied";
        }

        private void ApplyHotkeyChange()
        {
            try
            {
                // Convert WinUI modifiers to Win32 modifiers
                uint modifiers = 0;
                if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Control))
                    modifiers |= 0x0002; // MOD_CONTROL
                if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Shift))
                    modifiers |= 0x0004; // MOD_SHIFT
                if (_newHotkeyModifiers.HasFlag(Windows.System.VirtualKeyModifiers.Menu))
                    modifiers |= 0x0001; // MOD_ALT

                uint vKey = (uint)_newHotkeyKey;

                // Re-register the hotkey
                _hotkeyService.RegisterHotkey(_windowHandle, modifiers, vKey);
                HotkeyStatusText.Text = "Hotkey updated successfully!";
            }
            catch (Exception ex)
            {
                HotkeyStatusText.Text = $"Failed to update hotkey: {ex.Message}";
            }
        }

        private void OnCloseSettings(object sender, RoutedEventArgs e)
        {
            HideSettingsPanel();
        }

        private void ConfigureWindowStyle()
        {
            // Configure title bar for transparency
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

            // Configure presenter for non-resizable window
            var presenter = AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }
        }
    }
}
