<p align="center">
  <img src="assets/cc-usage-tracker-icon.png" alt="CC Usage Tracker icon" width="160">
</p>

# CC Usage Tracker

CC Usage Tracker opens your Claude and Codex usage pages side by side from the Windows system tray.

Use it on Windows 10 or Windows 11 with Google Chrome. The app manages two Chrome app windows; it does not scrape usage data or replace the provider websites.

## Download

[Download the latest installer](https://github.com/chieaid24/cc-usage-tracker/releases/latest)

Download `CCUsageTracker-Setup-x64.exe`, run it, then press `Ctrl+Alt+U`. The installer adds CC Usage Tracker to the Start menu and starts it in the system tray. It installs for your Windows account without administrator privileges.

Windows may show a Microsoft Defender SmartScreen warning because community releases are not code-signed. Check `SHA256SUMS.txt` on the release before running a download.

### Portable build

Download `CCUsageTracker-win-x64.zip`, extract it, and run `CCUsageTracker.exe`. The portable build enables Start with Windows on first launch. Clear that option from the tray menu or Settings if you do not want it.

## Use

| Action | Shortcut |
| --- | --- |
| Open or close both usage windows | `Ctrl+Alt+U` |
| Close both while one is focused | `Esc` |
| Toggle from the tray | Double-click the tray icon |

Claude opens on the left and Codex opens on the right. CC Usage Tracker places both windows on the monitor under your mouse by default and removes them from the taskbar and Alt+Tab.

The tray menu provides these commands:

- **Show Usage** opens both pages.
- **Close Usage** closes the tracked Chrome windows.
- **Refresh Usage** closes and reopens both pages.
- **Open Full Pages** opens both URLs as normal tabs in your default browser.
- **Settings** changes URLs, Chrome location, monitor selection, layout, startup, and always-on-top behavior.
- **Start with Windows** toggles current-user startup.
- **View Logs** opens the local log directory.
- **About** shows version and project information.
- **Exit** closes tracked windows and removes the tray icon.

## Sign in

CC Usage Tracker uses Chrome's normal website sessions. You may need to sign in to Claude or ChatGPT inside each popup the first time you open it. The app does not read the session.

## Settings and data

The default usage pages are:

```text
Claude: https://claude.ai/settings/usage
Codex:  https://chatgpt.com/codex/settings/usage
```

If a provider changes its usage page, open Settings, replace the affected URL with the new absolute HTTPS URL, and save.

Settings are stored at:

```text
%LocalAppData%\CCUsageTracker\settings.json
```

Logs are stored at:

```text
%LocalAppData%\CCUsageTracker\logs\
```

Logs rotate across three files with a 1 MB limit per file. Launch `CCUsageTracker.exe --debug` for additional diagnostic entries.

## Privacy

CC Usage Tracker launches and manages Chrome windows. It does not:

- Scrape page contents.
- Read cookies, tokens, browser history, or account data.
- Require API keys.
- Send telemetry.
- Run a local server or cloud backend.

The configured public usage URLs can appear in local logs. Do not put credentials or private tokens in those URLs.

## Troubleshooting

### Chrome was not found

Install Google Chrome or open Settings and select `chrome.exe`. The app checks normal per-user and machine-wide Chrome installation paths.

### A usage URL stopped working

Find the provider's current usage page, then update its URL in Settings. Only absolute `https://` URLs are accepted.

### Ctrl+Alt+U does not work

Another application may already own the shortcut. CC Usage Tracker shows a tray notification and records the error in the logs. Close or reconfigure the conflicting application, then restart CC Usage Tracker.

### A popup remains in the taskbar or Alt+Tab

Exit Chrome and CC Usage Tracker, then start both normally. Windows blocks one process from changing another process at a higher privilege level. Do not run Chrome as administrator.

### Windows blocks the installer

The release is unsigned and may trigger SmartScreen. Download it only from this repository, compare its SHA-256 hash with `SHA256SUMS.txt`, and use the portable ZIP if your policy blocks installers.

## Uninstall

Open Windows Settings, select Apps, find CC Usage Tracker, and choose Uninstall. The uninstaller removes the application and current-user startup entry.

User settings and logs remain under `%LocalAppData%\CCUsageTracker`. Delete that directory manually if you also want to remove local preferences and diagnostics.

## Develop

Install the .NET 10 SDK on Windows, then run:

```powershell
.\scripts\build.ps1
```

The script restores packages, builds Release, runs tests, publishes a self-contained single-file `win-x64` executable, and creates `artifacts\CCUsageTracker-win-x64.zip`.

Install Inno Setup 6, then build all local release assets:

```powershell
.\scripts\release-local.ps1 -InnoSetupPath "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
```

Push a semantic version tag such as `v0.1.0` to run the release workflow. The workflow tests the project, creates the installer and portable ZIP, generates SHA-256 checksums, and publishes a GitHub Release.

## Manual verification

Verify these behaviors on a Windows x64 desktop before publishing a release:

- [ ] First launch shows one tray notification.
- [ ] No normal window or taskbar button appears while idle.
- [ ] Start with Windows launches the app after sign-in.
- [ ] `Ctrl+Alt+U` opens exactly two Chrome app windows.
- [ ] Claude appears on the left and Codex appears on the right.
- [ ] Both windows stay inside the selected monitor's working area.
- [ ] Multiple monitors and negative coordinates place correctly.
- [ ] Neither popup appears in the taskbar or Alt+Tab.
- [ ] A second `Ctrl+Alt+U` closes both popups.
- [ ] `Esc` closes both only while a tracked popup is focused.
- [ ] Chrome already running does not prevent window capture.
- [ ] Chrome closed does not prevent launch.
- [ ] Manually closing one popup leaves the other manageable.
- [ ] Exit closes tracked windows and immediately removes the tray icon.
- [ ] Start with Windows can be disabled and enabled again.
- [ ] Uninstall removes the app and startup entry.

## License and trademarks

The code is available under the [MIT License](LICENSE).

CC Usage Tracker is an unofficial utility. It is not affiliated with, endorsed by, or sponsored by Anthropic or OpenAI. Claude is a trademark of Anthropic. OpenAI, ChatGPT, and Codex are trademarks of OpenAI.
