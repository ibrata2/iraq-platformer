#ifndef Version
  #define Version "0.0.0-dev"
#endif
[Setup]
AppId=IraqPlatformer
AppName=Iraq Platformer
AppVersion={#Version}
AppPublisher=Ibrahim Al-Khazraji
AppPublisherURL=https://t.me/ibraprogram
AppSupportURL=https://t.me/ibraprogram
DefaultDirName={localappdata}\Programs\Iraq Platformer
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=dist
OutputBaseFilename=IraqPlatformer-Setup
SetupIconFile=assets\hamoudi.ico
UninstallDisplayIcon={app}\IraqPlatformer.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "dist\publish\IraqPlatformer.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Iraq Platformer"; Filename: "{app}\IraqPlatformer.exe"

[Run]
Filename: "{app}\IraqPlatformer.exe"; Description: "Launch Iraq Platformer"; Flags: nowait postinstall skipifsilent
