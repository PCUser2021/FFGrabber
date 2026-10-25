#ifndef AppVersion
  #define AppVersion "0.5.8"
#endif
#ifndef AppArch
  #define AppArch "x64"
#endif
#ifndef AppFileVersion
  #define AppFileVersion "0.5.8.0"
#endif
#define AppName "FFGrabber"
#define AppPublisher "FFGrabber contributors"

[Setup]
AppId={{A14F9D45-65B9-4B27-8F99-2BF0B89BFA7A}
SetupIconFile=..\src\FFGrabber\Assets\Windows11.ico
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
VersionInfoDescription=FFGrabber graphical frontend for a user-imported FFmpeg
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
; FFGrabber intentionally installs no FFmpeg binaries. Users import their own ffmpeg.exe, ffprobe.exe and ffplay.exe.
Source: "..\artifacts\publish\{#AppArch}\*"; DestDir: "{app}"; Excludes: "ffmpeg.exe,ffprobe.exe,ffplay.exe,tools\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\FFGrabber"; Filename: "{app}\FFGrabber.exe"; IconFilename: "{app}\FFGrabber.exe"; IconIndex: 0
Name: "{autodesktop}\FFGrabber"; Filename: "{app}\FFGrabber.exe"; IconFilename: "{app}\FFGrabber.exe"; IconIndex: 0; Tasks: desktopicon


[Registry]
; Current-user Explorer verbs. File associations remain unchanged; FFGrabber opens only when one of these commands is chosen.
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberConvert"; ValueType: string; ValueName: ""; ValueData: "Convert with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberConvert"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberConvert\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-convert ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberPlay"; ValueType: string; ValueName: ""; ValueData: "Play with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberPlay"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberPlay\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-play ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberMediaInfo"; ValueType: string; ValueName: ""; ValueData: "Inspect with MediaInfo"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberMediaInfo"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberMediaInfo\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-mediainfo ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberRemux"; ValueType: string; ValueName: ""; ValueData: "Remux / stream copy with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberRemux"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberRemux\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-remux ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberExtractAudio"; ValueType: string; ValueName: ""; ValueData: "Extract audio stream with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberExtractAudio"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberExtractAudio\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-extract-audio ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberConvert"; ValueType: string; ValueName: ""; ValueData: "Convert with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberConvert"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberConvert\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-convert ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberPlay"; ValueType: string; ValueName: ""; ValueData: "Play with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberPlay"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberPlay\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-play ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberMediaInfo"; ValueType: string; ValueName: ""; ValueData: "Inspect with MediaInfo"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberMediaInfo"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberMediaInfo\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-mediainfo ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberRemux"; ValueType: string; ValueName: ""; ValueData: "Remux / stream copy with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberRemux"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberRemux\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-remux ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberExtractAudio"; ValueType: string; ValueName: ""; ValueData: "Extract audio stream with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberExtractAudio"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\audio\shell\FFGrabberExtractAudio\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-extract-audio ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberDemuxVideo"; ValueType: string; ValueName: ""; ValueData: "Extract video stream (copy) with FFGrabber"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberDemuxVideo"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\FFGrabber.exe"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\video\shell\FFGrabberDemuxVideo\command"; ValueType: string; ValueName: ""; ValueData: """{app}\FFGrabber.exe"" --context-demux ""%1"""; Flags: uninsdeletekey

[Run]
Filename: "{app}\FFGrabber.exe"; Description: "Launch FFGrabber"; Flags: postinstall nowait skipifsilent
