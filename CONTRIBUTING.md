# Contributing

CC Usage Tracker accepts focused bug fixes and features that preserve its local, tray-only design.

## Set up

Use Windows 10 or Windows 11 with the .NET 10 SDK and Google Chrome. Install Inno Setup 6 only when you need to build the installer.

Fork the repository, create a branch, and run:

```powershell
.\scripts\build.ps1
```

The command must finish with a clean build, passing tests, and a portable ZIP under `artifacts\`.

## Submit a change

Keep browser interaction limited to process launch and Win32 window management. Do not add scraping, cookie access, browser automation, telemetry, administrator requirements, or a cloud service.

Add tests for settings, layout, and other behavior that can run without Chrome or an interactive desktop. Describe any manual Windows checks in the pull request.

Use a concise Conventional Commit title such as `fix(layout): clamp narrow monitor bounds`.
