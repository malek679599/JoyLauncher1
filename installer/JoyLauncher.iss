#define AppName "JoyLauncher"
#ifndef AppVersion
  #define AppVersion "0.1.0-preview.1"
#endif

[Setup]
AppId={{4E2E8D2B-4C9F-4A68-AF5B-8C1D2B8F40A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=JoyLauncher
AppPublisherURL=https://github.com/malek679599/JoyLauncher1
AppSupportURL=https://github.com/malek679599/JoyLauncher1/issues
AppUpdatesURL=https://github.com/malek679599/JoyLauncher1/releases
DefaultDirName={localappdata}\Programs\JoyLauncher
DefaultGroupName=JoyLauncher
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\artifacts\Release
OutputBaseFilename=JoyLauncher-Setup-{#AppVersion}-win-x64
UninstallDisplayIcon={app}\JoyLauncher.exe
SetupLogging=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
Uninstallable=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\artifacts\JoyLauncher\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\JoyLauncher"; Filename: "{app}\JoyLauncher.exe"
Name: "{autodesktop}\JoyLauncher"; Filename: "{app}\JoyLauncher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\JoyLauncher.exe"; Description: "Launch JoyLauncher"; Flags: postinstall nowait skipifsilent
