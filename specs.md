# rcmd for Windows - Specifications

## Purpose
rcmd for Windows is a lightweight Windows tray application that lets you switch to running apps instantly using a modifier key plus a letter. It is inspired by macOS rcmd and targets fast, keyboard-driven app switching with minimal UI.

## Goals
- Switch to a running app by pressing the modifier key plus a letter.
- Provide simple, low-friction customization for app-to-letter mappings.
- Run quietly in the background as a tray application.
- Stay responsive and low-overhead (fast window enumeration, minimal UI work).

## Non-Goals
- Full window manager functionality.
- App launching for non-running apps (future enhancement).
- Multi-user or enterprise configuration management.

## User Experience
- Hold the configured modifier (default: Right Alt).
- Press a letter to switch to the first matching app.
- Optional cycling behavior when multiple windows map to the same letter.
- Optional hide/minimize behaviors via modifier combinations.

## Key Features
- Low-level keyboard hook for modifier + letter detection.
- Window enumeration to discover visible app windows.
- Window focus and minimize controls via Win32 APIs.
- Settings UI for custom letter mappings and exclusions.
- Overlay that previews available app letters when the modifier is held.

## Settings Storage
- Location: `%AppData%\RcmdWindows\settings.json`
- Stores custom app-letter mappings, excluded apps, and hotkey behavior flags.

## Architecture Overview
- `Program.cs`: App entry point (Windows Forms message loop).
- `TrayApplication.cs`: Coordinates keyboard hook, switching, overlay, and tray menu.
- `KeyboardHook.cs`: Low-level keyboard hook and hotkey parsing.
- `ApplicationManager.cs`: Enumerates and caches windows, applies letter mapping.
- `WindowSwitcher.cs`: Win32 focus/minimize operations and window cycling helpers.
- `SwitcherOverlay.cs`: Visual overlay showing available letters.
- `SettingsForm.cs`: Settings UI and app management dialog.
- `AppSettings.cs`: Settings model, persistence, and registry integration.

## Build and Run
- Target: .NET 8, Windows.
- Build: `dotnet build`
- Run: `dotnet run`
