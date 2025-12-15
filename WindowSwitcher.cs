using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RcmdWindows
{
    public class WindowSwitcher
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;

        public void SwitchToWindow(WindowInfo window)
        {
            try
            {
                var settings = AppSettings.Instance;

                // Check if we should bring all windows of this app to front
                if (settings.AppWindowFocus == WindowFocusBehavior.AllWindows)
                {
                    BringAllWindowsOfProcessToFront(window);
                }
                else
                {
                    // Just bring the specified window
                    BringWindowToFront(window.Handle);
                }

                Console.WriteLine($"Switched to: {window.ProcessName} - {window.Title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error switching to window: {ex.Message}");
            }
        }

        private void BringWindowToFront(IntPtr handle)
        {
            // If window is minimized, restore it first
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            // Get the foreground window's thread
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            uint currentThreadId = GetCurrentThreadId();

            // Attach to the foreground thread to allow SetForegroundWindow
            bool attached = false;
            if (foregroundThreadId != currentThreadId)
            {
                attached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            // Bring window to foreground using multiple methods for reliability
            BringWindowToTop(handle);
            ShowWindow(handle, SW_SHOW);
            SetForegroundWindow(handle);

            // Detach from foreground thread
            if (attached)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }

        private void BringAllWindowsOfProcessToFront(WindowInfo mainWindow)
        {
            GetWindowThreadProcessId(mainWindow.Handle, out uint targetProcessId);
            var allWindows = GetAllVisibleWindows();

            // First restore all windows of this process (except main window)
            foreach (var handle in allWindows)
            {
                GetWindowThreadProcessId(handle, out uint windowProcessId);
                if (windowProcessId == targetProcessId && handle != mainWindow.Handle)
                {
                    if (IsIconic(handle))
                    {
                        ShowWindow(handle, SW_RESTORE);
                    }
                    BringWindowToTop(handle);
                    ShowWindow(handle, SW_SHOW);
                }
            }

            // Finally bring the main window to the very front
            BringWindowToFront(mainWindow.Handle);
        }

        public void MinimizeCurrentWindow()
        {
            try
            {
                var settings = AppSettings.Instance;
                IntPtr foregroundWindow = GetForegroundWindow();

                if (foregroundWindow == IntPtr.Zero)
                    return;

                // Get process ID of the current foreground window
                GetWindowThreadProcessId(foregroundWindow, out uint currentProcessId);

                if (settings.MinimizeKeyHides == MinimizeScope.AllApps)
                {
                    // Minimize all visible windows
                    MinimizeAllWindows();
                    Console.WriteLine("Minimized all windows");
                }
                else // FocusedApp
                {
                    if (settings.MinimizeKeyAffects == MinimizeAffects.AllWindows)
                    {
                        // Minimize all windows of the focused app
                        MinimizeAllWindowsOfProcess(currentProcessId);
                        Console.WriteLine("Minimized all windows of focused app");
                    }
                    else // FocusedWindow
                    {
                        // Minimize only the focused window (original behavior)
                        ShowWindow(foregroundWindow, SW_MINIMIZE);
                        Console.WriteLine("Minimized current window");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error minimizing window: {ex.Message}");
            }
        }

        /// <summary>
        /// Minimizes all visible windows.
        /// </summary>
        private void MinimizeAllWindows()
        {
            var windows = GetAllVisibleWindows();
            foreach (var handle in windows)
            {
                ShowWindow(handle, SW_MINIMIZE);
            }
        }

        /// <summary>
        /// Minimizes all windows belonging to a specific process.
        /// </summary>
        private void MinimizeAllWindowsOfProcess(uint processId)
        {
            var windows = GetAllVisibleWindows();
            foreach (var handle in windows)
            {
                GetWindowThreadProcessId(handle, out uint windowProcessId);
                if (windowProcessId == processId)
                {
                    ShowWindow(handle, SW_MINIMIZE);
                }
            }
        }

        /// <summary>
        /// Gets all visible windows with titles.
        /// </summary>
        private List<IntPtr> GetAllVisibleWindows()
        {
            var windows = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return true;

                windows.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// Gets the currently focused window as a WindowInfo.
        /// </summary>
        public WindowInfo? GetFocusedWindow()
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                    return null;

                int length = GetWindowTextLength(foregroundWindow);
                if (length == 0)
                    return null;

                var builder = new StringBuilder(length + 1);
                GetWindowText(foregroundWindow, builder, builder.Capacity);

                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                var process = System.Diagnostics.Process.GetProcessById((int)processId);

                return new WindowInfo
                {
                    Handle = foregroundWindow,
                    Title = builder.ToString(),
                    ProcessName = process.ProcessName
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Hides (minimizes) a specific window.
        /// </summary>
        public void HideWindow(WindowInfo window)
        {
            try
            {
                ShowWindow(window.Handle, SW_MINIMIZE);
                Console.WriteLine($"Hidden window: {window.ProcessName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding window: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides all windows except those belonging to the specified window's process.
        /// </summary>
        public void HideOtherApps(WindowInfo keepVisible)
        {
            try
            {
                GetWindowThreadProcessId(keepVisible.Handle, out uint keepProcessId);
                var windows = GetAllVisibleWindows();

                foreach (var handle in windows)
                {
                    GetWindowThreadProcessId(handle, out uint windowProcessId);
                    if (windowProcessId != keepProcessId)
                    {
                        ShowWindow(handle, SW_MINIMIZE);
                    }
                }
                Console.WriteLine($"Hidden all apps except {keepVisible.ProcessName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding other apps: {ex.Message}");
            }
        }
    }
}
