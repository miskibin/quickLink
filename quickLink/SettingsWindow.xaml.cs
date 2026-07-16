using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using quickLink.Models;
using quickLink.Services;
using WinRT.Interop;

namespace quickLink
{
    public sealed partial class SettingsWindow : Window
    {
        #region Constants
        private const int WINDOW_WIDTH = 900;
        private const int WINDOW_HEIGHT = 640;

        // Win32 modifier flags
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        // Default hotkey (Ctrl + Shift + A) - matches AppSettings defaults.
        private static readonly Windows.System.VirtualKeyModifiers DefaultHotkeyModifiers =
            Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift;
        private static readonly Windows.System.VirtualKey DefaultHotkeyKey =
            Windows.System.VirtualKey.A;
        #endregion

        #region Fields
        private static SettingsWindow? _instance;

        private readonly SettingsService _settingsService;
        private IntPtr _windowHandle;
        private bool _loading;

        private Windows.System.VirtualKeyModifiers _newHotkeyModifiers = DefaultHotkeyModifiers;
        private Windows.System.VirtualKey _newHotkeyKey = DefaultHotkeyKey;
        #endregion

        #region P/Invoke
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);
        #endregion

        #region Construction & single-instance
        public static void ShowOrActivate(SettingsService settingsService)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }

            _instance = new SettingsWindow(settingsService);
            _instance.Activate();
        }

        public SettingsWindow(SettingsService settingsService)
        {
            _settingsService = settingsService;

            InitializeComponent();

            Title = "QuickLink Settings";
            _windowHandle = WindowNative.GetWindowHandle(this);

            InitializeWindow();
            ApplyBackdrop();
            LoadSettingsIntoUi();

            Closed += (_, _) => _instance = null;
        }

        private void InitializeWindow()
        {
            var dpiScale = GetDpiScaleForWindow();
            var scaledWidth = (int)(WINDOW_WIDTH * dpiScale);
            var scaledHeight = (int)(WINDOW_HEIGHT * dpiScale);
            AppWindow.Resize(new Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));

            // Center on the display that contains the window.
            var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                var centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - scaledWidth) / 2;
                var centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - scaledHeight) / 2;
                AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
            }
        }

        private double GetDpiScaleForWindow()
        {
            try
            {
                var dpi = GetDpiForWindow(_windowHandle);
                return dpi / 96.0;
            }
            catch
            {
                return 1.0;
            }
        }

        private void ApplyBackdrop()
        {
            // Prefer Mica; fall back to Desktop Acrylic on systems where Mica isn't supported.
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
        #endregion

        #region Navigation
        private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // SelectionChanged can fire while the initial selection is applied during
            // InitializeComponent, before the content panels have been wired up.
            if (GeneralPanel == null || AiPanel == null || HotkeyPanel == null) return;

            var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;

            GeneralPanel.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            AiPanel.Visibility = tag == "AI" ? Visibility.Visible : Visibility.Collapsed;
            HotkeyPanel.Visibility = tag == "Hotkey" ? Visibility.Visible : Visibility.Collapsed;
        }
        #endregion

        #region Load
        private async void LoadSettingsIntoUi()
        {
            _loading = true;

            var settings = _settingsService.Current;

            // General
            SearchUrlTextBox.Text = settings.SearchUrl ?? string.Empty;
            HideFooterCheckBox.IsChecked = settings.HideFooter;

            // AI provider
            SetProviderDropdown(settings.AiProvider);
            LoadProviderIntoFields(settings.AiProvider);

            // Hotkey
            (_newHotkeyModifiers, _newHotkeyKey) =
                ConvertFromWin32Modifiers(settings.HotkeyModifiers, settings.HotkeyKey);
            UpdateHotkeyDisplay();

            _loading = false;

            await LoadStartupSettingAsync();
        }
        #endregion

        #region General handlers
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
                // StartupTask API only works in packaged (MSIX) apps.
                // For unpackaged mode, fall back to registry-based startup.
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                    StartWithSystemCheckBox.IsChecked = key?.GetValue("QuickLink") != null;
                }
                catch
                {
                    StartWithSystemCheckBox.IsChecked = false;
                    StartWithSystemCheckBox.IsEnabled = false;
                }
            }
        }

        private async void OnStartWithSystemChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            var enable = StartWithSystemCheckBox.IsChecked == true;

            // Try MSIX StartupTask API first (works for packaged apps)
            try
            {
                var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("QuickLinkStartup");

                if (enable)
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
                return;
            }
            catch
            {
                // Fall back to registry-based startup for unpackaged mode.
            }

            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("QuickLink", $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue("QuickLink", false);
                }
            }
            catch
            {
                StartWithSystemCheckBox.IsChecked = false;
            }
        }

        private async void OnHideFooterChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            _settingsService.Current.HideFooter = HideFooterCheckBox.IsChecked ?? false;
            await _settingsService.SaveAsync();
        }

        private async void OnSearchUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;

            var newUrl = SearchUrlTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newUrl) || !newUrl.Contains("{query}"))
                return;

            _settingsService.Current.SearchUrl = newUrl;
            await _settingsService.SaveAsync();
        }
        #endregion

        #region AI provider handlers
        private void SetProviderDropdown(AiProvider provider)
        {
            var tag = provider.ToString();
            for (int i = 0; i < AiProviderComboBox.Items.Count; i++)
            {
                if (AiProviderComboBox.Items[i] is ComboBoxItem item && item.Tag as string == tag)
                {
                    AiProviderComboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void LoadProviderIntoFields(AiProvider provider)
        {
            var config = _settingsService.Current.GetActiveProviderConfig();

            ApiKeyBox.Password = config.ApiKey ?? string.Empty;
            AiModelTextBox.Text = config.Model ?? string.Empty;
            BaseUrlTextBox.Text = config.BaseUrl ?? string.Empty;

            var defaultModel = GrokService.GetDefaultModel(provider);
            AiModelTextBox.PlaceholderText = string.IsNullOrEmpty(defaultModel)
                ? "Enter a model name..."
                : defaultModel;

            BaseUrlPanel.Visibility = provider == AiProvider.Custom
                ? Visibility.Visible
                : Visibility.Collapsed;

            ApiKeyLabel.Text = provider switch
            {
                AiProvider.XAI => "xAI API Key",
                AiProvider.OpenAI => "OpenAI API Key",
                AiProvider.Claude => "Anthropic API Key",
                AiProvider.Custom => "API Key (optional)",
                _ => "API Key"
            };
        }

        private async void OnAiProviderChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (AiProviderComboBox.SelectedItem is not ComboBoxItem selected) return;
            if (selected.Tag is not string tag || !Enum.TryParse<AiProvider>(tag, out var provider)) return;

            _settingsService.Current.AiProvider = provider;

            // Load the newly selected provider's saved config into the fields
            // without re-triggering the field change handlers.
            _loading = true;
            LoadProviderIntoFields(provider);
            _loading = false;

            await _settingsService.SaveAsync();
        }

        private async void OnApiKeyChanged(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            _settingsService.Current.GetActiveProviderConfig().ApiKey = ApiKeyBox.Password ?? string.Empty;
            await _settingsService.SaveAsync();
        }

        private async void OnAiModelChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;

            _settingsService.Current.GetActiveProviderConfig().Model = AiModelTextBox.Text?.Trim() ?? string.Empty;
            await _settingsService.SaveAsync();
        }

        private async void OnBaseUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (_loading) return;

            _settingsService.Current.GetActiveProviderConfig().BaseUrl = BaseUrlTextBox.Text?.Trim() ?? string.Empty;
            await _settingsService.SaveAsync();
        }

        private void OnToggleRevealApiKey(object sender, RoutedEventArgs e)
        {
            var reveal = ApiKeyBox.PasswordRevealMode == PasswordRevealMode.Hidden;
            ApiKeyBox.PasswordRevealMode = reveal ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
            RevealApiKeyIcon.Glyph = reveal ? "\uE7B3" : "\uE890"; // RedEye / View
        }
        #endregion

        #region Hotkey capture
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
            // Reset the pending hotkey values when losing focus without applying,
            // so tabbing away doesn't keep unapplied changes.
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
                : "Press keys...";
        }

        private void OnResetHotkey(object sender, RoutedEventArgs e)
        {
            _newHotkeyModifiers = DefaultHotkeyModifiers;
            _newHotkeyKey = DefaultHotkeyKey;
            UpdateHotkeyDisplay();
            ApplyHotkeyChange();
            HotkeyStatusText.Text = "Reset to default (Ctrl + Shift + A) and applied";
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

                // MainWindow owns the Win32 registration; report success/failure back to the user.
                var ok = _settingsService.TryApplyHotkey(modifiers, vKey);
                await _settingsService.SaveAsync();

                HotkeyStatusText.Text = ok
                    ? "Hotkey updated and saved!"
                    : "Saved, but the combination could not be registered (it may be in use).";
            }
            catch (Exception ex)
            {
                HotkeyStatusText.Text = $"Failed to update hotkey: {ex.Message}";
            }
        }

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
    }
}
