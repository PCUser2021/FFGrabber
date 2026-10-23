#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
#ifndef AppArch
  #define AppArch "x64"
#endif
#ifndef AppFileVersion
  #define AppFileVersion "0.2.0.0"
#endif
#define AppName "FFGrabber"
#define AppPublisher "FFGrabber contributors"

[Setup]
AppId={{A14F9D45-65B9-4B27-8F99-2BF0B89BFA7A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/PCUser2021/FFGrabber
AppSupportURL=https://github.com/PCUser2021/FFGrabber/issues
AppUpdatesURL=https://github.com/PCUser2021/FFGrabber/releases
DefaultDirName={localappdata}\Programs\FFGrabber
DefaultGroupName=FFGrabber
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\FFGrabber.exe
OutputDir=..\artifacts\installer
OutputBaseFilename=FFGrabber-Setup-{#AppArch}-v{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
Uninstallable=yes
SetupLogging=yes
VersionInfoVersion={#AppFileVersion}
VersionInfoProductName=FFGrabber
VersionInfoDescription=FFGrabber Windows media workstation
CloseApplications=yes
RestartApplications=no
#if AppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible and not arm64
ArchitecturesInstallIn64BitMode=x64compatible and not arm64
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\{#AppArch}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\FFGrabber"; Filename: "{app}\FFGrabber.exe"
Name: "{autodesktop}\FFGrabber"; Filename: "{app}\FFGrabber.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FFGrabber.exe"; Description: "Launch FFGrabber"; Flags: postinstall nowait skipifsilent
