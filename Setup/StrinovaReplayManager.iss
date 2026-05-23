#define MyAppName "Strinova Replay Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AppPublisher"
#define MyAppExeName "StrinovaReplayManager.exe"
#define MyAppPublishDir "..\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#define WinAppRuntimeInstaller "windowsappruntimeinstall-x64.exe"

[Setup]
AppId={{C75BF7C6-85AF-4442-AD42-4A90F796DD24}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename={#MyAppName}-Setup
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "Redist\{#WinAppRuntimeInstaller}"; DestDir: "{tmp}"; Flags: deleteafterinstall dontcopy noencryption
Source: "{#MyAppPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Code]
function IsWinAppRuntimeInstalled: Boolean;
var
  FindRec: TFindRec;
  SearchPath: String;
begin
  SearchPath := ExpandConstant('{commonpf64}\WindowsApps\Microsoft.WindowsAppRuntime.2_*');
  Result := FindFirst(SearchPath, FindRec);
  if Result then
    FindClose(FindRec);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  InstallerPath: String;
begin
  Result := '';
  NeedsRestart := False;

  if IsWinAppRuntimeInstalled then
    Exit;

  WizardForm.StatusLabel.Caption := 'Installing Windows App Runtime...';
  WizardForm.ProgressGauge.Style := npbstMarquee;

  ExtractTemporaryFile('{#WinAppRuntimeInstaller}');
  InstallerPath := ExpandConstant('{tmp}\{#WinAppRuntimeInstaller}');

  if not Exec(InstallerPath, '--quiet', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'Failed to launch the Windows App Runtime installer.';
    Exit;
  end;

  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    Result := Format('Windows App Runtime installer failed with exit code %d.', [ResultCode]);
    Exit;
  end;

  if ResultCode = 3010 then
    NeedsRestart := True;

  if not IsWinAppRuntimeInstalled then
    Result := 'Windows App Runtime installation did not complete. Enable Developer Mode or sideloading and try again.';
end;

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked
