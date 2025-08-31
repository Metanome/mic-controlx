using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MicControlX
{
    /// <summary>
    /// Dedicated hotkey manager using a message-only window for reliable global hotkey handling
    /// This ensures hotkeys work even when the main window is hidden or minimized
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        #region Windows API
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const int HWND_MESSAGE = -3; // Message-only window
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent,
            IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        
        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
        
        #region Fields
        private IntPtr messageWindow;
        private HwndSource? hwndSource;
        private int currentHotkey;
        private bool isDisposed;
        #endregion
        
        #region Events
        public event Action? HotkeyPressed;
        #endregion
        
        #region Constructor & Destructor
        public HotkeyManager()
        {
            CreateMessageWindow();
        }
        
        ~HotkeyManager()
        {
            Dispose(false);
        }
        #endregion
        
        #region Public Methods
        public bool RegisterHotkey(int virtualKey)
        {
            if (messageWindow == IntPtr.Zero)
                return false;
                
            // Unregister existing hotkey
            UnregisterCurrentHotkey();
            
            // Register new hotkey
            bool success = RegisterHotKey(messageWindow, HOTKEY_ID, 0, virtualKey);
            if (success)
            {
                currentHotkey = virtualKey;
            }
            
            return success;
        }
        
        public void UnregisterCurrentHotkey()
        {
            if (messageWindow != IntPtr.Zero && currentHotkey != 0)
            {
                UnregisterHotKey(messageWindow, HOTKEY_ID);
                currentHotkey = 0;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        
        #region Private Methods
        private void CreateMessageWindow()
        {
            try
            {
                // Create a message-only window using HwndSource
                var parameters = new HwndSourceParameters("MicControlX_HotkeyWindow")
                {
                    WindowStyle = 0,
                    ExtendedWindowStyle = 0,
                    Width = 0,
                    Height = 0,
                    ParentWindow = new IntPtr(HWND_MESSAGE) // Message-only window
                };
                
                hwndSource = new HwndSource(parameters);
                hwndSource.AddHook(WindowProc);
                messageWindow = hwndSource.Handle;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create message window: {ex.Message}");
            }
        }
        
        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            
            return IntPtr.Zero;
        }
        
        private void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    UnregisterCurrentHotkey();
                    hwndSource?.RemoveHook(WindowProc);
                    hwndSource?.Dispose();
                }
                
                if (messageWindow != IntPtr.Zero)
                {
                    DestroyWindow(messageWindow);
                    messageWindow = IntPtr.Zero;
                }
                
                isDisposed = true;
            }
        }
        #endregion
    }
}
