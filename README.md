# rALT for Windows

A Windows app switcher inspired by macOS's rcmd. Switch to applications instantly using Right Alt (rALT) + letter shortcuts.

## Features

- **Instant app switching**: Hold Right Alt (rALT) and press the first letter of an app's process name
- **System tray app**: Runs quietly in the background
- **Low-level keyboard hook**: Captures Right Alt + letter combinations
- **Automatic window enumeration**: Finds running applications
- **Restore minimized windows**: Automatically restores windows before switching
- **Settings UI**: Configure behavior, captured keys, and app shortcuts
- **Overlay**: Shows available app letters while holding the modifier

## How It Works

1. Hold the **Right Alt (rALT)** key
2. Press a **letter** (a-z)
3. The app will switch to the first running application whose process name starts with that letter

### Examples

- Right Alt + **C** -> Chrome
- Right Alt + **V** -> Visual Studio Code (or VS)
- Right Alt + **N** -> Notepad
- Right Alt + **E** -> Excel or Explorer

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- Windows OS

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

The application will start in the system tray.

## Current Limitations (POC)

- Only matches by process name first letter by default
- No ability to launch apps that aren't running yet
- Console output for debugging (will be hidden in release builds)

## Next Steps

- [ ] Implement improved cycling between apps with the same first letter
- [ ] Add configuration file support (JSON)
- [ ] Launch apps that aren't running
- [ ] Window-level switching (not just apps)
- [ ] Auto-start with Windows

## Technical Details

- Uses Windows low-level keyboard hooks (`SetWindowsHookEx`)
- Right Alt virtual key code: `VK_RMENU (0xA5)`
- Window enumeration via `EnumWindows` API
- Window switching via `SetForegroundWindow` API
- Runs as a Windows Forms application for event loop and tray support
