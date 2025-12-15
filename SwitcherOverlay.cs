using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RcmdWindows
{
    public class SwitcherOverlay : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private List<AppLetterGroup> appGroups = new List<AppLetterGroup>();
        private char? highlightedLetter = null;

        private class AppLetterGroup
        {
            public char Letter { get; set; }
            public List<WindowInfo> Windows { get; set; } = new List<WindowInfo>();
            public Icon? AppIcon { get; set; }
        }

        public SwitcherOverlay()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.DoubleBuffered = true;
            this.Opacity = 0.95;
            this.AutoScaleMode = AutoScaleMode.Dpi;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Make the window click-through and non-activating
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE,
                exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

            // Round corners on Windows 11
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch { }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        public void UpdateApps(List<WindowInfo> windows)
        {
            appGroups.Clear();

            var settings = AppSettings.Instance;

            // Group windows by their assigned letter (custom or default)
            // Also filter out windows whose letter is disabled in captured keys
            var grouped = windows
                .Where(w => !string.IsNullOrEmpty(w.ProcessName) && !settings.IsExcluded(w.ProcessName))
                .GroupBy(w => char.ToUpper(settings.GetLetterForProcess(w.ProcessName)))
                .Where(g => settings.IsKeyCaptured(g.Key)) // Only show apps with captured keys
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in grouped)
            {
                var appGroup = new AppLetterGroup
                {
                    Letter = group.Key,
                    Windows = group.ToList()
                };

                // Try to get icon from the first window's process
                try
                {
                    var firstWindow = group.First();
                    var process = System.Diagnostics.Process.GetProcessById(
                        GetProcessIdFromWindow(firstWindow.Handle));
                    if (process.MainModule != null)
                    {
                        appGroup.AppIcon = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
                    }
                }
                catch { }

                appGroups.Add(appGroup);
            }

            CalculateSizeAndPosition();
            this.Invalidate();
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private int GetProcessIdFromWindow(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out uint processId);
            return (int)processId;
        }

        private void CalculateSizeAndPosition()
        {
            int itemSize = 48;
            int padding = 8;
            int itemsPerRow = Math.Min(appGroups.Count, 13);
            int rows = (int)Math.Ceiling(appGroups.Count / 13.0);

            if (appGroups.Count == 0)
            {
                this.Size = new Size(200, 60);
            }
            else
            {
                int width = (itemSize + padding) * itemsPerRow + padding;
                int height = (itemSize + padding) * rows + padding;
                this.Size = new Size(width, height);
            }

            // Position at bottom center, above taskbar
            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            int bottomMargin = 10; // Gap above taskbar
            this.Location = new Point(
                screen.X + (screen.Width - this.Width) / 2,
                screen.Y + screen.Height - this.Height - bottomMargin
            );
        }

        public void SetHighlightedLetter(char? letter)
        {
            highlightedLetter = letter.HasValue ? char.ToUpper(letter.Value) : null;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw background with rounded corners
            using (var path = CreateRoundedRectangle(ClientRectangle, 12))
            using (var brush = new SolidBrush(Color.FromArgb(40, 40, 40)))
            {
                g.FillPath(brush, path);
            }

            if (appGroups.Count == 0)
            {
                using (var font = new Font("Segoe UI", 10f))
                using (var brush = new SolidBrush(Color.FromArgb(150, 150, 150)))
                {
                    var text = "No apps available";
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush,
                        (Width - size.Width) / 2,
                        (Height - size.Height) / 2);
                }
                return;
            }

            int itemSize = 48;
            int padding = 8;
            int x = padding;
            int y = padding;
            int itemsPerRow = 13;
            int itemIndex = 0;

            foreach (var group in appGroups)
            {
                bool isHighlighted = highlightedLetter.HasValue &&
                                     group.Letter == highlightedLetter.Value;
                bool isAvailable = true;

                DrawAppItem(g, x, y, itemSize, group, isHighlighted, isAvailable);

                itemIndex++;
                if (itemIndex % itemsPerRow == 0)
                {
                    x = padding;
                    y += itemSize + padding;
                }
                else
                {
                    x += itemSize + padding;
                }
            }
        }

        private void DrawAppItem(Graphics g, int x, int y, int size,
            AppLetterGroup group, bool isHighlighted, bool isAvailable)
        {
            var rect = new Rectangle(x, y, size, size);

            // Background
            Color bgColor = isHighlighted
                ? Color.FromArgb(70, 130, 180)  // Steel blue when highlighted
                : Color.FromArgb(60, 60, 60);

            using (var path = CreateRoundedRectangle(rect, 8))
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            // Draw icon or letter
            int iconSize = 24;
            int iconX = x + (size - iconSize) / 2;
            int iconY = y + 4;

            if (group.AppIcon != null)
            {
                try
                {
                    g.DrawIcon(group.AppIcon, new Rectangle(iconX, iconY, iconSize, iconSize));
                }
                catch
                {
                    DrawLetterFallback(g, iconX, iconY, iconSize, group.Letter);
                }
            }
            else
            {
                DrawLetterFallback(g, iconX, iconY, iconSize, group.Letter);
            }

            // Draw letter indicator at bottom
            using (var font = new Font("Segoe UI Semibold", 9f))
            using (var brush = new SolidBrush(isHighlighted ? Color.White : Color.FromArgb(200, 200, 200)))
            {
                var letterStr = group.Letter.ToString();
                var letterSize = g.MeasureString(letterStr, font);
                g.DrawString(letterStr, font, brush,
                    x + (size - letterSize.Width) / 2,
                    y + size - letterSize.Height - 2);
            }

            // Draw count badge if multiple windows
            if (group.Windows.Count > 1)
            {
                var badgeText = group.Windows.Count.ToString();
                using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
                using (var bgBrush = new SolidBrush(Color.FromArgb(100, 149, 237)))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var badgeSize = g.MeasureString(badgeText, font);
                    int badgeW = Math.Max(14, (int)badgeSize.Width + 4);
                    int badgeH = 14;
                    int badgeX = x + size - badgeW - 2;
                    int badgeY = y + 2;

                    var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                    using (var path = CreateRoundedRectangle(badgeRect, 7))
                    {
                        g.FillPath(bgBrush, path);
                    }
                    g.DrawString(badgeText, font, textBrush,
                        badgeX + (badgeW - badgeSize.Width) / 2,
                        badgeY + (badgeH - badgeSize.Height) / 2);
                }
            }
        }

        private void DrawLetterFallback(Graphics g, int x, int y, int size, char letter)
        {
            using (var font = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
            {
                var letterStr = letter.ToString();
                var letterSize = g.MeasureString(letterStr, font);
                g.DrawString(letterStr, font, brush,
                    x + (size - letterSize.Width) / 2,
                    y + (size - letterSize.Height) / 2);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        public new void Show()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Show()));
                return;
            }

            base.Show();
            this.Invalidate();
        }

        public new void Hide()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Hide()));
                return;
            }

            base.Hide();
        }
    }
}
