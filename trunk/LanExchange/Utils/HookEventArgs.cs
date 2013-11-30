using System;

namespace LanExchange.Utils
{
    public class HookEventArgs : EventArgs
    {
        public int HookCode;	// Hook code
        public IntPtr wParam;	// WPARAM argument
        public IntPtr lParam;	// LPARAM argument
    }
}