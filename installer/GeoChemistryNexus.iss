#define MyAppName "GeoChemistry Nexus"
#define MyAppExeName "GeoChemistryNexus.exe"
#define MyAppPublisher "Maxwell Lei"
#define MyAppURL "https://github.com/MaxwellLei/GeoChemistry-Nexus"
; 发版时与 GeoChemistryNexus.csproj 中的 AssemblyVersion 保持一致
#define MyAppVersion "0.8.2"

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
english.UninstallOptionsTitle=Uninstall Options
english.UninstallOptionsPrompt=You are about to uninstall GeoChemistry Nexus.%n%nYou may also remove local user data stored in your profile (settings, template databases, logs, etc.). This cannot be undone.
english.DeleteUserData=Also delete local user data
english.UninstallContinue=Continue
english.UninstallCancel=Cancel
chinesesimp.AssocDiagramTemplate=GeoChemistry Nexus 图解模板
chinesesimp.AssocGeothermometer=GeoChemistry Nexus 温压计模板
chinesesimp.UninstallOptionsTitle=卸载选项
chinesesimp.UninstallOptionsPrompt=即将卸载 GeoChemistry Nexus。%n%n可选择同时删除用户目录下的本地数据（配置、模板库、日志等）。此操作不可恢复。
chinesesimp.DeleteUserData=同时删除本地用户数据
chinesesimp.UninstallContinue=继续
chinesesimp.UninstallCancel=取消
chinesetrad.AssocDiagramTemplate=GeoChemistry Nexus 圖解模板
chinesetrad.AssocGeothermometer=GeoChemistry Nexus 溫壓計模板
chinesetrad.UninstallOptionsTitle=解除安裝選項
chinesetrad.UninstallOptionsPrompt=即將解除安裝 GeoChemistry Nexus。%n%n可選擇同時刪除使用者目錄下的本機資料（設定、範本庫、記錄等）。此操作無法復原。
chinesetrad.DeleteUserData=同時刪除本機使用者資料
chinesetrad.UninstallContinue=繼續
chinesetrad.UninstallCancel=取消
german.AssocDiagramTemplate=GeoChemistry Nexus Diagrammvorlage
german.AssocGeothermometer=GeoChemistry Nexus Geothermometer-Paket
german.UninstallOptionsTitle=Deinstallationsoptionen
german.UninstallOptionsPrompt=Sie sind dabei, GeoChemistry Nexus zu deinstallieren.%n%nOptional können lokale Benutzerdaten im Profil gelöscht werden (Einstellungen, Vorlagendatenbanken, Protokolle usw.). Dies kann nicht rückgängig gemacht werden.
german.DeleteUserData=Lokale Benutzerdaten ebenfalls löschen
german.UninstallContinue=Weiter
german.UninstallCancel=Abbrechen
french.AssocDiagramTemplate=Modèle de diagramme GeoChemistry Nexus
french.AssocGeothermometer=Package géothermobaromètre GeoChemistry Nexus
french.UninstallOptionsTitle=Options de désinstallation
french.UninstallOptionsPrompt=Vous êtes sur le point de désinstaller GeoChemistry Nexus.%n%nVous pouvez également supprimer les données utilisateur locales du profil (paramètres, bases de modèles, journaux, etc.). Cette action est irréversible.
french.DeleteUserData=Supprimer aussi les données utilisateur locales
french.UninstallContinue=Continuer
french.UninstallCancel=Annuler
spanish.AssocDiagramTemplate=Plantilla de diagrama GeoChemistry Nexus
spanish.AssocGeothermometer=Paquete de geotermómetro GeoChemistry Nexus
spanish.UninstallOptionsTitle=Opciones de desinstalación
spanish.UninstallOptionsPrompt=Está a punto de desinstalar GeoChemistry Nexus.%n%nTambién puede eliminar los datos locales del usuario en su perfil (configuración, bases de plantillas, registros, etc.). Esta acción no se puede deshacer.
spanish.DeleteUserData=Eliminar también los datos locales del usuario
spanish.UninstallContinue=Continuar
spanish.UninstallCancel=Cancelar
russian.AssocDiagramTemplate=Шаблон диаграммы GeoChemistry Nexus
russian.AssocGeothermometer=Пакет геотермометра GeoChemistry Nexus
russian.UninstallOptionsTitle=Параметры удаления
russian.UninstallOptionsPrompt=Вы собираетесь удалить GeoChemistry Nexus.%n%nПри желании можно также удалить локальные данные пользователя в профиле (настройки, базы шаблонов, журналы и т. д.). Это действие необратимо.
russian.DeleteUserData=Также удалить локальные данные пользователя
russian.UninstallContinue=Продолжить
russian.UninstallCancel=Отмена
japanese.AssocDiagramTemplate=GeoChemistry Nexus ダイアグラムテンプレート
japanese.AssocGeothermometer=GeoChemistry Nexus 地質温度計パッケージ
japanese.UninstallOptionsTitle=アンインストールオプション
japanese.UninstallOptionsPrompt=GeoChemistry Nexus をアンインストールしようとしています。%n%n必要に応じて、プロファイル内のローカルユーザーデータ（設定、テンプレートデータベース、ログなど）も削除できます。この操作は元に戻せません。
japanese.DeleteUserData=ローカルユーザーデータも削除する
japanese.UninstallContinue=続行
japanese.UninstallCancel=キャンセル
korean.AssocDiagramTemplate=GeoChemistry Nexus 다이어그램 템플릿
korean.AssocGeothermometer=GeoChemistry Nexus 지온지압계 패키지
korean.UninstallOptionsTitle=제거 옵션
korean.UninstallOptionsPrompt=GeoChemistry Nexus를 제거하려고 합니다.%n%n선택적으로 프로필의 로컬 사용자 데이터(설정, 템플릿 데이터베이스, 로그 등)도 삭제할 수 있습니다. 이 작업은 되돌릴 수 없습니다.
korean.DeleteUserData=로컬 사용자 데이터도 삭제
korean.UninstallContinue=계속
korean.UninstallCancel=취소

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
var
  DeleteUserData: Boolean;

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

