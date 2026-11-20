#ifndef PayloadExe
  #define PayloadExe "..\CodexKnob.exe"
#endif

[Setup]
AppId=CodexKnob
AppName=Codex Knob
AppVersion=1.2.0
AppPublisher=Codex Knob contributors
AppPublisherURL=https://github.com/Gabeujin/codex-knob
DefaultDirName={localappdata}\Programs\CodexKnob\1.2.0
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
AppMutex=Local\Ak47CodexKnob
CloseApplications=no
SetupIconFile=..\assets\knob.ico
UninstallDisplayIcon={app}\CodexKnob.exe
OutputBaseFilename=Codex-Knob-1.2.0-Setup-x64
VersionInfoVersion=1.2.0.0
WizardStyle=modern
Compression=lzma2
SolidCompression=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ko"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "{#PayloadExe}"; DestDir: "{app}"; DestName: "CodexKnob.exe"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Codex Knob"; Filename: "{app}\CodexKnob.exe"

[Run]
Filename: "{app}\CodexKnob.exe"; Description: "{cm:LaunchProgram,Codex Knob}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[CustomMessages]
en.FrameworkTitle=Microsoft .NET Framework 4.8
en.FrameworkDownload=Downloading the .NET Framework runtime from Microsoft.
en.FrameworkLaunchFailed=Windows could not start the .NET Framework installer. Allow the administrator prompt and try again.
en.FrameworkFailed=.NET Framework installation failed (code %1). Try again, or install .NET Framework 4.8 from Microsoft before running Setup.
en.FrameworkRestart=Restart Windows to finish installing .NET Framework, then run Codex Knob Setup again.
en.ExistingAppBackupFailed=Could not back up the existing Codex Knob executable. Close the app and check that its folder is accessible.
ko.FrameworkTitle=Microsoft .NET Framework 4.8
ko.FrameworkDownload=Microsoft에서 .NET Framework 런타임을 다운로드합니다.
ko.FrameworkLaunchFailed=.NET Framework 설치 프로그램을 시작하지 못했습니다. 관리자 권한 요청을 허용한 뒤 다시 시도하세요.
ko.FrameworkFailed=.NET Framework 설치에 실패했습니다(코드 %1). 다시 시도하거나 Microsoft에서 .NET Framework 4.8을 설치한 뒤 이 설치 프로그램을 실행하세요.
ko.FrameworkRestart=Windows를 다시 시작해 .NET Framework 설치를 마친 뒤 Codex Knob 설치 프로그램을 다시 실행하세요.
ko.ExistingAppBackupFailed=기존 Codex Knob 실행 파일을 백업하지 못했습니다. 앱을 닫고 폴더에 접근할 수 있는지 확인하세요.
ja.FrameworkTitle=Microsoft .NET Framework 4.8
ja.FrameworkDownload=Microsoft から .NET Framework ランタイムをダウンロードしています。
ja.FrameworkLaunchFailed=.NET Framework のインストーラーを起動できませんでした。管理者権限の確認を許可して、もう一度お試しください。
ja.FrameworkFailed=.NET Framework をインストールできませんでした (コード %1)。再試行するか、Microsoft から .NET Framework 4.8 をインストールしてからセットアップを実行してください。
ja.FrameworkRestart=Windows を再起動して .NET Framework のインストールを完了してから、Codex Knob のセットアップを再実行してください。
ja.ExistingAppBackupFailed=既存の Codex Knob 実行ファイルをバックアップできませんでした。アプリを閉じ、フォルダーにアクセスできることを確認してください。

[Code]
const
  FrameworkUrl = 'https://download.microsoft.com/download/f/3/a/f3a6af84-da23-40a5-8d1c-49cc10c8e76f/NDP48-x86-x64-AllOS-ENU.exe';
  FrameworkFile = 'NDP48-x86-x64-AllOS-ENU.exe';
  FrameworkSHA256 = '0a3a390c47e639d0f7fc65b21195fee6b7f65b066f80f70c60fab191d14b7e40';
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

var
  FrameworkDownloadPage: TDownloadWizardPage;

procedure InitializeWizard;
begin
  FrameworkDownloadPage := CreateDownloadPage(CustomMessage('FrameworkTitle'), CustomMessage('FrameworkDownload'), nil);
end;

function DownloadFramework: String;
begin
  Result := '';
  FrameworkDownloadPage.Clear;
  FrameworkDownloadPage.Add(FrameworkUrl, FrameworkFile, FrameworkSHA256);
  FrameworkDownloadPage.Show;
  try
    try
      FrameworkDownloadPage.Download;
    except
      Result := GetExceptionMessage;
    end;
  finally
    FrameworkDownloadPage.Hide;
  end;
end;

function PreserveExistingExecutable: String;
var
  ExistingFile, ExistingHash, BackupFolder, BackupFile: String;
begin
  Result := '';
  ExistingFile := ExpandConstant('{app}\CodexKnob.exe');
  if not FileExists(ExistingFile) then
    Exit;
  try
    ExistingHash := GetSHA256OfFile(ExistingFile);
    BackupFolder := ExpandConstant('{localappdata}\CodexKnob\installer-backups');
    BackupFile := AddBackslash(BackupFolder) + ExistingHash + '.exe';
    if not FileExists(BackupFile) then
      if not (ForceDirectories(BackupFolder) and FileCopy(ExistingFile, BackupFile, True)) then
      begin
        Result := CustomMessage('ExistingAppBackupFailed');
        Exit;
      end;
    if GetSHA256OfFile(BackupFile) <> ExistingHash then
      Result := CustomMessage('ExistingAppBackupFailed');
  except
    Result := CustomMessage('ExistingAppBackupFailed');
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Code: Integer;
begin
  Result := '';
  if IsDotNetInstalled(net48, 0) then
  begin
    Result := PreserveExistingExecutable;
    Exit;
  end;
  Result := DownloadFramework;
  if Result <> '' then
    Exit;
  if not ShellExec('runas', ExpandConstant('{tmp}\') + FrameworkFile, '/passive /norestart', '',
    SW_SHOWNORMAL, ewWaitUntilTerminated, Code) then
    Result := CustomMessage('FrameworkLaunchFailed')
  else if (Code = 1641) or (Code = 3010) then
  begin
    NeedsRestart := True;
    Result := CustomMessage('FrameworkRestart');
  end
  else if (Code <> 0) or not IsDotNetInstalled(net48, 0) then
    Result := FmtMessage(CustomMessage('FrameworkFailed'), [IntToStr(Code)])
  else
    Result := PreserveExistingExecutable;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  CurrentCommand: String;
begin
  if (CurUninstallStep = usUninstall) and
    RegQueryStringValue(HKCU, RunKey, 'CodexKnob', CurrentCommand) and
    (CurrentCommand = '"' + ExpandConstant('{app}\CodexKnob.exe') + '" --background') then
    RegDeleteValue(HKCU, RunKey, 'CodexKnob');
end;
