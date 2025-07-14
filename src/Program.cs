using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MicControlX
{
    static class Program
    {
        private static Mutex? applicationMutex;
        private const string APP_MUTEX_NAME = "MicControlX_SingleInstance_Mutex";
        private const string APP_TITLE = "MicControlX";
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        [STAThread]
        static void Main(string[] args)
        {
            // Check if another instance is already running
            bool isNewInstance;
            applicationMutex = new Mutex(true, APP_MUTEX_NAME, out isNewInstance);

            if (!isNewInstance)
            {
                // Another instance is running, try to bring it to foreground
                BringExistingInstanceToFront();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Start minimized if --minimized flag is present
            bool startMinimized = args.Contains("--minimized");
            Application.Run(new MainWindow(startMinimized));
            
            // Release mutex when application exits
            applicationMutex?.ReleaseMutex();
            applicationMutex?.Dispose();
        }
        
        private static void BringExistingInstanceToFront()
        {
            try
            {
                // Try to find window by title first
                IntPtr hWnd = FindWindow(null, APP_TITLE);
                
                if (hWnd != IntPtr.Zero)
                {
                    // Window found, bring it to front
                    if (IsIconic(hWnd))
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }
                    else
                    {
                        ShowWindow(hWnd, SW_SHOW);
                    }
                    SetForegroundWindow(hWnd);
                }
                else
                {
                    // Fallback: try to find process and send message
                    Process currentProcess = Process.GetCurrentProcess();
                    Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
                    
                    foreach (Process process in processes)
                    {
                        if (process.Id != currentProcess.Id)
                        {
                            // Try to bring main window to front if it has one
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                if (IsIconic(process.MainWindowHandle))
                                {
                                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                                }
                                SetForegroundWindow(process.MainWindowHandle);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to bring existing instance to front: {ex.Message}");
            }
        }
    }
}