function CmdLineParamExists(const Param: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
  begin
    if CompareText(ParamStr(I), Param) = 0 then
    begin
      Result := True;
      Exit;
    end;
  end;
end;

function ShowUninstallOptionsDialog: Boolean;
var
  Form: TSetupForm;
  InfoLabel: TNewStaticText;
  PathLabel: TNewStaticText;
  CheckBox: TNewCheckBox;
  OKButton: TNewButton;
  CancelButton: TNewButton;
begin
  { Inno Setup 6.6+：宽高须在创建时传入，之后为只读 }
  Form := CreateCustomForm(ScaleX(480), ScaleY(230), False, False);
  try
    Form.Caption := ExpandConstant('{cm:UninstallOptionsTitle}');
    Form.Position := poScreenCenter;

    InfoLabel := TNewStaticText.Create(Form);
    InfoLabel.Parent := Form;
    InfoLabel.Left := ScaleX(20);
    InfoLabel.Top := ScaleY(16);
    InfoLabel.Width := Form.ClientWidth - ScaleX(40);
    InfoLabel.Height := ScaleY(72);
    InfoLabel.AutoSize := False;
    InfoLabel.WordWrap := True;
    InfoLabel.Caption := ExpandConstant('{cm:UninstallOptionsPrompt}');

    CheckBox := TNewCheckBox.Create(Form);
    CheckBox.Parent := Form;
    CheckBox.Left := ScaleX(20);
    CheckBox.Top := ScaleY(100);
    CheckBox.Width := Form.ClientWidth - ScaleX(40);
    CheckBox.Height := ScaleY(20);
    CheckBox.Caption := ExpandConstant('{cm:DeleteUserData}');
    CheckBox.Checked := False;

    PathLabel := TNewStaticText.Create(Form);
    PathLabel.Parent := Form;
    PathLabel.Left := ScaleX(40);
    PathLabel.Top := ScaleY(126);
    PathLabel.Width := Form.ClientWidth - ScaleX(60);
    PathLabel.Height := ScaleY(18);
    PathLabel.AutoSize := False;
    PathLabel.Caption := ExpandConstant('{localappdata}\GeoChemistryNexus');
    PathLabel.Font.Color := clGrayText;

    OKButton := TNewButton.Create(Form);
    OKButton.Parent := Form;
    OKButton.Width := ScaleX(100);
    OKButton.Height := ScaleY(25);
    OKButton.Left := Form.ClientWidth - ScaleX(220);
    OKButton.Top := Form.ClientHeight - ScaleY(44);
    OKButton.Caption := ExpandConstant('{cm:UninstallContinue}');
    OKButton.Default := True;
    OKButton.ModalResult := mrOk;

    CancelButton := TNewButton.Create(Form);
    CancelButton.Parent := Form;
    CancelButton.Width := ScaleX(100);
    CancelButton.Height := ScaleY(25);
    CancelButton.Left := Form.ClientWidth - ScaleX(110);
    CancelButton.Top := Form.ClientHeight - ScaleY(44);
    CancelButton.Caption := ExpandConstant('{cm:UninstallCancel}');
    CancelButton.Cancel := True;
    CancelButton.ModalResult := mrCancel;

    Form.ActiveControl := OKButton;

    Result := Form.ShowModal = mrOk;
    if Result then
      DeleteUserData := CheckBox.Checked;
  finally
    Form.Free;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  DeleteUserData := False;

  if UninstallSilent then
  begin
    { 静默卸载默认保留本地数据；需要删除时传入 /DELETEUSERDATA }
    DeleteUserData := CmdLineParamExists('/DELETEUSERDATA');
    Result := True;
  end
  else
    Result := ShowUninstallOptionsDialog;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UserDataDir: String;
begin
  if (CurUninstallStep = usPostUninstall) and DeleteUserData then
  begin
    UserDataDir := ExpandConstant('{localappdata}\GeoChemistryNexus');
    if DirExists(UserDataDir) then
      DelTree(UserDataDir, True, True, True);
  end;
end;
