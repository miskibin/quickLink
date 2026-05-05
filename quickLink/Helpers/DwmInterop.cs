using System;
using System.Runtime.InteropServices;

namespace quickLink.Helpers
{
    internal static class DwmInterop
    {
        private const int DWMWA_CLOAK = 13;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static int Cloak(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                throw new ArgumentException("Window handle is null.", nameof(hwnd));

            int value = 1;
            return DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
        }

        public static int Uncloak(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                throw new ArgumentException("Window handle is null.", nameof(hwnd));

            int value = 0;
            return DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
        }
    }
}
