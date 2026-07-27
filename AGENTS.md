# Repository guidance

## Product

CC Usage Tracker is a C# .NET 10 Windows Forms tray utility. It launches two
Google Chrome app windows and manages them through Win32. It never scrapes or
reads browser account data.

## Architecture

- `App/` owns application lifetime and single-instance signaling.
- `Browser/` locates Chrome, launches URLs, and detects new top-level windows.
- `Configuration/` validates and atomically persists local settings.
- `Hotkeys/` owns the registered toggle and scoped Escape hook.
- `Native/` contains Win32 interop declarations and constants.
- `Startup/` owns the current-user Run registry entry.
- `UsageWindows/` tracks, styles, places, refreshes, and closes popup windows.
- `UI/` contains only settings and about dialogs.

Keep native, browser, and settings concerns out of `TrayApplicationContext`.
Keep comments short and only explain non-obvious Win32 constraints.

## Verification

Run on Windows:

```powershell
.\scripts\build.ps1
```

Before a release, install Inno Setup 6 and run:

```powershell
.\scripts\release-local.ps1 -InnoSetupPath "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
```

Do not claim interactive Chrome behavior is verified without rerunning the
Windows hotkey, placement, window-style, Escape, and close checks.

## Security constraints

Do not add DOM access, browser automation, cookie or profile access, telemetry,
administrator privileges, command-shell construction from settings, or a
network backend. Accept only absolute HTTPS usage URLs.
