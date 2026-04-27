; Inno Setup Script for AutoPortal
; 版本：1.1.0
; 发布者：worable
; 许可协议：Attribution-NonCommercial-ShareAlike 4.0 International

#define MyAppName "AutoPortal"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "worable"
#define MyAppExeName "AutoPortal.exe"
#define MyAppURL "https://github.com/worable/AutoPortal"

[Setup]
; 基本设置
AppId={{87D9A1DB-52E8-4421-90DD-88716F64D8A9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion=1.1.0.0
VersionInfoCompany=worable

; 安装路径
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes

; 输出设置
OutputDir=.{#MyAppName}_Setup_{#MyAppVersion}.exe
OutputBaseFilename=AutoPortal_v{#MyAppVersion}_Setup
SetupIconFile=Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
LicenseFile=LICENSE

; 权限要求
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; 压缩设置
Compression=lzma2/max
SolidCompression=yes

; 其他设置
WizardStyle=modern
WizardResizable=no
AllowNoIcons=yes
DisableDirPage=no
DisableWelcomePage=no
DisableFinishedPage=no
DisableReadyPage=no
ShowLanguageDialog=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplifiedCustom.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; 主程序文件
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.png"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.xaml"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.pri"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\Resources\*"; DestDir: "{app}\Resources"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\Pages\*"; DestDir: "{app}\Pages"; Flags: ignoreversion

; 注意：不要包含以下文件
; Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*.exe"; DestDir: "{app}"; Flags: ignoreversion except: {#MyAppExeName}

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 安装后处理
  end;
end;

function GetUninstallString(): String;
begin
  Result := '{uninstallexe}';
end;
