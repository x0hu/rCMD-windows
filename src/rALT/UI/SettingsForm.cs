using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace rALT
{
    public class SettingsForm : Form
    {
        private readonly ApplicationManager appManager;
        private readonly AppSettings settings;
        private readonly AppSettings draft;
        private readonly ToolTip toolTip;
        private readonly DarkTabControl tabControl;
        private readonly Panel footer;
        private readonly Dictionary<char, Button> capturedKeyButtons = new Dictionary<char, Button>();

        public SettingsForm(ApplicationManager appManager)
        {
            this.appManager = appManager;
            settings = AppSettings.Instance;
            draft = settings.Clone();
            toolTip = new ToolTip();

            Text = "Settings";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            MinimumSize = new Size(880, 680);

            tabControl = BuildTabs();

            footer = BuildFooter();
            Controls.Add(tabControl);
            Controls.Add(footer);

            ApplyUiScale(draft.SettingsUiScale);
            UiTheme.Apply(this);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            TryEnableDarkTitleBar();
        }

        private void TryEnableDarkTitleBar()
        {
            int enabled = 1;
            _ = DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int));
            _ = DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
        }

        private DarkTabControl BuildTabs()
        {
            var tabs = new DarkTabControl
            {
                Dock = DockStyle.Fill
            };

            tabs.TabPages.Add(BuildGeneralTab());
            tabs.TabPages.Add(BuildShortcutsTab());
            tabs.TabPages.Add(BuildSwitchingTab());
            tabs.TabPages.Add(BuildAppsTab());
            tabs.TabPages.Add(BuildAdvancedTab());

            return tabs;
        }

        private TabPage BuildGeneralTab()
        {
            return CreateTabPage(
                "General",
                BuildGeneralSection(),
                BuildAppearanceSection()
            );
        }

        private TabPage BuildShortcutsTab()
        {
            return CreateTabPage(
                "Shortcuts",
                BuildHotkeySection(),
                BuildCapturedKeysSection()
            );
        }

        private TabPage BuildSwitchingTab()
        {
            return CreateTabPage(
                "Switching",
                BuildFocusSection(),
                BuildSwitchingBehaviorSection()
            );
        }

        private TabPage BuildAppsTab()
        {
            return CreateTabPage(
                "Apps",
                BuildAppShortcutsSection()
            );
        }

        private TabPage BuildAdvancedTab()
        {
            return CreateTabPage(
                "Advanced",
                BuildKeyConfigSection(),
                BuildDataSection()
            );
        }

        private static TabPage CreateTabPage(string title, params Control[] sections)
        {
            var page = new TabPage(title);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12)
            };

            page.Controls.Add(scrollPanel);

            for (int i = sections.Length - 1; i >= 0; i--)
            {
                AddSection(scrollPanel, sections[i]);
            }

            return page;
        }

        private static void AddSection(Panel container, Control section)
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 12)
            };

            section.Dock = DockStyle.Top;
            section.AutoSize = true;
            wrapper.Controls.Add(section);
            container.Controls.Add(wrapper);
        }

        private Panel BuildFooter()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                Padding = new Padding(16, 12, 16, 12)
            };

            var applyButton = new Button
            {
                Text = "Apply",
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 6),
                MinimumSize = new Size(100, 34)
            };
            applyButton.Click += (s, e) => ApplyDraftSettings();

            var saveButton = new Button
            {
                Text = "Save && Close",
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 6),
                MinimumSize = new Size(120, 34)
            };
            saveButton.Click += (s, e) =>
            {
                ApplyDraftSettings();
                Close();
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 6),
                MinimumSize = new Size(100, 34)
            };
            cancelButton.Click += (s, e) => Close();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            flow.Controls.Add(applyButton);
            flow.Controls.Add(saveButton);
            flow.Controls.Add(cancelButton);
            panel.Controls.Add(flow);
            panel.Paint += (s, e) =>
            {
                using var pen = new Pen(UiTheme.Border);
                e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
            };
            return panel;
        }

        private GroupBox BuildGeneralSection()
        {
            var box = CreateGroupBox("General");
            var panel = CreateFlowPanel();

            var launchAtLogin = CreateCheckBox("Launch app automatically when you sign in", draft.LaunchAtLogin);
            launchAtLogin.CheckedChanged += (s, e) => draft.LaunchAtLogin = launchAtLogin.Checked;
            toolTip.SetToolTip(launchAtLogin, "Adds or removes this app from your Windows startup apps.");

            var hideTray = CreateCheckBox("Hide tray icon while running", draft.HideTrayIcon);
            hideTray.CheckedChanged += (s, e) => draft.HideTrayIcon = hideTray.Checked;
            toolTip.SetToolTip(hideTray, "If enabled, the app still runs in the background but won't show in the tray.");

            panel.Controls.Add(launchAtLogin);
            panel.Controls.Add(hideTray);
            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildAppearanceSection()
        {
            var box = CreateGroupBox("Appearance");
            var table = CreateTwoColumnTable();

            var overlaySizeLabel = CreateLabel("Overlay size");
            var overlaySize = CreateComboBox(Enum.GetNames(typeof(OverlaySize)));
            overlaySize.SelectedItem = draft.SwitcherOverlaySize.ToString();
            overlaySize.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse(overlaySize.SelectedItem?.ToString(), out OverlaySize size))
                {
                    draft.SwitcherOverlaySize = size;
                }
            };

            var settingsSizeLabel = CreateLabel("Settings window size");
            var settingsSize = CreateComboBox(Enum.GetNames(typeof(UiScale)));
            settingsSize.SelectedItem = draft.SettingsUiScale.ToString();
            settingsSize.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse(settingsSize.SelectedItem?.ToString(), out UiScale scale))
                {
                    draft.SettingsUiScale = scale;
                    ApplyUiScale(scale);
                }
            };

            AddRow(table, overlaySizeLabel, overlaySize);
            AddRow(table, settingsSizeLabel, settingsSize);
            box.Controls.Add(table);
            return box;
        }

        private GroupBox BuildHotkeySection()
        {
            var box = CreateGroupBox("Hotkeys");
            var panel = CreateFlowPanel();

            var assignLetter = CreateCheckBox("Allow Alt to assign app letters", draft.EnableAssignLetterHotkey);
            assignLetter.CheckedChanged += (s, e) => draft.EnableAssignLetterHotkey = assignLetter.Checked;
            toolTip.SetToolTip(assignLetter, "Hold the app modifier and Alt to assign a letter to the focused app.");

            var forceCycle = CreateCheckBox("Allow Right Shift to force cycle", draft.EnableForceCycleHotkey);
            forceCycle.CheckedChanged += (s, e) => draft.EnableForceCycleHotkey = forceCycle.Checked;
            toolTip.SetToolTip(forceCycle, "Hold the app modifier and Right Shift to cycle apps with the same letter.");

            var hideOthers = CreateCheckBox("Allow Left Shift to hide other apps", draft.EnableHideOthersOnFocus);
            hideOthers.CheckedChanged += (s, e) => draft.EnableHideOthersOnFocus = hideOthers.Checked;
            toolTip.SetToolTip(hideOthers, "Hold the app modifier and Left Shift to minimize other apps.");

            panel.Controls.Add(assignLetter);
            panel.Controls.Add(forceCycle);
            panel.Controls.Add(hideOthers);

            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildFocusSection()
        {
            var box = CreateGroupBox("Window Focus");
            var table = CreateTwoColumnTable();

            var appFocusLabel = CreateLabel("Bring windows");
            var appFocus = CreateComboBox(new[] { "All windows", "Main window" });
            appFocus.SelectedIndex = draft.AppWindowFocus == WindowFocusBehavior.AllWindows ? 0 : 1;
            appFocus.SelectedIndexChanged += (s, e) =>
            {
                draft.AppWindowFocus = appFocus.SelectedIndex == 0
                    ? WindowFocusBehavior.AllWindows
                    : WindowFocusBehavior.MainWindow;
            };

            var alreadyFocusedLabel = CreateLabel("If app is already focused");
            var alreadyFocused = CreateComboBox(new[] { "Hide app", "Cycle apps", "Cycle windows" });
            alreadyFocused.SelectedIndex = (int)draft.WhenAlreadyFocusedBehavior;
            alreadyFocused.SelectedIndexChanged += (s, e) =>
            {
                draft.WhenAlreadyFocusedBehavior = (WhenAlreadyFocused)alreadyFocused.SelectedIndex;
            };

            AddRow(table, appFocusLabel, appFocus);
            AddRow(table, alreadyFocusedLabel, alreadyFocused);

            box.Controls.Add(table);
            return box;
        }

        private GroupBox BuildSwitchingBehaviorSection()
        {
            var box = CreateGroupBox("Switching Behavior");
            var panel = CreateFlowPanel();

            var excludeStatic = CreateCheckBox("Prefer active apps when cycling", draft.ExcludeStaticApps);
            excludeStatic.CheckedChanged += (s, e) => draft.ExcludeStaticApps = excludeStatic.Checked;
            toolTip.SetToolTip(excludeStatic, "Prioritize active/running apps while cycling.");

            var singleAppMode = CreateCheckBox("Single app mode (hide others on switch)", draft.SingleAppMode);
            singleAppMode.CheckedChanged += (s, e) => draft.SingleAppMode = singleAppMode.Checked;
            toolTip.SetToolTip(singleAppMode, "Keep only the selected app visible after switching.");

            panel.Controls.Add(excludeStatic);
            panel.Controls.Add(singleAppMode);
            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildKeyConfigSection()
        {
            var box = CreateGroupBox("Key Mapping");
            var table = CreateTwoColumnTable();

            var appsKeyLabel = CreateLabel("App modifier");
            var appsKey = CreateComboBox(Enum.GetNames(typeof(ModifierKey)));
            appsKey.SelectedItem = draft.AppsModifierKey.ToString();
            appsKey.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse(appsKey.SelectedItem?.ToString(), out ModifierKey key))
                {
                    draft.AppsModifierKey = key;
                }
            };

            var windowsKeyLabel = CreateLabel("Windows modifier");
            var windowsKey = CreateComboBox(Enum.GetNames(typeof(ModifierKey)));
            windowsKey.SelectedItem = draft.WindowsModifierKey.ToString();
            windowsKey.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse(windowsKey.SelectedItem?.ToString(), out ModifierKey key))
                {
                    draft.WindowsModifierKey = key;
                }
            };

            var minimizeScopeLabel = CreateLabel("Minimize key scope");
            var minimizeScope = CreateComboBox(new[] { "All apps", "Focused app" });
            minimizeScope.SelectedIndex = draft.MinimizeKeyHides == MinimizeScope.AllApps ? 0 : 1;
            minimizeScope.SelectedIndexChanged += (s, e) =>
            {
                draft.MinimizeKeyHides = minimizeScope.SelectedIndex == 0 ? MinimizeScope.AllApps : MinimizeScope.FocusedApp;
            };

            var minimizeAffectsLabel = CreateLabel("Minimize key target");
            var minimizeAffects = CreateComboBox(new[] { "All windows", "Focused window" });
            minimizeAffects.SelectedIndex = draft.MinimizeKeyAffects == MinimizeAffects.AllWindows ? 0 : 1;
            minimizeAffects.SelectedIndexChanged += (s, e) =>
            {
                draft.MinimizeKeyAffects = minimizeAffects.SelectedIndex == 0
                    ? MinimizeAffects.AllWindows
                    : MinimizeAffects.FocusedWindow;
            };

            var hideKeyLabel = CreateLabel("Hide/minimize key");
            var hideKey = CreateSingleCharTextBox(draft.HideMinimizeKey);
            hideKey.TextChanged += (s, e) =>
            {
                if (TryGetChar(hideKey.Text, out char value))
                {
                    draft.HideMinimizeKey = value;
                }
            };

            var menuKeyLabel = CreateLabel("Actions key");
            var menuKey = CreateSingleCharTextBox(draft.MenuActionsKey);
            menuKey.TextChanged += (s, e) =>
            {
                if (TryGetChar(menuKey.Text, out char value))
                {
                    draft.MenuActionsKey = value;
                }
            };

            AddRow(table, appsKeyLabel, appsKey);
            AddRow(table, windowsKeyLabel, windowsKey);
            AddRow(table, minimizeScopeLabel, minimizeScope);
            AddRow(table, minimizeAffectsLabel, minimizeAffects);
            AddRow(table, hideKeyLabel, hideKey);
            AddRow(table, menuKeyLabel, menuKey);

            box.Controls.Add(table);
            return box;
        }

        private GroupBox BuildAppShortcutsSection()
        {
            var box = CreateGroupBox("App Shortcuts");
            var panel = CreateFlowPanel();

            var desc = new Label
            {
                Text = "Pick letters for apps and choose which apps are included.",
                AutoSize = true
            };

            var button = new Button
            {
                Text = "Manage app letters...",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                MinimumSize = new Size(170, 32)
            };
            button.Margin = new Padding(0, 6, 0, 6);

            button.Click += (s, e) =>
            {
                using (var dialog = new AppManagementDialog(appManager, draft))
                {
                    dialog.ShowDialog(this);
                }
            };

            panel.Controls.Add(desc);
            panel.Controls.Add(button);
            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildCapturedKeysSection()
        {
            var box = CreateGroupBox("Captured Keys");
            var panel = CreateFlowPanel();

            var desc = new Label
            {
                Text = "Blue = captured by rALT. Gray = pass-through to apps. Default is all blue.",
                AutoSize = true
            };

            var resetButton = new Button
            {
                Text = "Reset to default",
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0, 6, 0, 8)
            };
            resetButton.Click += (s, e) =>
            {
                draft.DisabledKeys.Clear();
                RefreshMappedKeyStyles();
            };

            var keyboard = BuildCapturedKeyKeyboard();

            panel.Controls.Add(desc);
            panel.Controls.Add(resetButton);
            panel.Controls.Add(keyboard);
            box.Controls.Add(panel);
            return box;
        }

        private Control BuildCapturedKeyKeyboard()
        {
            capturedKeyButtons.Clear();

            var keyboard = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = UiTheme.Panel,
                Padding = new Padding(10),
                Margin = new Padding(0, 8, 0, 0)
            };

            AddKeyboardRow(keyboard, new[]
            {
                KeyboardKeySpec.Mapped("`", '`'),
                KeyboardKeySpec.Mapped("1", '1'),
                KeyboardKeySpec.Mapped("2", '2'),
                KeyboardKeySpec.Mapped("3", '3'),
                KeyboardKeySpec.Mapped("4", '4'),
                KeyboardKeySpec.Mapped("5", '5'),
                KeyboardKeySpec.Mapped("6", '6'),
                KeyboardKeySpec.Mapped("7", '7'),
                KeyboardKeySpec.Mapped("8", '8'),
                KeyboardKeySpec.Mapped("9", '9'),
                KeyboardKeySpec.Mapped("0", '0'),
                KeyboardKeySpec.Mapped("-", '-'),
                KeyboardKeySpec.Mapped("=", '='),
                KeyboardKeySpec.Static("Backspace", 3)
            });

            AddKeyboardRow(keyboard, new[]
            {
                KeyboardKeySpec.Static("Tab", 2),
                KeyboardKeySpec.Mapped("Q", 'Q'),
                KeyboardKeySpec.Mapped("W", 'W'),
                KeyboardKeySpec.Mapped("E", 'E'),
                KeyboardKeySpec.Mapped("R", 'R'),
                KeyboardKeySpec.Mapped("T", 'T'),
                KeyboardKeySpec.Mapped("Y", 'Y'),
                KeyboardKeySpec.Mapped("U", 'U'),
                KeyboardKeySpec.Mapped("I", 'I'),
                KeyboardKeySpec.Mapped("O", 'O'),
                KeyboardKeySpec.Mapped("P", 'P'),
                KeyboardKeySpec.Mapped("[", '['),
                KeyboardKeySpec.Mapped("]", ']'),
                KeyboardKeySpec.Mapped("\\", '\\', 2)
            });

            AddKeyboardRow(keyboard, new[]
            {
                KeyboardKeySpec.Static("Caps", 2),
                KeyboardKeySpec.Mapped("A", 'A'),
                KeyboardKeySpec.Mapped("S", 'S'),
                KeyboardKeySpec.Mapped("D", 'D'),
                KeyboardKeySpec.Mapped("F", 'F'),
                KeyboardKeySpec.Mapped("G", 'G'),
                KeyboardKeySpec.Mapped("H", 'H'),
                KeyboardKeySpec.Mapped("J", 'J'),
                KeyboardKeySpec.Mapped("K", 'K'),
                KeyboardKeySpec.Mapped("L", 'L'),
                KeyboardKeySpec.Mapped(";", ';'),
                KeyboardKeySpec.Mapped("'", '\''),
                KeyboardKeySpec.Static("Enter", 3)
            });

            AddKeyboardRow(keyboard, new[]
            {
                KeyboardKeySpec.Static("Shift", 3),
                KeyboardKeySpec.Mapped("Z", 'Z'),
                KeyboardKeySpec.Mapped("X", 'X'),
                KeyboardKeySpec.Mapped("C", 'C'),
                KeyboardKeySpec.Mapped("V", 'V'),
                KeyboardKeySpec.Mapped("B", 'B'),
                KeyboardKeySpec.Mapped("N", 'N'),
                KeyboardKeySpec.Mapped("M", 'M'),
                KeyboardKeySpec.Mapped(",", ','),
                KeyboardKeySpec.Mapped(".", '.'),
                KeyboardKeySpec.Mapped("/", '/'),
                KeyboardKeySpec.Static("Shift", 3)
            });

            AddKeyboardRow(keyboard, new[]
            {
                KeyboardKeySpec.Static("Ctrl", 2),
                KeyboardKeySpec.Static("Win", 2),
                KeyboardKeySpec.Static("Alt", 2),
                KeyboardKeySpec.Static("Space", 8),
                KeyboardKeySpec.Static("Alt", 2),
                KeyboardKeySpec.Static("Menu", 2),
                KeyboardKeySpec.Static("Ctrl", 2)
            });

            return keyboard;
        }

        private void AddKeyboardRow(TableLayoutPanel keyboard, IReadOnlyList<KeyboardKeySpec> keys)
        {
            var row = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = UiTheme.Panel,
                Margin = new Padding(0, 0, 0, 6)
            };

            foreach (var key in keys)
            {
                row.Controls.Add(CreateKeyboardKeyControl(key));
            }

            keyboard.Controls.Add(row);
        }

        private Control CreateKeyboardKeyControl(KeyboardKeySpec key)
        {
            const int unitWidth = 40;
            const int keyHeight = 34;
            int keyWidth = (unitWidth * key.WidthUnits) + ((key.WidthUnits - 1) * 4);

            if (key.KeyChar.HasValue)
            {
                var mappedButton = new Button
                {
                    AutoSize = false,
                    Size = new Size(keyWidth, keyHeight),
                    Text = key.Label,
                    TextAlign = ContentAlignment.MiddleCenter,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(0, 0, 4, 4),
                    UseVisualStyleBackColor = false,
                    TabStop = false
                };

                char normalizedKey = char.ToUpperInvariant(key.KeyChar.Value);
                capturedKeyButtons[normalizedKey] = mappedButton;

                bool isCaptured = IsCaptured(normalizedKey);
                ApplyMappedKeyStyle(mappedButton, isCaptured);

                mappedButton.Click += (s, e) =>
                {
                    bool nextCaptured = !IsCaptured(normalizedKey);
                    SetCaptured(normalizedKey, nextCaptured);
                    ApplyMappedKeyStyle(mappedButton, nextCaptured);
                };

                return mappedButton;
            }

            var spacer = new Button
            {
                AutoSize = false,
                Size = new Size(keyWidth, keyHeight),
                Text = key.Label,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 4, 4),
                BackColor = UiTheme.Panel,
                ForeColor = UiTheme.MutedText,
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            spacer.FlatAppearance.BorderColor = UiTheme.Border;
            spacer.FlatAppearance.MouseOverBackColor = UiTheme.Panel;
            spacer.FlatAppearance.MouseDownBackColor = UiTheme.Panel;
            return spacer;
        }

        private static void ApplyMappedKeyStyle(Button button, bool captured)
        {
            button.BackColor = captured ? UiTheme.Accent : UiTheme.Control;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = captured ? UiTheme.Accent : UiTheme.Border;
            button.FlatAppearance.MouseOverBackColor = button.BackColor;
            button.FlatAppearance.MouseDownBackColor = button.BackColor;
        }

        private void RefreshMappedKeyStyles()
        {
            foreach (var pair in capturedKeyButtons)
            {
                ApplyMappedKeyStyle(pair.Value, IsCaptured(pair.Key));
            }
        }

        private bool IsCaptured(char key)
        {
            return !draft.DisabledKeys.Contains(char.ToUpperInvariant(key));
        }

        private void SetCaptured(char key, bool captured)
        {
            char normalized = char.ToUpperInvariant(key);
            if (captured)
            {
                draft.DisabledKeys.Remove(normalized);
            }
            else
            {
                draft.DisabledKeys.Add(normalized);
            }
        }

        private readonly struct KeyboardKeySpec
        {
            private KeyboardKeySpec(string label, char? keyChar, int widthUnits)
            {
                Label = label;
                KeyChar = keyChar;
                WidthUnits = widthUnits;
            }

            public string Label { get; }
            public char? KeyChar { get; }
            public int WidthUnits { get; }

            public static KeyboardKeySpec Mapped(string label, char keyChar, int widthUnits = 1)
            {
                return new KeyboardKeySpec(label, keyChar, widthUnits);
            }

            public static KeyboardKeySpec Static(string label, int widthUnits = 1)
            {
                return new KeyboardKeySpec(label, null, widthUnits);
            }
        }

        private GroupBox BuildDataSection()
        {
            var box = CreateGroupBox("Data");
            var label = new Label
            {
                Text = $"Settings file:\n{System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "rALT", "settings.json")}",
                AutoSize = true,
                MaximumSize = new Size(760, 0)
            };
            box.Controls.Add(label);
            return box;
        }

        private void ApplyDraftSettings()
        {
            settings.ApplyFrom(draft, includeLaunchAtLogin: false);
            settings.SetLaunchAtLogin(draft.LaunchAtLogin);
            settings.Save();
        }

        private static bool TryGetChar(string text, out char value)
        {
            value = '\0';
            if (string.IsNullOrWhiteSpace(text))
                return false;

            value = text.Trim()[0];
            return true;
        }

        private static GroupBox CreateGroupBox(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
        }

        private static FlowLayoutPanel CreateFlowPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0)
            };
        }

        private static TableLayoutPanel CreateTwoColumnTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            return table;
        }

        private static void AddRow(TableLayoutPanel table, Control left, Control right)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.Margin = new Padding(0, 0, 8, 8);
            right.Margin = new Padding(0, 0, 0, 8);
            left.Dock = DockStyle.Fill;
            right.Dock = DockStyle.Fill;
            table.Controls.Add(left, 0, row);
            table.Controls.Add(right, 1, row);
        }

        private static CheckBox CreateCheckBox(string text, bool isChecked)
        {
            var checkbox = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Checked = isChecked
            };
            return checkbox;
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static ComboBox CreateComboBox(IEnumerable<string> items)
        {
            var box = new NoScrollComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            box.Items.AddRange(items.Cast<object>().ToArray());
            UiTheme.ApplyComboBox(box);
            return box;
        }

        private static TextBox CreateSingleCharTextBox(char value)
        {
            var textBox = new TextBox
            {
                Width = 60,
                MaxLength = 1,
                Text = value.ToString()
            };

            textBox.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && e.KeyChar != '\b')
                {
                    e.KeyChar = char.ToUpper(e.KeyChar);
                }
            };

            return textBox;
        }

        private void ApplyUiScale(UiScale scale)
        {
            var baseFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
            float baseSize = baseFont.Size;
            float fontSize = scale switch
            {
                UiScale.Small => baseSize,
                UiScale.Medium => baseSize + 1f,
                UiScale.Large => baseSize + 2f,
                _ => baseSize + 1f
            };

            Font = new Font(baseFont.FontFamily, fontSize, FontStyle.Regular);

            ClientSize = scale switch
            {
                UiScale.Small => new Size(900, 680),
                UiScale.Medium => new Size(1020, 760),
                UiScale.Large => new Size(1140, 840),
                _ => new Size(1020, 760)
            };
        }

    }

    public class LetterInputDialog : Form
    {
        private readonly TextBox letterInput;
        private readonly Button okButton;
        private readonly Button cancelButton;
        private readonly Button resetButton;
        private readonly Label promptLabel;
        private readonly AppSettings settings;

        public char? SelectedLetter { get; private set; }

        public LetterInputDialog(string processName, AppSettings settings)
        {
            this.settings = settings;
            Text = "Set Shortcut Letter";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(320, 150);

            char currentLetter = settings.GetLetterForProcess(processName);

            promptLabel = new Label
            {
                Text = $"Enter shortcut letter for {processName}:",
                Location = new Point(12, 12),
                Size = new Size(296, 20)
            };
            Controls.Add(promptLabel);

            letterInput = new TextBox
            {
                Location = new Point(12, 40),
                Size = new Size(50, 25),
                MaxLength = 1,
                Text = currentLetter.ToString(),
                TextAlign = HorizontalAlignment.Center
            };
            letterInput.KeyPress += LetterInput_KeyPress;
            Controls.Add(letterInput);

            resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(70, 38),
                Size = new Size(70, 27)
            };
            resetButton.Click += (s, e) =>
            {
                SelectedLetter = null;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(resetButton);

            okButton = new Button
            {
                Text = "OK",
                Location = new Point(135, 95),
                Size = new Size(75, 27),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(216, 95),
                Size = new Size(75, 27),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            UiTheme.Apply(this);
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
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(
                    "Please enter a valid letter (A-Z).",
                    "Invalid Input",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                DialogResult = DialogResult.None;
            }
        }
    }

    public class AppManagementDialog : Form
    {
        private readonly ListView appListView;
        private readonly Button closeButton;
        private readonly Label instructionLabel;
        private readonly ApplicationManager appManager;
        private readonly AppSettings settings;
        private bool isLoadingApps = false;

        public AppManagementDialog(ApplicationManager appManager, AppSettings settings)
        {
            this.appManager = appManager;
            this.settings = settings;

            Text = "Manage App Shortcuts";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(600, 520);

            instructionLabel = new Label
            {
                Text = "Double-click to change shortcut letter. Use checkbox to enable/disable apps.",
                Location = new Point(12, 12),
                Size = new Size(560, 20)
            };
            Controls.Add(instructionLabel);

            appListView = new ListView
            {
                Location = new Point(12, 40),
                Size = new Size(560, 410),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                HideSelection = false,
                MultiSelect = false
            };

            appListView.Columns.Add("Enabled", 80, HorizontalAlignment.Center);
            appListView.Columns.Add("Letter", 60, HorizontalAlignment.Center);
            appListView.Columns.Add("Application", 300, HorizontalAlignment.Left);
            appListView.Columns.Add("Status", 90, HorizontalAlignment.Center);

            appListView.MouseDoubleClick += AppListView_MouseDoubleClick;
            appListView.ItemChecked += AppListView_ItemChecked;

            Controls.Add(appListView);

            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(482, 465),
                Size = new Size(90, 30)
            };
            closeButton.Click += (s, e) => Close();
            Controls.Add(closeButton);

            UiTheme.Apply(this);
            UiTheme.ApplyListViewBasic(appListView);

            LoadApps();
        }

        private void LoadApps()
        {
            isLoadingApps = true;
            appListView.Items.Clear();

            var windows = appManager.GetAllWindows();
            var uniqueApps = windows
                .GroupBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(w => w.ProcessName)
                .ToList();

            foreach (var app in uniqueApps)
            {
                char letter = settings.GetLetterForProcess(app.ProcessName);
                bool isExcluded = settings.ExcludedProcesses.Contains(app.ProcessName);
                bool hasCustomMapping = settings.ProcessLetterMappings.ContainsKey(app.ProcessName);

                var item = new ListViewItem
                {
                    Checked = !isExcluded
                };
                item.Text = isExcluded ? "No" : "Yes";

                item.SubItems.Add(letter.ToString());
                item.SubItems.Add(app.ProcessName);
                item.SubItems.Add(hasCustomMapping ? "Custom" : "Default");
                item.Tag = app.ProcessName;

                if (isExcluded)
                {
                    item.ForeColor = UiTheme.MutedText;
                }
                else if (hasCustomMapping)
                {
                    item.ForeColor = UiTheme.Accent;
                }

                appListView.Items.Add(item);
            }

            isLoadingApps = false;
        }

        private void AppListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (isLoadingApps)
                return;

            if (e.Item == null)
                return;

            string processName = e.Item.Tag?.ToString() ?? "";
            bool enabled = e.Item.Checked;

            if (enabled)
            {
                settings.ExcludedProcesses.Remove(processName);
            }
            else
            {
                settings.ExcludedProcesses.Add(processName);
            }

            e.Item.Text = enabled ? "Yes" : "No";

            if (!enabled)
            {
                e.Item.ForeColor = UiTheme.MutedText;
            }
            else
            {
                bool hasCustomMapping = settings.ProcessLetterMappings.ContainsKey(processName);
                e.Item.ForeColor = hasCustomMapping ? UiTheme.Accent : UiTheme.Text;
            }
        }

        private void AppListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var hit = appListView.HitTest(e.Location);
            if (hit.Item == null)
                return;

            // Ignore checkbox double-clicks so enabling/disabling never opens the letter dialog.
            if ((hit.Location & ListViewHitTestLocations.StateImage) != 0)
                return;

            var selectedItem = hit.Item;
            string processName = selectedItem.Tag?.ToString() ?? "";

            using (var dialog = new LetterInputDialog(processName, settings))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (dialog.SelectedLetter.HasValue)
                    {
                        settings.ProcessLetterMappings[processName] = char.ToUpper(dialog.SelectedLetter.Value);
                    }
                    else
                    {
                        settings.ProcessLetterMappings.Remove(processName);
                    }
                    LoadApps();
                }
            }
        }
    }

    internal static class UiTheme
    {
        public static readonly Color Bg = Color.FromArgb(24, 24, 24);
        public static readonly Color Panel = Color.FromArgb(32, 32, 32);
        public static readonly Color Control = Color.FromArgb(45, 45, 45);
        public static readonly Color Input = Color.FromArgb(38, 38, 38);
        public static readonly Color Border = Color.FromArgb(70, 70, 70);
        public static readonly Color Text = Color.FromArgb(230, 230, 230);
        public static readonly Color MutedText = Color.FromArgb(160, 160, 160);
        public static readonly Color Accent = Color.FromArgb(96, 160, 240);

        public static void Apply(Form form)
        {
            form.BackColor = Bg;
            form.ForeColor = Text;
            ApplyToControls(form.Controls);
        }

        public static void ApplyComboBox(ComboBox box)
        {
            box.BackColor = Input;
            box.ForeColor = Text;
            box.FlatStyle = FlatStyle.Flat;
            box.DrawMode = DrawMode.OwnerDrawFixed;
            box.DrawItem -= ComboBox_DrawItem;
            box.DrawItem += ComboBox_DrawItem;
        }

        public static void ApplyListView(ListView listView)
        {
            listView.BackColor = Input;
            listView.ForeColor = Text;
            listView.BorderStyle = BorderStyle.FixedSingle;
            listView.OwnerDraw = true;
            listView.DrawColumnHeader -= ListView_DrawColumnHeader;
            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem -= ListView_DrawItem;
            listView.DrawItem += ListView_DrawItem;
            listView.DrawSubItem -= ListView_DrawSubItem;
            listView.DrawSubItem += ListView_DrawSubItem;
        }

        public static void ApplyListViewBasic(ListView listView)
        {
            listView.OwnerDraw = false;
            listView.BackColor = Input;
            listView.ForeColor = Text;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case GroupBox groupBox:
                        groupBox.ForeColor = Text;
                        groupBox.BackColor = Panel;
                        break;
                    case TableLayoutPanel table:
                        table.BackColor = Bg;
                        break;
                    case FlowLayoutPanel flow:
                        flow.BackColor = Bg;
                        break;
                    case TabControl tabControl:
                        tabControl.BackColor = Bg;
                        tabControl.ForeColor = Text;
                        break;
                    case TabPage tabPage:
                        tabPage.BackColor = Bg;
                        tabPage.ForeColor = Text;
                        break;
                    case Panel panel:
                        panel.BackColor = Bg;
                        break;
                    case Label label:
                        label.ForeColor = Text;
                        label.BackColor = label.Parent?.BackColor ?? Bg;
                        break;
                    case CheckBox checkBox:
                        checkBox.ForeColor = Text;
                        checkBox.BackColor = checkBox.Parent?.BackColor ?? Bg;
                        break;
                    case Button button:
                        button.BackColor = Control;
                        button.ForeColor = Text;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderColor = Border;
                        break;
                    case TextBox textBox:
                        textBox.BackColor = Input;
                        textBox.ForeColor = Text;
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox comboBox:
                        ApplyComboBox(comboBox);
                        break;
                    case CheckedListBox checkedListBox:
                        checkedListBox.BackColor = Input;
                        checkedListBox.ForeColor = Text;
                        checkedListBox.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ListView listView:
                        ApplyListView(listView);
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyToControls(control.Controls);
                }
            }
        }

        private static void ComboBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox box)
                return;

            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected ? Accent : Input;
            Color fg = Text;

            using (var bgBrush = new SolidBrush(bg))
            using (var textBrush = new SolidBrush(fg))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
                if (e.Index >= 0)
                {
                    object? item = box.Items[e.Index];
                    string text = item?.ToString() ?? string.Empty;
                    e.Graphics.DrawString(text, box.Font, textBrush, e.Bounds);
                }
            }

            e.DrawFocusRectangle();
        }

        private static void ListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.Header == null || e.Font == null)
                return;

            using (var bg = new SolidBrush(Panel))
            using (var textBrush = new SolidBrush(Text))
            using (var pen = new Pen(Border))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
                e.Graphics.DrawRectangle(pen, e.Bounds);
                e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds);
            }
        }

        private static void ListView_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            if (e.Item == null)
                return;

            bool isSelected = e.Item.Selected;
            Color bg = isSelected ? Accent : Input;
            using (var bgBrush = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }
        }

        private static void ListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null || e.SubItem == null)
                return;

            bool isSelected = e.Item.Selected;
            Color fg = isSelected ? Color.White : e.Item.ForeColor;
            using (var textBrush = new SolidBrush(fg))
            {
                Font font = e.SubItem.Font ?? e.Item.Font ?? SystemFonts.DefaultFont;
                e.Graphics.DrawString(e.SubItem.Text, font, textBrush, e.Bounds);
            }
        }
    }

    internal sealed class NoScrollComboBox : ComboBox
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (DroppedDown)
            {
                base.OnMouseWheel(e);
                return;
            }

            // Ignore mouse wheel to prevent accidental value changes.
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL && !DroppedDown)
            {
                return;
            }

            base.WndProc(ref m);
        }
    }

    internal sealed class DarkTabControl : TabControl
    {
        private const int WM_PAINT = 0x000F;

        public DarkTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(150, 34);
            Padding = new Point(18, 8);
            Multiline = false;
            BackColor = UiTheme.Bg;
            ForeColor = UiTheme.Text;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            Resize += (s, e) => Invalidate();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (e.Control is TabPage page)
            {
                page.BackColor = UiTheme.Bg;
                page.ForeColor = UiTheme.Text;
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= TabPages.Count)
                return;

            Rectangle rect = GetTabRect(e.Index);
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color bg = isSelected ? UiTheme.Panel : UiTheme.Control;
            Color border = isSelected ? UiTheme.Accent : UiTheme.Border;

            using (var bgBrush = new SolidBrush(bg))
            using (var pen = new Pen(border))
            {
                e.Graphics.FillRectangle(bgBrush, rect);
                e.Graphics.DrawRectangle(pen, rect);
            }

            TextRenderer.DrawText(
                e.Graphics,
                TabPages[e.Index].Text,
                Font,
                rect,
                UiTheme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            );
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                PaintTabStripGaps();
            }
        }

        private void PaintTabStripGaps()
        {
            using var g = Graphics.FromHwnd(Handle);
            int stripHeight = ItemSize.Height + 10;

            if (TabCount == 0)
            {
                using var bg = new SolidBrush(UiTheme.Control);
                g.FillRectangle(bg, 0, 0, Width, stripHeight);
                return;
            }

            Rectangle firstTab = GetTabRect(0);
            Rectangle lastTab = GetTabRect(TabCount - 1);

            using (var bg = new SolidBrush(UiTheme.Control))
            {
                if (firstTab.Left > 0)
                {
                    g.FillRectangle(bg, new Rectangle(0, 0, firstTab.Left, stripHeight));
                }

                if (lastTab.Right < Width)
                {
                    g.FillRectangle(bg, new Rectangle(lastTab.Right, 0, Width - lastTab.Right, stripHeight));
                }
            }

            using var border = new Pen(UiTheme.Border);
            g.DrawLine(border, 0, stripHeight - 1, Width, stripHeight - 1);
        }
    }
}

