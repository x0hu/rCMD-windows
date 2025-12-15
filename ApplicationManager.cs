using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RcmdWindows
{
    public struct WindowInfo
    {
        public IntPtr Handle;
        public string Title;
        public string ProcessName;
    }

    public class ApplicationManager
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private List<WindowInfo> cachedWindows = new List<WindowInfo>();
        private DateTime lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan cacheTimeout = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Finds the first window matching a given letter. For cycling support, use GetWindowsByLetter() instead.
        /// </summary>
        public WindowInfo? FindWindowByLetter(char letter)
        {
            var windows = GetWindowsByLetter(letter);
            return windows.Count > 0 ? windows[0] : null;
        }

        private void RefreshWindowCache()
        {
            if (DateTime.Now - lastCacheUpdate < cacheTimeout)
                return;

            cachedWindows.Clear();
            EnumWindows(EnumWindowCallback, IntPtr.Zero);
            lastCacheUpdate = DateTime.Now;
        }

        private bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd))
                return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return true;

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            string title = builder.ToString();

            if (string.IsNullOrWhiteSpace(title))
                return true;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                var process = Process.GetProcessById((int)processId);
                var processName = process.ProcessName;

                cachedWindows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = processName
                });
            }
            catch
            {
                // Process may have exited, ignore
            }

            return true;
        }

        public List<WindowInfo> GetAllWindows()
        {
            RefreshWindowCache();
            return new List<WindowInfo>(cachedWindows);
        }

        /// <summary>
        /// Gets all windows that match a given letter (based on custom or default mapping).
        /// </summary>
        public List<WindowInfo> GetWindowsByLetter(char letter)
        {
            RefreshWindowCache();
            var settings = AppSettings.Instance;

            return cachedWindows
                .Where(w => !string.IsNullOrEmpty(w.ProcessName) && !settings.IsExcluded(w.ProcessName))
                .Where(w => char.ToLower(settings.GetLetterForProcess(w.ProcessName)) == char.ToLower(letter))
                .ToList();
        }
    }
}
