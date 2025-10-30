using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace quickLink.Services
{
    public class GlobalHotkeyService : IDisposable
    {
  // Win32 API imports
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

      [DllImport("user32.dll")]
   private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
     private static extern IntPtr GetForegroundWindow();

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_SPACE = 0x20;

     private IntPtr _windowHandle;
        private bool _isRegistered;
        private uint _currentModifiers = MOD_CONTROL;
        private uint _currentKey = VK_SPACE;

        public event EventHandler? HotkeyPressed;

   public bool RegisterHotkey(IntPtr windowHandle)
     {
   _windowHandle = windowHandle;
 _isRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, _currentModifiers, _currentKey);
   return _isRegistered;
  }

        public bool RegisterHotkey(IntPtr windowHandle, uint modifiers, uint key)
        {
            _windowHandle = windowHandle;
            _currentModifiers = modifiers;
            _currentKey = key;
            
            // Unregister old hotkey first
            if (_isRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
            }
            
            _isRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, key);
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

      public void Dispose()
        {
      UnregisterHotkey();
    }
    }
}
