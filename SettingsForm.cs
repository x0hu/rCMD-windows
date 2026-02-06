using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RcmdWindows
{
    public class SettingsForm : Form
    {
        private readonly ApplicationManager appManager;
        private readonly AppSettings settings;
        private readonly AppSettings draft;
        private readonly ToolTip toolTip;
        private readonly TableLayoutPanel root;
        private readonly Panel leftColumn;
        private readonly Panel rightColumn;
        private readonly Panel footer;

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
            MinimumSize = new Size(980, 800);

            root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16),
                AutoSize = false
            };

            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            leftColumn = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoScroll = false,
                Padding = new Padding(0, 0, 8, 0)
            };

            rightColumn = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoScroll = false,
                Padding = new Padding(0, 0, 12, 0)
            };

            root.Controls.Add(leftColumn, 0, 0);
            root.Controls.Add(rightColumn, 1, 0);

            footer = BuildFooter();
            Controls.Add(root);
            Controls.Add(footer);

            Resize += (s, e) => UpdateSectionWidths();
            ApplyUiScale(draft.SettingsUiScale);
            BuildUI();
            UiTheme.Apply(this);
        }

        private void BuildUI()
        {
            AddSection(leftColumn, BuildGeneralSection());
            AddSection(leftColumn, BuildHotkeySection());
            AddSection(leftColumn, BuildKeyConfigSection());

            AddSection(rightColumn, BuildFocusSection());
            AddSection(rightColumn, BuildAppShortcutsSection());
            AddSection(rightColumn, BuildCapturedKeysSection());
        }

        private void AddSection(Control container, Control section)
        {
            section.Margin = new Padding(0, 0, 10, 16);
            section.Dock = DockStyle.Top;
            section.AutoSize = true;
            container.Controls.Add(section);
            section.BringToFront();

            if (section is GroupBox groupBox)
            {
                groupBox.AutoSize = false;
                int targetWidth = Math.Max(0, container.ClientSize.Width - 12);
                groupBox.Width = targetWidth;
                groupBox.PerformLayout();
                int preferredHeight = groupBox.GetPreferredSize(new Size(targetWidth, 0)).Height;
                groupBox.Height = preferredHeight + 20;
            }
        }

        private Panel BuildFooter()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                Padding = new Padding(16, 12, 16, 12)
            };

            var saveButton = new Button
            {
                Text = "Save",
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 6),
                MinimumSize = new Size(90, 34)
            };
            saveButton.Click += (s, e) =>
            {
                settings.ApplyFrom(draft, includeLaunchAtLogin: false);
                settings.SetLaunchAtLogin(draft.LaunchAtLogin);
                settings.Save();
                Close();
            };

            var closeButton = new Button
            {
                Text = "Close",
                AutoSize = true,
                Padding = new Padding(12, 6, 12, 6),
                MinimumSize = new Size(90, 34)
            };
            closeButton.Click += (s, e) => Close();

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 0)
            };

            flow.Controls.Add(saveButton);
            flow.Controls.Add(closeButton);
            panel.Controls.Add(flow);
            return panel;
        }

        private GroupBox BuildGeneralSection()
        {
            var box = CreateGroupBox("General");
            var panel = CreateFlowPanel();

            var launchAtLogin = CreateCheckBox("Launch at login", draft.LaunchAtLogin, (s, e) => { });
            launchAtLogin.CheckedChanged += (s, e) => draft.LaunchAtLogin = launchAtLogin.Checked;

            var hideTray = CreateCheckBox("Hide tray icon", draft.HideTrayIcon, (s, e) => { });
            hideTray.CheckedChanged += (s, e) =>
            {
                draft.HideTrayIcon = hideTray.Checked;
            };

            var excludeStatic = CreateCheckBox("Exclude static apps when cycling from dynamic apps", draft.ExcludeStaticApps, (s, e) => { });
            excludeStatic.CheckedChanged += (s, e) =>
            {
                draft.ExcludeStaticApps = excludeStatic.Checked;
            };

            panel.Controls.Add(launchAtLogin);
            panel.Controls.Add(hideTray);
            panel.Controls.Add(excludeStatic);

            var sizeTable = CreateTwoColumnTable();

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

            AddRow(sizeTable, overlaySizeLabel, overlaySize);

            panel.Controls.Add(sizeTable);
            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildHotkeySection()
        {
            var box = CreateGroupBox("Hotkeys");
            var panel = CreateFlowPanel();

            var assignLetter = CreateCheckBox("Enable assign letter hotkey (Alt)", draft.EnableAssignLetterHotkey, (s, e) => { });
            assignLetter.CheckedChanged += (s, e) =>
            {
                draft.EnableAssignLetterHotkey = assignLetter.Checked;
            };
            toolTip.SetToolTip(assignLetter, "Hold the app modifier and Alt to assign a letter to the focused app.");

            var forceCycle = CreateCheckBox("Enable force cycle hotkey (Right Shift)", draft.EnableForceCycleHotkey, (s, e) => { });
            forceCycle.CheckedChanged += (s, e) =>
            {
                draft.EnableForceCycleHotkey = forceCycle.Checked;
            };
            toolTip.SetToolTip(forceCycle, "Hold the app modifier and Right Shift to cycle apps with the same letter.");

            var hideOthers = CreateCheckBox("Enable hide others on focus (Left Shift)", draft.EnableHideOthersOnFocus, (s, e) => { });
            hideOthers.CheckedChanged += (s, e) =>
            {
                draft.EnableHideOthersOnFocus = hideOthers.Checked;
            };
            toolTip.SetToolTip(hideOthers, "Hold the app modifier and Left Shift to minimize other apps.");

            var singleApp = CreateCheckBox("Single app mode", draft.SingleAppMode, (s, e) => { });
            singleApp.CheckedChanged += (s, e) =>
            {
                draft.SingleAppMode = singleApp.Checked;
            };
            toolTip.SetToolTip(singleApp, "Always hide other apps when switching.");

            panel.Controls.Add(assignLetter);
            panel.Controls.Add(forceCycle);
            panel.Controls.Add(hideOthers);
            panel.Controls.Add(singleApp);

            box.Controls.Add(panel);
            return box;
        }

        private GroupBox BuildFocusSection()
        {
            var box = CreateGroupBox("Focus Behavior");
            var table = CreateTwoColumnTable();

            var appFocusLabel = CreateLabel("App window focus");
            var appFocus = CreateComboBox(new[] { "All windows", "Main window" });
            appFocus.SelectedIndex = draft.AppWindowFocus == WindowFocusBehavior.AllWindows ? 0 : 1;
            appFocus.SelectedIndexChanged += (s, e) =>
            {
                draft.AppWindowFocus = appFocus.SelectedIndex == 0
                    ? WindowFocusBehavior.AllWindows
                    : WindowFocusBehavior.MainWindow;
            };

            var alreadyFocusedLabel = CreateLabel("When already focused");
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

        private GroupBox BuildKeyConfigSection()
        {
            var box = CreateGroupBox("Key Configuration");
            var table = CreateTwoColumnTable();
            box.Padding = new Padding(12, 12, 12, 26);

            var appsKeyLabel = CreateLabel("Apps modifier key");
            var appsKey = CreateComboBox(Enum.GetNames(typeof(ModifierKey)));
            appsKey.SelectedItem = draft.AppsModifierKey.ToString();
            appsKey.SelectedIndexChanged += (s, e) =>
            {
                if (Enum.TryParse(appsKey.SelectedItem?.ToString(), out ModifierKey key))
                {
                    draft.AppsModifierKey = key;
                }
            };

            var minimizeScopeLabel = CreateLabel("Minimize key hides");
            var minimizeScope = CreateComboBox(new[] { "All apps", "Focused app" });
            minimizeScope.SelectedIndex = draft.MinimizeKeyHides == MinimizeScope.AllApps ? 0 : 1;
            minimizeScope.SelectedIndexChanged += (s, e) =>
            {
                draft.MinimizeKeyHides = minimizeScope.SelectedIndex == 0 ? MinimizeScope.AllApps : MinimizeScope.FocusedApp;
            };

            var minimizeAffectsLabel = CreateLabel("Minimize key affects");
            var minimizeAffects = CreateComboBox(new[] { "All windows", "Focused window" });
            minimizeAffects.SelectedIndex = draft.MinimizeKeyAffects == MinimizeAffects.AllWindows ? 0 : 1;
            minimizeAffects.SelectedIndexChanged += (s, e) =>
            {
                draft.MinimizeKeyAffects = minimizeAffects.SelectedIndex == 0
                    ? MinimizeAffects.AllWindows
                    : MinimizeAffects.FocusedWindow;
            };

            var hideKeyLabel = CreateLabel("Hide/Minimize key");
            var hideKey = CreateSingleCharTextBox(draft.HideMinimizeKey);
            hideKey.TextChanged += (s, e) =>
            {
                if (TryGetChar(hideKey.Text, out char value))
                {
                    draft.HideMinimizeKey = value;
                }
            };

            var menuKeyLabel = CreateLabel("Menu/Actions key");
            var menuKey = CreateSingleCharTextBox(draft.MenuActionsKey);
            menuKey.TextChanged += (s, e) =>
            {
                if (TryGetChar(menuKey.Text, out char value))
                {
                    draft.MenuActionsKey = value;
                }
            };

            AddRow(table, appsKeyLabel, appsKey);
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
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.Padding = new Padding(0, 0, 0, 6);
            box.Padding = new Padding(12, 12, 12, 18);

            var desc = new Label
            {
                Text = "Assign custom letters to apps and exclude apps from switching.",
                AutoSize = true
            };

            var button = new Button
            {
                Text = "Manage Apps...",
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
                Text = "Disable keys to prevent them from being captured by rcmd.",
                AutoSize = true
            };

            int listHeight = draft.SettingsUiScale switch
            {
                UiScale.Small => 160,
                UiScale.Medium => 220,
                UiScale.Large => 280,
                _ => 220
            };

            var list = new CheckedListBox
            {
                CheckOnClick = true,
                IntegralHeight = false,
                Height = listHeight,
                Dock = DockStyle.Top
            };

            var keyItems = BuildCapturedKeyItems();
            foreach (var item in keyItems)
            {
                bool captured = !draft.DisabledKeys.Contains(char.ToUpper(item.KeyChar));
                list.Items.Add(item, captured);
            }

            list.ItemCheck += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= list.Items.Count)
                    return;

                object? rawItem = list.Items[e.Index];
                if (rawItem is CapturedKeyItem keyItem)
                {
                    bool captured = e.NewValue == CheckState.Checked;
                    char upper = char.ToUpper(keyItem.KeyChar);
                    if (captured)
                    {
                        draft.DisabledKeys.Remove(upper);
                    }
                    else
                    {
                        draft.DisabledKeys.Add(upper);
                    }
                }
            };

            panel.Controls.Add(desc);
            panel.Controls.Add(list);
            box.Controls.Add(panel);
            return box;
        }

        private List<CapturedKeyItem> BuildCapturedKeyItems()
        {
            var items = new List<CapturedKeyItem>();

            for (char c = 'A'; c <= 'Z'; c++)
                items.Add(new CapturedKeyItem(c.ToString(), c));

            for (char c = '0'; c <= '9'; c++)
                items.Add(new CapturedKeyItem(c.ToString(), c));

            string extra = "-=[];\\',./`";
            foreach (var c in extra)
                items.Add(new CapturedKeyItem(c.ToString(), c));

            return items;
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
                Padding = new Padding(12)
            };
        }

        private static FlowLayoutPanel CreateFlowPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
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
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
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

        private static CheckBox CreateCheckBox(string text, bool isChecked, EventHandler onChanged)
        {
            var checkbox = new CheckBox
            {
                Text = text,
                AutoSize = true,
                Checked = isChecked
            };
            checkbox.CheckedChanged += onChanged;
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
                UiScale.Small => new Size(980, 800),
                UiScale.Medium => new Size(1100, 860),
                UiScale.Large => new Size(1260, 940),
                _ => new Size(1100, 860)
            };

            int basePadding = scale switch
            {
                UiScale.Small => 16,
                UiScale.Medium => 18,
                UiScale.Large => 20,
                _ => 18
            };

            int footerHeight = footer?.Height ?? 64;
            root.Padding = new Padding(basePadding, basePadding, basePadding, basePadding + footerHeight);

            if (footer != null)
            {
                footer.Height = scale switch
                {
                    UiScale.Small => 80,
                    UiScale.Medium => 84,
                    UiScale.Large => 90,
                    _ => 84
                };
            }

            UpdateSectionWidths();
        }

        private void UpdateSectionWidths()
        {
            UpdateColumnWidths(leftColumn);
            UpdateColumnWidths(rightColumn);
        }

        private static void UpdateColumnWidths(Control column)
        {
            int width = column.ClientSize.Width;
            foreach (Control child in column.Controls)
            {
                int targetWidth = Math.Max(0, width - 12);
                child.Width = targetWidth;

                if (child is GroupBox groupBox)
                {
                    groupBox.PerformLayout();
                    int preferredHeight = groupBox.GetPreferredSize(new Size(targetWidth, 0)).Height;
                    groupBox.Height = preferredHeight + 20;
                }
            }
        }

        private class CapturedKeyItem
        {
            public string Label { get; }
            public char KeyChar { get; }

            public CapturedKeyItem(string label, char keyChar)
            {
                Label = label;
                KeyChar = keyChar;
            }

            public override string ToString() => Label;
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
                CheckBoxes = true
            };

            appListView.Columns.Add("Enabled", 70, HorizontalAlignment.Center);
            appListView.Columns.Add("Letter", 60, HorizontalAlignment.Center);
            appListView.Columns.Add("Application", 300, HorizontalAlignment.Left);
            appListView.Columns.Add("Status", 90, HorizontalAlignment.Center);

            appListView.DoubleClick += AppListView_DoubleClick;
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
            UiTheme.ApplyListView(appListView);

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

        private void AppListView_DoubleClick(object? sender, EventArgs e)
        {
            if (appListView.SelectedItems.Count == 0)
                return;

            var selectedItem = appListView.SelectedItems[0];
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
}
