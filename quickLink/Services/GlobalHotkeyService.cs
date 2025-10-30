using System;
using System.Runtime.InteropServices;

namespace quickLink.Services
{
    public sealed class GlobalHotkeyService : IDisposable
    {
        #region Constants

        private const int HOTKEY_ID = 9000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_SPACE = 0x20;

        #endregion

        #region Fields

        private IntPtr _windowHandle;
        private bool _isRegistered;
        private uint _currentModifiers = MOD_CONTROL;
        private uint _currentKey = VK_SPACE;
        private bool _disposed;

        #endregion

        #region P/Invoke

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion

        #region Events

        public event EventHandler? HotkeyPressed;

        #endregion

        #region Public Methods

        public bool RegisterHotkey(IntPtr windowHandle)
        {
            return RegisterHotkey(windowHandle, _currentModifiers, _currentKey);
        }

        public bool RegisterHotkey(IntPtr windowHandle, uint modifiers, uint key)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentException("Window handle cannot be zero.", nameof(windowHandle));

            // Unregister existing hotkey if already registered
            if (_isRegistered)
            {
                UnregisterHotkey();
            }

            _windowHandle = windowHandle;
            _currentModifiers = modifiers;
            _currentKey = key;
            
            _isRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, key);
            
            if (!_isRegistered)
            {
                // Get last Win32 error for debugging
                var errorCode = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey. Error code: {errorCode}");
            }
            
            return _isRegistered;
        }

        public void UnregisterHotkey()
        {
            if (_isRegistered && _windowHandle != IntPtr.Zero)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        public void OnHotkeyMessage(int hotkeyId)
        {
            if (hotkeyId == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed)
                return;

            UnregisterHotkey();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
