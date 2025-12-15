using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace RcmdWindows
{
    public class SettingsForm : Form
    {
        private ApplicationManager appManager;
        private Panel contentPanel;
        private int currentY = 20;

        // Colors matching macOS dark theme
        private static readonly Color BgColor = Color.FromArgb(30, 30, 30);
        private static readonly Color SectionBgColor = Color.FromArgb(45, 45, 45);
        private static readonly Color TextColor = Color.FromArgb(255, 255, 255);
        private static readonly Color SubTextColor = Color.FromArgb(140, 140, 140);
        private static readonly Color AccentColor = Color.FromArgb(100, 149, 237);
        private static readonly Color ToggleBgColor = Color.FromArgb(60, 60, 60);
        private static readonly Color ToggleSelectedColor = Color.FromArgb(80, 80, 80);
        private static readonly Color KeyBgColor = Color.FromArgb(70, 70, 70);
        private static readonly Color KeySelectedColor = Color.FromArgb(120, 100, 80);
        private static readonly Color KeyDisabledColor = Color.FromArgb(50, 50, 50);

        public SettingsForm(ApplicationManager appManager)
        {
            this.appManager = appManager;
            InitializeForm();
            BuildUI();
        }

        private void InitializeForm()
        {
            this.Text = "Settings";
            this.Size = new Size(600, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 9f);
            this.AutoScroll = true;

            // Ensure window appears on top when opened from system tray
            this.TopMost = true;

            contentPanel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                BackColor = BgColor
            };
            this.Controls.Add(contentPanel);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Disable TopMost after form is shown so it behaves normally
            this.TopMost = false;
            this.Activate();
        }

        private void BuildUI()
        {
            // Title
            var titleLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(titleLabel);
            currentY += 50;

            // General Settings Section
            BuildGeneralSettings();

            // Hotkey Settings Section
            BuildHotkeySettings();

            // Focus Behavior Section
            BuildFocusBehavior();

            // Key Configuration Section
            BuildKeyConfiguration();

            // App Management Section
            BuildAppManagement();

            // Captured Keys Section
            BuildCapturedKeys();

            // Set content panel height
            contentPanel.AutoScrollMinSize = new Size(0, currentY + 20);
        }

        private void BuildGeneralSettings()
        {
            var settings = AppSettings.Instance;

            // Launch at login
            AddCheckboxOption("Launch at login", settings.LaunchAtLogin, (checked_) =>
            {
                settings.SetLaunchAtLogin(checked_);
            });

            // Hide tray icon
            AddCheckboxOption("Hide tray icon", settings.HideTrayIcon, (checked_) =>
            {
                settings.HideTrayIcon = checked_;
                settings.Save();
            });

            // Exclude static apps
            AddCheckboxOption("Exclude Static apps when cycling from Dynamic apps", settings.ExcludeStaticApps, (checked_) =>
            {
                settings.ExcludeStaticApps = checked_;
                settings.Save();
            });

            currentY += 15;
            AddSeparator();
        }

        private void BuildHotkeySettings()
        {
            var settings = AppSettings.Instance;

            // Enable Assign Letter hotkey
            AddCheckboxOption("Enable Assign Letter hotkey", settings.EnableAssignLetterHotkey, (checked_) =>
            {
                settings.EnableAssignLetterHotkey = checked_;
                settings.Save();
            }, "Add Alt to assign a letter to the focused app");

            AddModifierKeyDisplay(new[] { "Shift", "Ctrl", "Alt", "_", "Alt", "Ctrl", "Shift" }, new[] { false, false, true, false, false, true, false });

            // Enable Force Cycle hotkey
            AddCheckboxOption("Enable Force Cycle hotkey", settings.EnableForceCycleHotkey, (checked_) =>
            {
                settings.EnableForceCycleHotkey = checked_;
                settings.Save();
            }, "Add Right Shift when focusing apps to cycle same letter apps");

            AddModifierKeyDisplay(new[] { "Shift", "Ctrl", "Alt", "_", "Alt", "Ctrl", "Shift" }, new[] { false, false, false, false, false, false, true });

            // Enable Hide Others on Focus
            AddCheckboxOption("Enable Hide Others on Focus hotkey", settings.EnableHideOthersOnFocus, (checked_) =>
            {
                settings.EnableHideOthersOnFocus = checked_;
                settings.Save();
            }, "Add Left Shift when focusing apps to hide unfocused apps");

            AddModifierKeyDisplay(new[] { "Shift", "Ctrl", "Alt", "_", "Alt", "Ctrl", "Shift" }, new[] { true, false, false, false, false, false, false });

            // Single App Mode (indented)
            AddCheckboxOption("Single App Mode", settings.SingleAppMode, (checked_) =>
            {
                settings.SingleAppMode = checked_;
                settings.Save();
            }, "Hide unfocused apps without having to add Left Shift", 40);

            currentY += 15;
            AddSeparator();
        }

        private void BuildFocusBehavior()
        {
            var settings = AppSettings.Instance;

            // App window focus behaviour
            AddSegmentedControl(
                "App window focus behaviour",
                "Which windows of the app to bring to front",
                new[] { "All windows", "Main window" },
                settings.AppWindowFocus == WindowFocusBehavior.AllWindows ? 0 : 1,
                (index) =>
                {
                    settings.AppWindowFocus = index == 0 ? WindowFocusBehavior.AllWindows : WindowFocusBehavior.MainWindow;
                    settings.Save();
                });

            // When already focused
            AddSegmentedControl(
                "When already focused",
                "",
                new[] { "Hide app", "Cycle apps", "Cycle windows" },
                (int)settings.WhenAlreadyFocusedBehavior,
                (index) =>
                {
                    settings.WhenAlreadyFocusedBehavior = (WhenAlreadyFocused)index;
                    settings.Save();
                });

            currentY += 15;
            AddSeparator();
        }

        private void BuildKeyConfiguration()
        {
            var settings = AppSettings.Instance;

            // Apps key with modifier selector
            AddModifierKeySelector("Apps key", settings.AppsModifierKey, (modifier) =>
            {
                settings.AppsModifierKey = modifier;
                settings.Save();
            });

            // Minimize key hides
            AddSegmentedControl(
                "Minimize key hides",
                "",
                new[] { "All apps", "Focused app" },
                settings.MinimizeKeyHides == MinimizeScope.AllApps ? 0 : 1,
                (index) =>
                {
                    settings.MinimizeKeyHides = index == 0 ? MinimizeScope.AllApps : MinimizeScope.FocusedApp;
                    settings.Save();
                }, 40);

            // Minimize key affects
            AddSegmentedControl(
                "Minimize key affects",
                "",
                new[] { "All windows", "Focused window" },
                settings.MinimizeKeyAffects == MinimizeAffects.AllWindows ? 0 : 1,
                (index) =>
                {
                    settings.MinimizeKeyAffects = index == 0 ? MinimizeAffects.AllWindows : MinimizeAffects.FocusedWindow;
                    settings.Save();
                }, 40);

            currentY += 10;

            // Hide/Minimize key
            AddKeySelector("Hide/Minimize key", "Used for hiding apps and minimizing windows", settings.HideMinimizeKey, (key) =>
            {
                settings.HideMinimizeKey = key;
                settings.Save();
            });

            // Menu/Actions key
            AddKeySelector("Menu/Actions key", "Used for showing this menu and assigning window actions", settings.MenuActionsKey, (key) =>
            {
                settings.MenuActionsKey = key;
                settings.Save();
            });

            currentY += 15;
            AddSeparator();
        }

        private void BuildAppManagement()
        {
            // Header
            var headerLabel = new Label
            {
                Text = "App Shortcuts",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(headerLabel);

            // Manage Apps button on the right
            var manageButton = new Button
            {
                Text = "Manage Apps...",
                Size = new Size(120, 30),
                Location = new Point(contentPanel.Width - 150, currentY - 3),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            manageButton.FlatAppearance.BorderSize = 0;
            manageButton.Click += (s, e) =>
            {
                using (var dialog = new AppManagementDialog(appManager))
                {
                    dialog.ShowDialog(this);
                }
            };
            contentPanel.Controls.Add(manageButton);

            currentY += 25;

            var subLabel = new Label
            {
                Text = "Assign custom letters to apps and exclude apps from switching",
                Font = new Font("Segoe UI", 9f),
                ForeColor = SubTextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(subLabel);

            currentY += 30;
            AddSeparator();
        }

        private void BuildCapturedKeys()
        {
            var settings = AppSettings.Instance;

            // Header
            var headerLabel = new Label
            {
                Text = "Captured keys",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(headerLabel);
            currentY += 25;

            var subLabel = new Label
            {
                Text = "Here you can disable keys to prevent them from being captured by rcmd",
                Font = new Font("Segoe UI", 9f),
                ForeColor = SubTextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(subLabel);
            currentY += 30;

            // Keyboard rows
            string[] row1 = { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
            string[] row2 = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]" };
            string[] row3 = { "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "\\" };
            string[] row4 = { "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/" };

            AddKeyboardRow(row1, 20);
            AddKeyboardRow(row2, 35);
            AddKeyboardRow(row3, 35);
            AddKeyboardRow(row4, 50);

            currentY += 20;
        }

        private void AddCheckboxOption(string text, bool isChecked, Action<bool> onChange, string? subText = null, int indent = 0)
        {
            var checkbox = new RoundCheckBox
            {
                Text = text,
                Checked = isChecked,
                Location = new Point(20 + indent, currentY),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10f, subText == null ? FontStyle.Regular : FontStyle.Bold)
            };
            // Set size after setting text and font
            checkbox.Size = checkbox.GetPreferredSize(Size.Empty);
            checkbox.CheckedChanged += (s, e) => onChange(checkbox.Checked);
            contentPanel.Controls.Add(checkbox);
            currentY += Math.Max(28, checkbox.Height + 4);

            if (subText != null)
            {
                var subLabel = new Label
                {
                    Text = subText,
                    ForeColor = SubTextColor,
                    Location = new Point(50 + indent, currentY - 8),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f)
                };
                contentPanel.Controls.Add(subLabel);
                currentY += 18;
            }
        }

        private void AddModifierKeyDisplay(string[] keys, bool[] selected)
        {
            int x = 45;
            int keyWidth = 42;
            int keyHeight = 28;
            int spacing = 3;

            var panel = new Panel
            {
                Location = new Point(x, currentY),
                Size = new Size(keys.Length * (keyWidth + spacing), keyHeight),
                BackColor = Color.Transparent
            };

            for (int i = 0; i < keys.Length; i++)
            {
                var keyLabel = new ModifierKeyLabel(keys[i], selected[i])
                {
                    Location = new Point(i * (keyWidth + spacing), 0),
                    Size = new Size(keyWidth, keyHeight)
                };
                panel.Controls.Add(keyLabel);
            }

            contentPanel.Controls.Add(panel);
            currentY += 40;
        }

        private void AddSegmentedControl(string label, string subText, string[] options, int selectedIndex, Action<int> onChange, int indent = 0)
        {
            var labelCtrl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20 + indent, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(labelCtrl);

            if (!string.IsNullOrEmpty(subText))
            {
                currentY += 18;
                var subLabel = new Label
                {
                    Text = subText,
                    ForeColor = SubTextColor,
                    Location = new Point(20 + indent, currentY),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8f)
                };
                contentPanel.Controls.Add(subLabel);
            }

            // Create segmented control on the right
            var segmented = new SegmentedControl(options, selectedIndex, onChange)
            {
                Location = new Point(contentPanel.Width - 280 - indent, currentY - (string.IsNullOrEmpty(subText) ? 0 : 10)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            contentPanel.Controls.Add(segmented);

            currentY += 35;
        }

        private void AddModifierKeySelector(string label, ModifierKey currentKey, Action<ModifierKey> onChange)
        {
            var labelCtrl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(labelCtrl);

            // Modifier key display
            string[] keys = { "Shift", "Ctrl", "Alt", "_", "Alt", "Ctrl", "Shift" };
            var keyLabels = new ModifierKeyLabel[7];

            // Map modifier key to index
            int GetIndexForModifierKey(ModifierKey key) => key switch
            {
                ModifierKey.Shift => 0,
                ModifierKey.Ctrl => 1,
                ModifierKey.Alt => 2,
                ModifierKey.RightAlt => 4,
                ModifierKey.Win => 3,
                _ => -1
            };

            int currentIndex = GetIndexForModifierKey(currentKey);

            int x = 180;
            int keyWidth = 42;
            int keyHeight = 28;
            int spacing = 3;

            for (int i = 0; i < keys.Length; i++)
            {
                int index = i;
                var keyLabel = new ModifierKeyLabel(keys[i], i == currentIndex, true)
                {
                    Location = new Point(x + i * (keyWidth + spacing), currentY - 3),
                    Size = new Size(keyWidth, keyHeight)
                };
                keyLabels[i] = keyLabel;

                keyLabel.Click += (s, e) =>
                {
                    // Skip spacer
                    if (keys[index] == "_") return;

                    ModifierKey newKey = index switch
                    {
                        0 => ModifierKey.Shift,
                        1 => ModifierKey.Ctrl,
                        2 => ModifierKey.Alt,
                        4 => ModifierKey.RightAlt,
                        5 => ModifierKey.Ctrl,
                        6 => ModifierKey.Shift,
                        _ => ModifierKey.None
                    };

                    // Update selection state for all labels
                    for (int j = 0; j < keyLabels.Length; j++)
                    {
                        keyLabels[j].IsSelected = (j == index);
                        keyLabels[j].Invalidate();
                    }

                    onChange(newKey);
                };
                contentPanel.Controls.Add(keyLabel);
            }

            currentY += 40;
        }

        private void AddKeySelector(string label, string subText, char currentKey, Action<char> onChange)
        {
            var labelCtrl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, currentY),
                AutoSize = true
            };
            contentPanel.Controls.Add(labelCtrl);

            var subLabel = new Label
            {
                Text = subText,
                ForeColor = SubTextColor,
                Location = new Point(20, currentY + 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f)
            };
            contentPanel.Controls.Add(subLabel);

            // Key display button
            var keyButton = new Button
            {
                Text = currentKey.ToString(),
                Size = new Size(40, 30),
                Location = new Point(contentPanel.Width - 80, currentY),
                BackColor = ToggleBgColor,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            keyButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            // Click handler to capture new key
            keyButton.Click += (s, e) =>
            {
                var btn = (Button)s!;
                string originalText = btn.Text;
                Color originalBgColor = btn.BackColor;

                btn.Text = "...";
                btn.BackColor = AccentColor;

                // Create temporary key handler
                KeyEventHandler? keyHandler = null;
                keyHandler = (sender, args) =>
                {
                    char? newKey = KeyCodeToChar(args.KeyCode);
                    if (newKey.HasValue)
                    {
                        btn.Text = newKey.Value.ToString();
                        onChange(newKey.Value);
                    }
                    else
                    {
                        btn.Text = originalText; // Revert if invalid key
                    }
                    btn.BackColor = originalBgColor;
                    this.KeyDown -= keyHandler;
                    this.KeyPreview = false;
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                };

                this.KeyPreview = true;
                this.KeyDown += keyHandler;
            };

            contentPanel.Controls.Add(keyButton);

            currentY += 50;
        }

        /// <summary>
        /// Converts a KeyCode to a character for key selector.
        /// </summary>
        private static char? KeyCodeToChar(Keys keyCode)
        {
            // Letters
            if (keyCode >= Keys.A && keyCode <= Keys.Z)
                return (char)('A' + (keyCode - Keys.A));

            // Numbers
            if (keyCode >= Keys.D0 && keyCode <= Keys.D9)
                return (char)('0' + (keyCode - Keys.D0));

            // Special keys
            return keyCode switch
            {
                Keys.OemMinus => '-',
                Keys.Oemplus => '=',
                Keys.OemOpenBrackets => '[',
                Keys.OemCloseBrackets => ']',
                Keys.OemSemicolon => ';',
                Keys.OemQuotes => '\'',
                Keys.Oemcomma => ',',
                Keys.OemPeriod => '.',
                Keys.OemQuestion => '/',
                Keys.OemPipe => '\\',
                Keys.Oemtilde => '`',
                _ => null
            };
        }

        private void AddKeyboardRow(string[] keys, int xOffset)
        {
            int keySize = 40;
            int spacing = 5;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                bool isLetter = key.Length == 1 && char.IsLetter(key[0]);
                bool isCaptured = !isLetter || AppSettings.Instance.IsKeyCaptured(key[0]);

                var keyButton = new KeyboardKey(key, isCaptured, isLetter)
                {
                    Location = new Point(xOffset + i * (keySize + spacing), currentY),
                    Size = new Size(keySize, keySize)
                };

                if (isLetter)
                {
                    keyButton.Click += (s, e) =>
                    {
                        var btn = (KeyboardKey)s!;
                        btn.IsCaptured = !btn.IsCaptured;
                        AppSettings.Instance.SetKeyCaptured(btn.KeyChar, btn.IsCaptured);
                        btn.Invalidate();
                    };
                }

                contentPanel.Controls.Add(keyButton);
            }

            currentY += keySize + spacing;
        }

        private void AddSeparator()
        {
            var separator = new Panel
            {
                Location = new Point(20, currentY),
                Size = new Size(contentPanel.Width - 40, 1),
                BackColor = Color.FromArgb(60, 60, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            contentPanel.Controls.Add(separator);
            currentY += 20;
        }
    }

    // Custom round checkbox to match macOS style
    public class RoundCheckBox : CheckBox
    {
        public RoundCheckBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.Clear(this.Parent?.BackColor ?? Color.FromArgb(30, 30, 30));

            int checkSize = 20;
            var checkRect = new Rectangle(0, (this.Height - checkSize) / 2, checkSize, checkSize);

            // Draw circle
            using (var brush = new SolidBrush(this.Checked ? Color.FromArgb(100, 149, 237) : Color.FromArgb(60, 60, 60)))
            {
                e.Graphics.FillEllipse(brush, checkRect);
            }

            // Draw checkmark if checked
            if (this.Checked)
            {
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    var points = new Point[]
                    {
                        new Point(checkRect.X + 5, checkRect.Y + checkSize / 2),
                        new Point(checkRect.X + checkSize / 2 - 1, checkRect.Y + checkSize - 6),
                        new Point(checkRect.X + checkSize - 5, checkRect.Y + 6)
                    };
                    e.Graphics.DrawLines(pen, points);
                }
            }

            // Draw text
            using (var brush = new SolidBrush(this.ForeColor))
            {
                float textX = checkSize + 10;
                float textY = (this.Height - e.Graphics.MeasureString(this.Text, this.Font).Height) / 2;
                e.Graphics.DrawString(this.Text, this.Font, brush, textX, textY);
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            using (var g = this.CreateGraphics())
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                var textSize = g.MeasureString(this.Text, this.Font);
                return new Size((int)(textSize.Width + 35), Math.Max(24, (int)(textSize.Height + 4)));
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            this.Size = GetPreferredSize(Size.Empty);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            this.Size = GetPreferredSize(Size.Empty);
        }
    }

    // Modifier key label for displaying key combinations
    public class ModifierKeyLabel : Control
    {
        private string keyText;
        private bool isClickable;

        public bool IsSelected { get; set; }

        public ModifierKeyLabel(string text, bool selected, bool clickable = false)
        {
            this.keyText = text;
            this.IsSelected = selected;
            this.isClickable = clickable;
            this.DoubleBuffered = true;

            if (clickable)
            {
                this.Cursor = Cursors.Hand;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            var path = GetRoundedRect(rect, 5);

            Color bgColor;
            Color textColor;

            if (keyText == "_")
            {
                bgColor = Color.FromArgb(40, 40, 40);
                textColor = Color.FromArgb(60, 60, 60);
            }
            else if (IsSelected)
            {
                bgColor = Color.FromArgb(140, 110, 80);
                textColor = Color.White;
            }
            else
            {
                bgColor = Color.FromArgb(60, 60, 60);
                textColor = Color.FromArgb(150, 150, 150);
            }

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Draw icon/symbol based on key type
            string displayText = keyText switch
            {
                "Shift" => "\u21E7",  // ⇧
                "Ctrl" => "^",
                "Alt" => "\u2325",    // ⌥
                "_" => "",
                _ => keyText
            };

            using (var brush = new SolidBrush(textColor))
            using (var font = new Font("Segoe UI Symbol", 10f))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(displayText, font, brush, rect, format);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Segmented control for toggle options
    public class SegmentedControl : Control
    {
        private string[] options;
        private int selectedIndex;
        private Action<int> onChange;

        public SegmentedControl(string[] options, int selectedIndex, Action<int> onChange)
        {
            this.options = options;
            this.selectedIndex = selectedIndex;
            this.onChange = onChange;
            this.Size = new Size(options.Length * 90, 30);
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int segmentWidth = this.Width / options.Length;

            using (var font = new Font("Segoe UI", 9f))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    var rect = new Rectangle(i * segmentWidth, 0, segmentWidth, this.Height);
                    var path = GetSegmentPath(rect, 5, i == 0, i == options.Length - 1);

                    Color bgColor = i == selectedIndex ? Color.FromArgb(80, 80, 80) : Color.FromArgb(50, 50, 50);
                    Color textColor = i == selectedIndex ? Color.White : Color.FromArgb(140, 140, 140);

                    using (var brush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (var brush = new SolidBrush(textColor))
                    {
                        var format = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        e.Graphics.DrawString(options[i], font, brush, rect, format);
                    }
                }
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int segmentWidth = this.Width / options.Length;
            int clickedIndex = e.X / segmentWidth;

            if (clickedIndex >= 0 && clickedIndex < options.Length && clickedIndex != selectedIndex)
            {
                selectedIndex = clickedIndex;
                onChange(selectedIndex);
                this.Invalidate();
            }
        }

        private GraphicsPath GetSegmentPath(Rectangle rect, int radius, bool isFirst, bool isLast)
        {
            var path = new GraphicsPath();

            if (isFirst)
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            }
            else
            {
                path.AddLine(rect.X, rect.Y, rect.X, rect.Y);
            }

            if (isLast)
            {
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            }
            else
            {
                path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
            }

            if (isFirst)
            {
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            }
            else
            {
                path.AddLine(rect.X, rect.Bottom, rect.X, rect.Y);
            }

            path.CloseFigure();
            return path;
        }
    }

    // Keyboard key button for captured keys display
    public class KeyboardKey : Control
    {
        public string KeyText { get; }
        public char KeyChar => KeyText.Length == 1 ? KeyText[0] : '\0';
        public bool IsCaptured { get; set; }
        public bool IsClickable { get; }

        public KeyboardKey(string keyText, bool isCaptured, bool isClickable)
        {
            this.KeyText = keyText;
            this.IsCaptured = isCaptured;
            this.IsClickable = isClickable;
            this.DoubleBuffered = true;

            if (isClickable)
            {
                this.Cursor = Cursors.Hand;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            var path = GetRoundedRect(rect, 6);

            Color bgColor;
            Color textColor;

            if (!IsClickable)
            {
                bgColor = Color.FromArgb(50, 50, 50);
                textColor = Color.FromArgb(100, 100, 100);
            }
            else if (IsCaptured)
            {
                bgColor = Color.FromArgb(90, 90, 90);
                textColor = Color.White;
            }
            else
            {
                bgColor = Color.FromArgb(45, 45, 45);
                textColor = Color.FromArgb(80, 80, 80);
            }

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            using (var brush = new SolidBrush(textColor))
            using (var font = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(KeyText, font, brush, rect, format);
            }
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Keep the LetterInputDialog for future use
    public class LetterInputDialog : Form
    {
        private TextBox letterInput;
        private Button okButton;
        private Button cancelButton;
        private Button resetButton;
        private Label promptLabel;

        public char? SelectedLetter { get; private set; }

        public LetterInputDialog(string processName)
        {
            this.Text = "Set Shortcut Letter";
            this.Size = new Size(320, 160);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            char currentLetter = AppSettings.Instance.GetLetterForProcess(processName);

            promptLabel = new Label
            {
                Text = $"Enter shortcut letter for {processName}:",
                Location = new Point(12, 15),
                Size = new Size(280, 20),
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            this.Controls.Add(promptLabel);

            letterInput = new TextBox
            {
                Location = new Point(12, 40),
                Size = new Size(50, 25),
                MaxLength = 1,
                Text = currentLetter.ToString(),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f),
                TextAlign = HorizontalAlignment.Center
            };
            letterInput.KeyPress += LetterInput_KeyPress;
            this.Controls.Add(letterInput);

            resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(70, 38),
                Size = new Size(70, 27),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            resetButton.Click += (s, e) =>
            {
                SelectedLetter = null;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(resetButton);

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(135, 85),
                Size = new Size(75, 27),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            this.Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(216, 85),
                Size = new Size(75, 27),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LetterInput_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsLetter(e.KeyChar) && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
            else if (char.IsLetter(e.KeyChar))
            {
                e.KeyChar = char.ToUpper(e.KeyChar);
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(letterInput.Text) && char.IsLetter(letterInput.Text[0]))
            {
                SelectedLetter = char.ToUpper(letterInput.Text[0]);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please enter a valid letter (A-Z).", "Invalid Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }
    }

    // App Management Dialog - for managing app shortcuts and exclusions
    public class AppManagementDialog : Form
    {
        private ListView appListView;
        private Button closeButton;
        private Label instructionLabel;
        private ApplicationManager appManager;
        private bool isLoadingApps = false;

        private static readonly Color BgColor = Color.FromArgb(30, 30, 30);
        private static readonly Color ListBgColor = Color.FromArgb(45, 45, 45);
        private static readonly Color TextColor = Color.White;

        public AppManagementDialog(ApplicationManager appManager)
        {
            this.appManager = appManager;
            InitializeForm();
            LoadApps();
        }

        private void InitializeForm()
        {
            this.Text = "Manage App Shortcuts";
            this.Size = new Size(580, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 9f);

            instructionLabel = new Label
            {
                Text = "Double-click to change shortcut letter. Use checkbox to enable/disable apps.",
                Location = new Point(15, 15),
                Size = new Size(540, 20),
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 9f)
            };
            this.Controls.Add(instructionLabel);

            appListView = new ListView
            {
                Location = new Point(15, 45),
                Size = new Size(535, 360),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                BackColor = ListBgColor,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };

            appListView.Columns.Add("Enabled", 65, HorizontalAlignment.Center);
            appListView.Columns.Add("Letter", 60, HorizontalAlignment.Center);
            appListView.Columns.Add("Application", 300, HorizontalAlignment.Left);
            appListView.Columns.Add("Status", 90, HorizontalAlignment.Center);

            appListView.DoubleClick += AppListView_DoubleClick;
            appListView.ItemChecked += AppListView_ItemChecked;

            this.Controls.Add(appListView);

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(460, 420),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            this.Controls.Add(closeButton);
        }

        private void LoadApps()
        {
            isLoadingApps = true;
            appListView.Items.Clear();

            var windows = appManager.GetAllWindows();
            var settings = AppSettings.Instance;

            // Group by process name to get unique apps
            var uniqueApps = windows
                .GroupBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(w => w.ProcessName)
                .ToList();

            foreach (var app in uniqueApps)
            {
                char letter = settings.GetLetterForProcess(app.ProcessName);
                bool isExcluded = settings.IsExcluded(app.ProcessName);
                bool hasCustomMapping = settings.ProcessLetterMappings.ContainsKey(app.ProcessName);

                var item = new ListViewItem();
                item.Checked = !isExcluded;
                item.SubItems.Add(letter.ToString());
                item.SubItems.Add(app.ProcessName);
                item.SubItems.Add(hasCustomMapping ? "Custom" : "Default");
                item.Tag = app.ProcessName;

                if (isExcluded)
                {
                    item.ForeColor = Color.Gray;
                }
                else if (hasCustomMapping)
                {
                    item.ForeColor = Color.FromArgb(100, 149, 237); // Cornflower blue
                }

                appListView.Items.Add(item);
            }
            isLoadingApps = false;
        }

        private void AppListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (isLoadingApps) return;

            string processName = e.Item.Tag?.ToString() ?? "";
            bool enabled = e.Item.Checked;

            AppSettings.Instance.SetExcluded(processName, !enabled);

            // Update the row appearance
            if (!enabled)
            {
                e.Item.ForeColor = Color.Gray;
            }
            else
            {
                bool hasCustomMapping = AppSettings.Instance.ProcessLetterMappings.ContainsKey(processName);
                e.Item.ForeColor = hasCustomMapping ? Color.FromArgb(100, 149, 237) : Color.White;
            }
        }

        private void AppListView_DoubleClick(object? sender, EventArgs e)
        {
            if (appListView.SelectedItems.Count == 0) return;

            var selectedItem = appListView.SelectedItems[0];
            string processName = selectedItem.Tag?.ToString() ?? "";

            using (var dialog = new LetterInputDialog(processName))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (dialog.SelectedLetter.HasValue)
                    {
                        AppSettings.Instance.SetLetterForProcess(processName, dialog.SelectedLetter.Value);
                    }
                    else
                    {
                        AppSettings.Instance.RemoveMapping(processName);
                    }
                    LoadApps();
                }
            }
        }
    }
}
