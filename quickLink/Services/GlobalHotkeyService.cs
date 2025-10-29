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
   private const uint VK_SPACE = 0x20;

     private IntPtr _windowHandle;
        private bool _isRegistered;

        public event EventHandler? HotkeyPressed;

   public bool RegisterHotkey(IntPtr windowHandle)
     {
   _windowHandle = windowHandle;
 _isRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_CONTROL, VK_SPACE);
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
