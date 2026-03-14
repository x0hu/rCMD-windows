# rALT for Windows

A lightweight Windows app switcher inspired by [rcmd](https://lowtechguys.com/rcmd/). Switch to any app faster insted of alt tabbing.

## Download (Recommended)

Most users should install from GitHub Releases.

1. Open the latest release.
2. Download `rALT.exe` (or the release zip if provided).
3. Run `rALT.exe`.
4. Optional: in Settings, enable launch at login.

## What rALT does

- Hold your app modifier key (default: Right Alt)
- Press a letter key
- Jump to a running app mapped to that letter

The app runs in the system tray and includes a settings UI for mappings and behavior.

## Build from source (Optional)

### Prerequisites

- Windows 10/11
- .NET 8 SDK

### Build

```powershell
dotnet build .\src\rALT\rALT.csproj
```

### Run locally

```powershell
dotnet run --project .\src\rALT\rALT.csproj
```

### Publish a distributable EXE

```powershell
.\scripts\publish-win-x64.ps1
```

Output:

```text
dist/win-x64/rALT.exe
```

## CI build artifact

GitHub Actions workflow: `.github/workflows/build-exe.yml`

- Builds `rALT.exe` on PRs and pushes to `main`
- Uploads artifact: `rALT-win-x64`

## Project layout

```
src/rALT/            # C# WinForms app project
scripts/             # helper scripts for publishing
```

## Notes

- This app currently switches among running apps; it does not launch apps that are closed.
- Settings are stored per-user in `%AppData%\rALT\settings.json`.
