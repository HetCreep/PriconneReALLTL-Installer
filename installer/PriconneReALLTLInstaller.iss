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
  #define AppVersion "3.1.0"
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
; Apps & Features: show just the product name (version lives in its own column) + embed
; version metadata into the setup exe so AV/SmartScreen and the file properties read clean.
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\PriconneReALLTLInstaller
; Let power users change the install location; suppress the re-install "folder exists" warning.
DisableDirPage=auto
DirExistsWarning=no
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; #2: show the MIT license + Acknowledgments (credits + ToS/ban disclaimer) during install.
; LICENSE.txt at the repo root stays PURE MIT for GitHub license auto-detect; this install-time
; page (installer\LicensePage.txt) carries the extra credits, so the two never conflict.
LicenseFile=LicensePage.txt
; The thing being installed is itself the installer app — if it's running, close it so the
; copy doesn't fail; we relaunch via [Run], so don't let the Restart Manager double-launch it.
CloseApplications=force
RestartApplications=no
; #38: a named mutex (also created by the app at startup — see Program.cs) lets Setup AND the
; uninstaller detect a running instance and close/prompt it first, instead of failing on the locked
; exe ("Some elements could not be removed"). Must match the app's mutex name exactly.
AppMutex=PriconneReALLTLInstaller
OutputDir=..\dist
OutputBaseFilename=PriconneReALLTLInstaller-{#Tag}-Setup
SetupIconFile={#SrcDir}\..\..\Resources\jewel.ico
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

[UninstallRun]
; #52/#8: before the exe is removed, un-wrap every managed launcher shortcut so wrapped .lnks revert
; to plain game-launchers (or our Desktop copies are deleted) instead of pointing at a removed
; installer exe. [UninstallRun] runs before files are deleted, so the exe still exists; runs hidden
; and a failure is non-fatal to the uninstall.
Filename: "{app}\{#AppExeName}"; Parameters: "--unwrap-all"; Flags: runhidden skipifdoesntexist; RunOnceId: "UnwrapShortcuts"

[UninstallDelete]
; Wipe the app's per-user data (user.config, version cache, downloaded zip cache) on
; uninstall — this is the "CleanerTool" the portable build would otherwise need. Does NOT
; touch the game's BepInEx translation patch (that lives in the game folder and has its
; own in-app Uninstall), nor wrapped launcher shortcuts (restored via the in-app manager).
Type: filesandordirs; Name: "{localappdata}\PriconneReALLTLInstaller"
; #39: sweep any stale .log a pre-#6 version left in the install dir (current builds log to
; %LOCALAPPDATA%; old v3.0.0 install-dir logs linger in {app} and would survive otherwise).
Type: files; Name: "{app}\*.log"
