using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace RcmdWindows
{
    // Enums for settings options
    public enum WindowFocusBehavior { AllWindows, MainWindow }
    public enum WhenAlreadyFocused { HideApp, CycleApps, CycleWindows }
    public enum MinimizeScope { AllApps, FocusedApp }
    public enum MinimizeAffects { AllWindows, FocusedWindow }
    public enum ModifierKey { None, Shift, Ctrl, Alt, RightAlt, Win }

    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RcmdWindows",
            "settings.json"
        );

        // ===== General Settings =====
        public bool LaunchAtLogin { get; set; } = false;
        public bool HideTrayIcon { get; set; } = false;
        public bool ExcludeStaticApps { get; set; } = true;

        // ===== Hotkey Options =====
        public bool EnableAssignLetterHotkey { get; set; } = false;
        public bool EnableForceCycleHotkey { get; set; } = false;
        public bool EnableHideOthersOnFocus { get; set; } = false;
        public bool SingleAppMode { get; set; } = false;

        // ===== Focus Behavior =====
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WindowFocusBehavior AppWindowFocus { get; set; } = WindowFocusBehavior.AllWindows;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WhenAlreadyFocused WhenAlreadyFocusedBehavior { get; set; } = WhenAlreadyFocused.CycleApps;

        // ===== Key Configuration =====
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKey AppsModifierKey { get; set; } = ModifierKey.RightAlt;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKey WindowsModifierKey { get; set; } = ModifierKey.RightAlt;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MinimizeScope MinimizeKeyHides { get; set; } = MinimizeScope.FocusedApp;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MinimizeAffects MinimizeKeyAffects { get; set; } = MinimizeAffects.FocusedWindow;

        public char HideMinimizeKey { get; set; } = '-';
        public char MenuActionsKey { get; set; } = '=';

        // ===== Captured Keys =====
        public HashSet<char> DisabledKeys { get; set; } = new HashSet<char>();

        // ===== App Mappings =====
        // Maps process name to assigned letter (e.g., "DiscordPTB" -> 'P')
        public Dictionary<string, char> ProcessLetterMappings { get; set; } = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase);

        // Apps to exclude from switching
        public HashSet<string> ExcludedProcesses { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static AppSettings? _instance;
        public static AppSettings Instance => _instance ??= Load();

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _instance = settings;
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public char GetLetterForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return '?'; // Fallback for invalid process names

            if (ProcessLetterMappings.TryGetValue(processName, out char letter))
            {
                return letter;
            }
            // Default to first letter of process name
            return char.ToUpper(processName[0]);
        }

        public void SetLetterForProcess(string processName, char letter)
        {
            ProcessLetterMappings[processName] = char.ToUpper(letter);
            Save();
        }

        public void RemoveMapping(string processName)
        {
            if (ProcessLetterMappings.Remove(processName))
            {
                Save();
            }
        }

        public bool IsExcluded(string processName)
        {
            return ExcludedProcesses.Contains(processName);
        }

        public void SetExcluded(string processName, bool excluded)
        {
            if (excluded)
            {
                ExcludedProcesses.Add(processName);
            }
            else
            {
                ExcludedProcesses.Remove(processName);
            }
            Save();
        }

        // ===== Launch at Login =====
        private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "RcmdWindows";

        public void SetLaunchAtLogin(bool enabled)
        {
            LaunchAtLogin = enabled;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key != null)
                {
                    if (enabled)
                    {
                        // Use Environment.ProcessPath for .NET 6+ (most reliable for single-file apps)
                        string? exePath = Environment.ProcessPath;

                        // Fallback to process module path
                        if (string.IsNullOrEmpty(exePath))
                        {
                            exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        }

                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key.SetValue(AppName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting launch at login: {ex.Message}");
            }
            Save();
        }

        public bool GetLaunchAtLoginFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        // ===== Captured Keys =====
        public bool IsKeyCaptured(char key)
        {
            return !DisabledKeys.Contains(char.ToUpper(key));
        }

        public void SetKeyCaptured(char key, bool captured)
        {
            key = char.ToUpper(key);
            if (captured)
            {
                DisabledKeys.Remove(key);
            }
            else
            {
                DisabledKeys.Add(key);
            }
            Save();
        }
    }
}
