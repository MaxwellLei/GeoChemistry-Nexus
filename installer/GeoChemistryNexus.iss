#define MyAppName "GeoChemistry Nexus"
#define MyAppExeName "GeoChemistryNexus.exe"
#define MyAppPublisher "Maxwell Lei"
#define MyAppURL "https://github.com/MaxwellLei/GeoChemistry-Nexus"
; 发版时与 GeoChemistryNexus.csproj 中的 AssemblyVersion 保持一致
#define MyAppVersion "0.9.0"

#define PublishDir "..\src\_publish\win-x64"
#define MyAppOutputBase "GeoChemistryNexus-Setup-" + MyAppVersion + "-x64"
; 与 GeoChemistryNexus.csproj 的 TargetFramework（net10.0-windows）保持一致；升级运行时补丁版本时只需改这里和 redist 中的文件
#define DotNetRedistFile "windowsdesktop-runtime-10.0.15-win-x64.exe"

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
; 升级会删掉旧的 .gndiag / .gngtm 关联，装完后通知资源管理器刷新
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
english.UninstallOptionsTitle=Uninstall Options
english.UninstallOptionsPrompt=You are about to uninstall GeoChemistry Nexus.%n%nYou may also remove local user data stored in your profile (settings, template databases, logs, etc.). This cannot be undone.
english.DeleteUserData=Also delete local user data
english.UninstallContinue=Continue
english.UninstallCancel=Cancel
chinesesimp.UninstallOptionsTitle=卸载选项
chinesesimp.UninstallOptionsPrompt=即将卸载 GeoChemistry Nexus。%n%n可选择同时删除用户目录下的本地数据（配置、模板库、日志等）。此操作不可恢复。
chinesesimp.DeleteUserData=同时删除本地用户数据
chinesesimp.UninstallContinue=继续
chinesesimp.UninstallCancel=取消
chinesetrad.UninstallOptionsTitle=解除安裝選項
chinesetrad.UninstallOptionsPrompt=即將解除安裝 GeoChemistry Nexus。%n%n可選擇同時刪除使用者目錄下的本機資料（設定、範本庫、記錄等）。此操作無法復原。
chinesetrad.DeleteUserData=同時刪除本機使用者資料
chinesetrad.UninstallContinue=繼續
chinesetrad.UninstallCancel=取消
german.UninstallOptionsTitle=Deinstallationsoptionen
german.UninstallOptionsPrompt=Sie sind dabei, GeoChemistry Nexus zu deinstallieren.%n%nOptional können lokale Benutzerdaten im Profil gelöscht werden (Einstellungen, Vorlagendatenbanken, Protokolle usw.). Dies kann nicht rückgängig gemacht werden.
german.DeleteUserData=Lokale Benutzerdaten ebenfalls löschen
german.UninstallContinue=Weiter
german.UninstallCancel=Abbrechen
french.UninstallOptionsTitle=Options de désinstallation
french.UninstallOptionsPrompt=Vous êtes sur le point de désinstaller GeoChemistry Nexus.%n%nVous pouvez également supprimer les données utilisateur locales du profil (paramètres, bases de modèles, journaux, etc.). Cette action est irréversible.
french.DeleteUserData=Supprimer aussi les données utilisateur locales
french.UninstallContinue=Continuer
french.UninstallCancel=Annuler
spanish.UninstallOptionsTitle=Opciones de desinstalación
spanish.UninstallOptionsPrompt=Está a punto de desinstalar GeoChemistry Nexus.%n%nTambién puede eliminar los datos locales del usuario en su perfil (configuración, bases de plantillas, registros, etc.). Esta acción no se puede deshacer.
spanish.DeleteUserData=Eliminar también los datos locales del usuario
spanish.UninstallContinue=Continuar
spanish.UninstallCancel=Cancelar
russian.UninstallOptionsTitle=Параметры удаления
russian.UninstallOptionsPrompt=Вы собираетесь удалить GeoChemistry Nexus.%n%nПри желании можно также удалить локальные данные пользователя в профиле (настройки, базы шаблонов, журналы и т. д.). Это действие необратимо.
russian.DeleteUserData=Также удалить локальные данные пользователя
russian.UninstallContinue=Продолжить
russian.UninstallCancel=Отмена
japanese.UninstallOptionsTitle=アンインストールオプション
japanese.UninstallOptionsPrompt=GeoChemistry Nexus をアンインストールしようとしています。%n%n必要に応じて、プロファイル内のローカルユーザーデータ（設定、テンプレートデータベース、ログなど）も削除できます。この操作は元に戻せません。
japanese.DeleteUserData=ローカルユーザーデータも削除する
japanese.UninstallContinue=続行
japanese.UninstallCancel=キャンセル
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
Source: "redist\{#DotNetRedistFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet10DesktopInstalled

[InstallDelete]
Type: files; Name: "{app}\FileTypeIcons\diagram-template.ico"
Type: files; Name: "{app}\FileTypeIcons\geothermobarometer-template.ico"
Type: dirifempty; Name: "{app}\FileTypeIcons"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; 旧版登记过 .gndiag / .gngtm。升级时整键删除，不再创建新关联。
Root: HKLM; Subkey: "Software\Classes\.gndiag"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\.gngtm"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\GeoChemistryNexus.DiagramTemplate"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\GeoChemistryNexus.Geothermometer"; Flags: deletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.gndiag"; Flags: deletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.gngtm"; Flags: deletekey

[Run]
Filename: "{tmp}\{#DotNetRedistFile}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 10 Desktop Runtime..."; Check: not IsDotNet10DesktopInstalled; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DeleteUserData: Boolean;

{ .NET 运行时在注册表中以“值名称”记录已安装版本（如 10.0.15），位于 32 位视图（WOW6432Node）下的 x64\sharedfx 键 }
function IsDotNet10DesktopInstalled: Boolean;
var
  ValueNames: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetValueNames(HKLM32, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', ValueNames) then
  begin
    for I := 0 to GetArrayLength(ValueNames) - 1 do
    begin
      if Copy(ValueNames[I], 1, 3) = '10.' then
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
