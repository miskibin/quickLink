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

        public MainWindow()
        {
            _dataService = new DataService();
            _clipboardService = new ClipboardService();
            _allItems = new ObservableCollection<ClipboardItem>();
            _filteredItems = new ObservableCollection<ClipboardItem>();

            InitializeComponent();

            ItemsList.ItemsSource = _filteredItems;

            _windowHandle = WindowNative.GetWindowHandle(this);

            // Remove title bar completely
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(null);

            // Compact modern window size
            AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 300));
            
            // Remove default window borders for clean look
            var presenter = AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }

            CenterWindow();

            _hotkeyService = new GlobalHotkeyService();
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _hotkeyService.RegisterHotkey(_windowHandle);

            // Subscribe to window messages
            SubclassWindow();
            _ = LoadDataAsync();

            Activated += (s, e) => SearchBox.Focus(FocusState.Programmatic);

            // Hide window initially (it will be shown via hotkey)
            AppWindow.Hide();
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
            if (_isEditing)
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
            if (AppWindow.IsVisible)
            {
                AppWindow.Hide();
            }
            else
            {
                AppWindow.Show();
                this.Activate();
                SearchBox.Focus(FocusState.Programmatic);
                SearchBox.SelectAll();
            }
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
            args.Handled = true;
        }

        private void OnAddNewTapped(object sender, TappedRoutedEventArgs e)
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

            ItemsList.Visibility = Visibility.Collapsed;
            EditPanel.Visibility = Visibility.Visible;
            EditTitle.Focus(FocusState.Programmatic);
        }

        private void HideEditPanel()
        {
            _isEditing = false;
            _editingItem = null;
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
    }
}
