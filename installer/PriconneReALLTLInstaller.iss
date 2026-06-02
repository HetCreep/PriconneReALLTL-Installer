; Inno Setup script — PriconneReALLTL Installer
; -----------------------------------------------------------------------------
; PER-USER install (PrivilegesRequired=lowest): no admin prompt, installs under
; the user profile so the app's built-in self-update can overwrite the exe in
; place without elevation. The portable single-file exe stays the primary
; distribution; this installer is a convenience for first-time setup + a clean
; uninstall that also wipes the app's local cache/settings.
;
; Build locally (Inno Setup 6+):
;   ISCC.exe /DAppVersion=2.4.0 /DTag=v2.4.0 installer\PriconneReALLTLInstaller.iss
; The GitHub Actions release workflow builds this automatically on a v* tag.
; -----------------------------------------------------------------------------

#define AppName "PriconneReALLTL Installer"
#define AppPublisher "HetCreep"
#define AppExeName "PriconneReALLTLInstaller.exe"
#define AppURL "https://github.com/HetCreep/PriconneReALLTL-Installer"
#define SrcDir "..\PriconneReALLTLInstaller\bin\Release"

; Overridable on the ISCC command line. AppVersion must be numeric (Inno [Setup]);
; Tag is the release tag used only in the output filename (defaults to "v"+AppVersion).
#ifndef AppVersion
  #define AppVersion "2.4.0"
#endif
#ifndef Tag
  #define Tag "v" + AppVersion
#endif

[Setup]
; Stable AppId — distinct from PriconneMultiAccountLauncher's AppId. Do NOT change
; (changing it makes Windows treat upgrades as a separate product).
AppId={{45A4682D-8431-422B-BA7C-304E9C318EAA}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\PriconneReALLTLInstaller
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=PriconneReALLTLInstaller-{#Tag}-Setup
SetupIconFile={#SrcDir}\..\..\Resources\jewel.ico
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Single-file exe (Newtonsoft.Json is embedded) + its .NET config (must sit beside the exe).
Source: "{#SrcDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\{#AppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Wipe the app's per-user data (user.config, version cache, downloaded zip cache) on
; uninstall — this is the "CleanerTool" the portable build would otherwise need. Does NOT
; touch the game's BepInEx translation patch (that lives in the game folder and has its
; own in-app Uninstall), nor wrapped launcher shortcuts (restored via the in-app manager).
Type: filesandordirs; Name: "{localappdata}\PriconneReALLTLInstaller"
