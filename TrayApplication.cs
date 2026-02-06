using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RcmdWindows
{
    public class TrayApplication : IDisposable
    {
        private readonly NotifyIcon trayIcon;
        private readonly KeyboardHook keyboardHook;
        private readonly ApplicationManager appManager;
        private readonly WindowSwitcher windowSwitcher;
        private readonly SwitcherOverlay switcherOverlay;

        // Track cycling state per letter
        private readonly Dictionary<char, int> lastWindowIndex = new Dictionary<char, int>();
        private char? lastSwitchedLetter = null;

        public TrayApplication()
        {
            trayIcon = CreateTrayIcon();
            appManager = new ApplicationManager();
            windowSwitcher = new WindowSwitcher();
            keyboardHook = new KeyboardHook();
            switcherOverlay = new SwitcherOverlay();

            keyboardHook.OnKeyPressed += HandleKeyPress;
            keyboardHook.OnModifierPressed += HandleModifierPressed;
            keyboardHook.OnModifierReleased += HandleModifierReleased;
            keyboardHook.OnMinimizePressed += HandleMinimizePressed;
            keyboardHook.OnSettingsPressed += HandleSettingsPressed;
            keyboardHook.Install();

            Console.WriteLine("rcmd for Windows started. Press modifier + letter to switch apps.");
        }

        private NotifyIcon CreateTrayIcon()
        {
            var icon = new NotifyIcon
            {
                Text = "rcmd for Windows",
                Visible = !AppSettings.Instance.HideTrayIcon
            };

            // Create a simple icon (you can replace this with a proper icon file later)
            using (var bitmap = new Bitmap(16, 16))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Blue);
                icon.Icon = Icon.FromHandle(bitmap.GetHicon());
            }

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Settings", null, OnSettings);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, OnExit);
            icon.ContextMenuStrip = contextMenu;

            return icon;
        }

        private void HandleModifierPressed()
        {
            var windows = appManager.GetAllWindows();
            switcherOverlay.UpdateApps(windows);
            switcherOverlay.Show();
        }

        private void HandleModifierReleased()
        {
            switcherOverlay.Hide();
            switcherOverlay.SetHighlightedLetter(null);
        }

        private void HandleMinimizePressed()
        {
            windowSwitcher.MinimizeCurrentWindow();
        }

        private void HandleSettingsPressed()
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            using (var settingsForm = new SettingsForm(appManager))
            {
                settingsForm.ShowDialog();
            }
            RefreshTrayIconVisibility();
        }

        private void RefreshTrayIconVisibility()
        {
            trayIcon.Visible = !AppSettings.Instance.HideTrayIcon;
        }

        private void HandleKeyPress(char letter, bool withLeftShift, bool withRightShift, bool withAlt)
        {
            var settings = AppSettings.Instance;
            Console.WriteLine($"Triggered: Modifier + {letter} (LShift={withLeftShift}, RShift={withRightShift}, Alt={withAlt})");

            // Highlight the letter in the overlay
            switcherOverlay.SetHighlightedLetter(letter);

            // Handle Assign Letter hotkey (Alt modifier)
            if (withAlt && settings.EnableAssignLetterHotkey)
            {
                ShowAssignLetterDialog(letter);
                return;
            }

            // Determine if we should force cycle (Right Shift modifier)
            bool forceCycle = withRightShift && settings.EnableForceCycleHotkey;

            // Find the window to switch to
            var windows = appManager.GetWindowsByLetter(letter);
            if (windows.Count == 0)
            {
                Console.WriteLine($"No window found for '{letter}'");
                return;
            }

            // Determine which window to switch to based on settings and modifiers
            WindowInfo? targetWindow = null;
            bool shouldCycle = forceCycle;

            // Check if already focused on an app with this letter
            var focusedWindow = windowSwitcher.GetFocusedWindow();
            if (focusedWindow.HasValue)
            {
                char focusedLetter = char.ToLower(settings.GetLetterForProcess(focusedWindow.Value.ProcessName));
                if (focusedLetter == char.ToLower(letter))
                {
                    // Already focused on this letter's app - apply WhenAlreadyFocused behavior
                    switch (settings.WhenAlreadyFocusedBehavior)
                    {
                        case WhenAlreadyFocused.HideApp:
                            windowSwitcher.HideWindow(focusedWindow.Value);
                            return;

                        case WhenAlreadyFocused.CycleApps:
                            // Do nothing - stay on the current window
                            return;

                        case WhenAlreadyFocused.CycleWindows:
                            shouldCycle = true;
                            break;
                    }
                }
            }

            // Get the target window (with cycling if needed)
            if (shouldCycle && windows.Count > 1)
            {
                // Cycle to next window
                if (!lastWindowIndex.ContainsKey(letter) || lastSwitchedLetter != letter)
                {
                    lastWindowIndex[letter] = 0;
                }
                else
                {
                    lastWindowIndex[letter] = (lastWindowIndex[letter] + 1) % windows.Count;
                }
                targetWindow = windows[lastWindowIndex[letter]];
            }
            else
            {
                targetWindow = windows[0];
                lastWindowIndex[letter] = 0;
            }

            lastSwitchedLetter = letter;

            if (targetWindow.HasValue)
            {
                // Handle Hide Others on Focus (Left Shift modifier) or Single App Mode
                bool hideOthers = (withLeftShift && settings.EnableHideOthersOnFocus) || settings.SingleAppMode;

                if (hideOthers)
                {
                    windowSwitcher.HideOtherApps(targetWindow.Value);
                }

                windowSwitcher.SwitchToWindow(targetWindow.Value);
            }
        }

        private void ShowAssignLetterDialog(char currentLetter)
        {
            var focusedWindow = windowSwitcher.GetFocusedWindow();
            if (!focusedWindow.HasValue)
                return;

            string processName = focusedWindow.Value.ProcessName;
            using (var dialog = new LetterInputDialog(processName, AppSettings.Instance))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (dialog.SelectedLetter.HasValue)
                    {
                        AppSettings.Instance.SetLetterForProcess(processName, dialog.SelectedLetter.Value);
                        Console.WriteLine($"Assigned '{dialog.SelectedLetter.Value}' to {processName}");
                    }
                    else
                    {
                        AppSettings.Instance.RemoveMapping(processName);
                        Console.WriteLine($"Reset letter mapping for {processName}");
                    }
                }
            }
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            OpenSettings();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        public void Dispose()
        {
            keyboardHook?.Dispose();
            switcherOverlay?.Dispose();
            trayIcon?.Dispose();
        }
    }
}
