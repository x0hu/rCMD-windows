using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RcmdWindows
{
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // Virtual key codes - Modifier keys
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4;    // Left Alt
        private const int VK_RMENU = 0xA5;    // Right Alt
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        // OEM key codes for special characters
        private const int VK_OEM_1 = 0xBA;      // ;:
        private const int VK_OEM_PLUS = 0xBB;   // =+
        private const int VK_OEM_COMMA = 0xBC;  // ,<
        private const int VK_OEM_MINUS = 0xBD;  // -_
        private const int VK_OEM_PERIOD = 0xBE; // .>
        private const int VK_OEM_2 = 0xBF;      // /?
        private const int VK_OEM_3 = 0xC0;      // `~
        private const int VK_OEM_4 = 0xDB;      // [{
        private const int VK_OEM_5 = 0xDC;      // \|
        private const int VK_OEM_6 = 0xDD;      // ]}
        private const int VK_OEM_7 = 0xDE;      // '"

        // KBDLLHOOKSTRUCT flags
        private const uint LLKHF_ALTDOWN = 0x20;
        private const uint LLKHF_EXTENDED = 0x01;

        private IntPtr hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? proc;
        private bool ignoreNextLControl = false;

        // Track modifier key states
        private bool appsModifierPressed = false;
        private bool leftShiftPressed = false;
        private bool rightShiftPressed = false;
        private bool leftAltPressed = false;

        public event Action<char, bool, bool, bool>? OnKeyPressed;  // letter, withLeftShift, withRightShift, withAlt
        public event Action? OnModifierPressed;
        public event Action? OnModifierReleased;
        public event Action? OnMinimizePressed;
        public event Action? OnSettingsPressed;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public void Install()
        {
            proc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                // Handle AltGr: Windows sends a fake LControl before RMenu on international layouts
                if (vkCode == VK_LCONTROL && ignoreNextLControl)
                {
                    ignoreNextLControl = false;
                    return CallNextHookEx(hookId, nCode, wParam, lParam);
                }

                // Track additional modifier key states (for hotkey combinations)
                if (vkCode == VK_LSHIFT)
                {
                    leftShiftPressed = isKeyDown;
                }
                else if (vkCode == VK_RSHIFT)
                {
                    rightShiftPressed = isKeyDown;
                }
                else if (vkCode == VK_LMENU)
                {
                    leftAltPressed = isKeyDown;
                }

                // Get the configured apps modifier key
                int appsModifierVk = GetVkCodeForModifierKey(AppSettings.Instance.AppsModifierKey);

                // Track the apps modifier key state
                if (vkCode == appsModifierVk)
                {
                    if (isKeyDown && !appsModifierPressed)
                    {
                        appsModifierPressed = true;
                        // Handle AltGr fake LControl
                        if (vkCode == VK_RMENU)
                            ignoreNextLControl = true;
                        OnModifierPressed?.Invoke();
                    }
                    else if (isKeyUp && appsModifierPressed)
                    {
                        appsModifierPressed = false;
                        ignoreNextLControl = false;
                        OnModifierReleased?.Invoke();
                    }
                }
                // If apps modifier is held and another key is pressed
                else if (appsModifierPressed && isKeyDown)
                {
                    // Check if it's a letter (A-Z)
                    if (vkCode >= 0x41 && vkCode <= 0x5A)
                    {
                        char letter = (char)vkCode;

                        // Check if this key is captured (not disabled) in settings
                        if (!AppSettings.Instance.IsKeyCaptured(letter))
                        {
                            return CallNextHookEx(hookId, nCode, wParam, lParam);
                        }

                        OnKeyPressed?.Invoke(char.ToLower(letter), leftShiftPressed, rightShiftPressed, leftAltPressed);
                        return (IntPtr)1;
                    }
                    // Check if it's the hide/minimize key
                    else if (vkCode == GetVkCodeForChar(AppSettings.Instance.HideMinimizeKey))
                    {
                        OnMinimizePressed?.Invoke();
                        return (IntPtr)1;
                    }
                    // Check if it's the menu/settings key
                    else if (vkCode == GetVkCodeForChar(AppSettings.Instance.MenuActionsKey))
                    {
                        OnSettingsPressed?.Invoke();
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// Gets the virtual key code for a ModifierKey setting.
        /// </summary>
        private static int GetVkCodeForModifierKey(ModifierKey key)
        {
            return key switch
            {
                ModifierKey.Shift => VK_LSHIFT,
                ModifierKey.Ctrl => VK_LCONTROL,
                ModifierKey.Alt => VK_LMENU,
                ModifierKey.RightAlt => VK_RMENU,
                ModifierKey.Win => VK_LWIN,
                _ => VK_RMENU  // Default to Right Alt
            };
        }

        public void Dispose()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the virtual key code for a given character.
        /// </summary>
        private static int GetVkCodeForChar(char c)
        {
            // Numbers 0-9
            if (c >= '0' && c <= '9')
                return c; // VK codes for 0-9 are same as ASCII

            // Letters A-Z
            if (c >= 'A' && c <= 'Z')
                return c;
            if (c >= 'a' && c <= 'z')
                return char.ToUpper(c);

            // Special characters
            return c switch
            {
                ';' or ':' => VK_OEM_1,
                '=' or '+' => VK_OEM_PLUS,
                ',' or '<' => VK_OEM_COMMA,
                '-' or '_' => VK_OEM_MINUS,
                '.' or '>' => VK_OEM_PERIOD,
                '/' or '?' => VK_OEM_2,
                '`' or '~' => VK_OEM_3,
                '[' or '{' => VK_OEM_4,
                '\\' or '|' => VK_OEM_5,
                ']' or '}' => VK_OEM_6,
                '\'' or '"' => VK_OEM_7,
                _ => 0
            };
        }
    }
}
