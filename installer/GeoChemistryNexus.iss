#define MyAppName "GeoChemistry Nexus"
#define MyAppExeName "GeoChemistryNexus.exe"
#define MyAppPublisher "Maxwell Lei"
#define MyAppURL "https://github.com/MaxwellLei/GeoChemistry-Nexus"
; 发版时与 GeoChemistryNexus.csproj 中的 AssemblyVersion 保持一致
#define MyAppVersion "0.8.0"

#define PublishDir "..\src\_publish\win-x64"
#define MyAppOutputBase "GeoChemistryNexus-Setup-" + MyAppVersion + "-x64"
#define DotNetRedistFile "windowsdesktop-runtime-6.0.8-win-x64.exe"
; 自定义文件类型
#define ExtDiagramTemplate ".gndiag"
#define ExtGeothermometer ".gngtm"
#define ProgIdDiagramTemplate "GeoChemistryNexus.DiagramTemplate"
#define ProgIdGeothermometer "GeoChemistryNexus.Geothermometer"
#define IconDiagramTemplate "diagram-template.ico"
#define IconGeothermometer "geothermobarometer-template.ico"
#define FileTypeIconsDir "FileTypeIcons"

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
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "chinesetrad"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"

[CustomMessages]
english.AssocDiagramTemplate=GeoChemistry Nexus Diagram Template
english.AssocGeothermometer=GeoChemistry Nexus Geothermometer Package
chinesesimp.AssocDiagramTemplate=GeoChemistry Nexus 图解模板
chinesesimp.AssocGeothermometer=GeoChemistry Nexus 温压计模板
chinesetrad.AssocDiagramTemplate=GeoChemistry Nexus 圖解模板
chinesetrad.AssocGeothermometer=GeoChemistry Nexus 溫壓計模板
german.AssocDiagramTemplate=GeoChemistry Nexus Diagrammvorlage
german.AssocGeothermometer=GeoChemistry Nexus Geothermometer-Paket
french.AssocDiagramTemplate=Modèle de diagramme GeoChemistry Nexus
french.AssocGeothermometer=Package géothermobaromètre GeoChemistry Nexus
spanish.AssocDiagramTemplate=Plantilla de diagrama GeoChemistry Nexus
spanish.AssocGeothermometer=Paquete de geotermómetro GeoChemistry Nexus
russian.AssocDiagramTemplate=Шаблон диаграммы GeoChemistry Nexus
russian.AssocGeothermometer=Пакет геотермометра GeoChemistry Nexus
japanese.AssocDiagramTemplate=GeoChemistry Nexus ダイアグラムテンプレート
japanese.AssocGeothermometer=GeoChemistry Nexus 地質温度計パッケージ
korean.AssocDiagramTemplate=GeoChemistry Nexus 다이어그램 템플릿
korean.AssocGeothermometer=GeoChemistry Nexus 지온지압계 패키지

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; 路径相对于本 .iss 文件所在目录（installer\）
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Templates.db,Geothermometer.db,GraphMapList.json,GeoT-List.json,Logs\*,portable.flag"
Source: "{#FileTypeIconsDir}\{#IconDiagramTemplate}"; DestDir: "{app}\FileTypeIcons"; Flags: ignoreversion
Source: "{#FileTypeIconsDir}\{#IconGeothermometer}"; DestDir: "{app}\FileTypeIcons"; Flags: ignoreversion
Source: "redist\{#DotNetRedistFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet6DesktopInstalled

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; ---------- .gndiag 图解模板 ----------
Root: HKLM; Subkey: "Software\Classes\{#ExtDiagramTemplate}"; ValueType: string; ValueName: ""; ValueData: "{#ProgIdDiagramTemplate}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\{#ExtDiagramTemplate}"; ValueType: string; ValueName: "Content Type"; ValueData: "application/zip"
Root: HKLM; Subkey: "Software\Classes\{#ExtDiagramTemplate}"; ValueType: string; ValueName: "PerceivedType"; ValueData: "compressed"
Root: HKLM; Subkey: "Software\Classes\{#ExtDiagramTemplate}\OpenWithProgids"; ValueType: string; ValueName: "{#ProgIdDiagramTemplate}"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\Classes\{#ProgIdDiagramTemplate}"; ValueType: string; ValueName: ""; ValueData: "{cm:AssocDiagramTemplate}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\{#ProgIdDiagramTemplate}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\FileTypeIcons\{#IconDiagramTemplate},0"
Root: HKLM; Subkey: "Software\Classes\{#ProgIdDiagramTemplate}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; ---------- .gngtm 温压计模板 ----------
Root: HKLM; Subkey: "Software\Classes\{#ExtGeothermometer}"; ValueType: string; ValueName: ""; ValueData: "{#ProgIdGeothermometer}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\Classes\{#ExtGeothermometer}"; ValueType: string; ValueName: "Content Type"; ValueData: "application/zip"
Root: HKLM; Subkey: "Software\Classes\{#ExtGeothermometer}"; ValueType: string; ValueName: "PerceivedType"; ValueData: "compressed"
Root: HKLM; Subkey: "Software\Classes\{#ExtGeothermometer}\OpenWithProgids"; ValueType: string; ValueName: "{#ProgIdGeothermometer}"; ValueData: ""; Flags: uninsdeletevalue uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\Classes\{#ProgIdGeothermometer}"; ValueType: string; ValueName: ""; ValueData: "{cm:AssocGeothermometer}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\Classes\{#ProgIdGeothermometer}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\FileTypeIcons\{#IconGeothermometer},0"
Root: HKLM; Subkey: "Software\Classes\{#ProgIdGeothermometer}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

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
