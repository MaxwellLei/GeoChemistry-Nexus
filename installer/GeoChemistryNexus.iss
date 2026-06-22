#define MyAppName "GeoChemistry Nexus"
#define MyAppExeName "GeoChemistryNexus.exe"
#define MyAppPublisher "Maxwell Lei"
#define MyAppURL "https://github.com/MaxwellLei/GeoChemistry-Nexus"
; 发版时与 GeoChemistryNexus.csproj 中的 AssemblyVersion 保持一致
#define MyAppVersion "0.7.0"

#define PublishDir "..\src\_publish\win-x64"
#define MyAppOutputBase "GeoChemistryNexus-Setup-" + MyAppVersion + "-x64"
#define DotNetRedistFile "windowsdesktop-runtime-6.0.8-win-x64.exe"

#ifexist "..\src\_publish\win-x64\GeoChemistryNexus.exe"
#else
  #error 未找到发布输出。请先在 VS 中发布到 src\_publish\win-x64，确认存在 GeoChemistryNexus.exe 后再编译安装包。
#endif

[Setup]
AppId={{A3F8C2E1-9B4D-4F6A-8C1E-2D5E7F9A0B3C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases/latest
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=dist
OutputBaseFilename={#MyAppOutputBase}
SetupIconFile=logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UsePreviousAppDir=yes
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 路径相对于本 .iss 文件所在目录（installer\）
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Templates.db,Geothermometer.db,GraphMapList.json,GeoT-List.json,Logs\*,portable.flag"
Source: "redist\{#DotNetRedistFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet6DesktopInstalled

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\{#DotNetRedistFile}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 6 Desktop Runtime..."; Check: not IsDotNet6DesktopInstalled; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet6DesktopInstalled: Boolean;
var
  SubKeyNames: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\dotnet\Setup\Installed Versions\sharedfx\Microsoft.WindowsDesktop.App', SubKeyNames) then
  begin
    for I := 0 to GetArrayLength(SubKeyNames) - 1 do
    begin
      if Copy(SubKeyNames[I], 1, 2) = '6.' then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;
