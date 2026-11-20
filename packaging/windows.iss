#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef PlayerDir
  #error PlayerDir is required
#endif

[Setup]
AppId={{0E518E4A-8D14-4901-82FE-D6B654316514}
AppName=Pico Park Reproduce
AppVersion={#AppVersion}
AppPublisher=xiaolizi0v0
AppPublisherURL=https://github.com/xiaolizi0v0/pico-park-reproduce
DefaultDirName={localappdata}\Programs\Pico Park Reproduce
DefaultGroupName=Pico Park Reproduce
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=PicoPark-Reproduce-Windows-x64-Setup
Compression=lzma2
UninstallDisplayIcon={app}\PicoPark.exe

[Files]
Source: "{#PlayerDir}\*"; DestDir: "{app}"; Excludes: "*_BurstDebugInformation_DoNotShip\*,*.pdb"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Pico Park Reproduce"; Filename: "{app}\PicoPark.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\PicoPark.exe"; WorkingDir: "{app}"; Description: "Launch Pico Park Reproduce"; Flags: nowait postinstall skipifsilent
