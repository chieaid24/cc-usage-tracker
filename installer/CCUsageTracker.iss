#define MyAppName "CC Usage Tracker"
#ifndef MyAppVersion
#define MyAppVersion "0.1.2"
#endif
#define MyAppPublisher "CC Usage Tracker"
#define MyAppExeName "CCUsageTracker.exe"

[Setup]
AppId={{6E2A53E4-A174-43ED-B787-6A8EB2460F35}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/chieaid24/cc-usage-tracker
AppSupportURL=https://github.com/chieaid24/cc-usage-tracker/issues
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=CCUsageTracker-Setup-x64
SetupIconFile=..\assets\cc-usage-tracker-icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked
Name: "startup"; Description: "Start CC Usage Tracker with Windows"; GroupDescription: "Startup:"; Flags: checkedonce

[Files]
Source: "..\artifacts\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CCUsageTracker"; ValueData: """{app}\{#MyAppExeName}"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch CC Usage Tracker"; Flags: nowait postinstall skipifsilent; Tasks: startup
Filename: "{app}\{#MyAppExeName}"; Parameters: "--no-startup"; Description: "Launch CC Usage Tracker"; Flags: nowait postinstall skipifsilent; Tasks: not startup

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
