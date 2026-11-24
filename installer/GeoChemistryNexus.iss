#define MyAppName "GeoChemistry Nexus"
#define MyAppExeName "GeoChemistryNexus.exe"
#define MyAppPublisher "Maxwell Lei"
#define MyAppURL "https://github.com/MaxwellLei/GeoChemistry-Nexus"
; 发版时与 GeoChemistryNexus.csproj 中的 AssemblyVersion 保持一致
#define MyAppVersion "0.9.0"

#define PublishDir "..\src\_publish\win-x64"
; 精简版（不内置 .NET 运行时）：由 GeoChemistryNexus-Lite.iss 先 #define LITE 再包含本脚本，
; 或命令行 ISCC /DLITE GeoChemistryNexus.iss。输出文件名带 -lite 后缀，须与客户端 OfficialContentEndpoints.LiteInstallerAssetSuffix 一致。
#ifdef LITE
  #define MyAppOutputBase "GeoChemistryNexus-Setup-" + MyAppVersion + "-x64-lite"
#else
  #define MyAppOutputBase "GeoChemistryNexus-Setup-" + MyAppVersion + "-x64"
#endif
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
SetupIconFile=GeoChemistryNexus_Icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Inno Setup 6 默认不显示欢迎页，开启后欢迎页与结束页都会带左侧大图
DisableWelcomePage=no
; 现代经典风格：欢迎/结束页左侧大图 + 内页右上角小图（由 tools\make_wizard_images.py 生成，覆盖 100%~300% 缩放）
WizardImageFile=assets\wizard-large-164x314.png,assets\wizard-large-192x386.png,assets\wizard-large-246x459.png,assets\wizard-large-273x556.png,assets\wizard-large-328x628.png,assets\wizard-large-355x700.png
WizardSmallImageFile=assets\wizard-small-55x55.png,assets\wizard-small-64x68.png,assets\wizard-small-83x80.png,assets\wizard-small-92x97.png,assets\wizard-small-110x106.png,assets\wizard-small-119x123.png,assets\wizard-small-138x140.png
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
; 升级会删掉旧的 .gndiag / .gngtm 关联，装完后通知资源管理器刷新
ChangesAssociations=yes
; 安装声明（免责声明）页：默认英文，各语言在 [Languages] 中覆盖
LicenseFile=license\Disclaimer.en.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"; LicenseFile: "license\Disclaimer.de.txt"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"; LicenseFile: "license\Disclaimer.fr.txt"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"; LicenseFile: "license\Disclaimer.es.txt"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"; LicenseFile: "license\Disclaimer.ru.txt"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"; LicenseFile: "license\Disclaimer.ja.txt"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"; LicenseFile: "license\Disclaimer.ko.txt"
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"; LicenseFile: "license\Disclaimer.zh-CN.txt"
Name: "chinesetrad"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"; LicenseFile: "license\Disclaimer.zh-TW.txt"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"; LicenseFile: "license\Disclaimer.pt-BR.txt"

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
brazilianportuguese.UninstallOptionsTitle=Opções de desinstalação
brazilianportuguese.UninstallOptionsPrompt=Você está prestes a desinstalar o GeoChemistry Nexus.%n%nVocê também pode remover os dados locais do usuário armazenados no seu perfil (configurações, bancos de dados de modelos, logs etc.). Esta ação não pode ser desfeita.
brazilianportuguese.DeleteUserData=Excluir também os dados locais do usuário
brazilianportuguese.UninstallContinue=Continuar
brazilianportuguese.UninstallCancel=Cancelar

#ifdef LITE
; 精简版在未安装 .NET 10 桌面运行时的电脑上提示改用完整版安装包
english.LiteRuntimeMissing=This is the Lite installer, which does not include the .NET 10 Desktop Runtime, and the runtime was not found on this computer.%n%nPlease download and run the full installer (the one without "lite" in its file name) instead.%n%nOpen the download page now?
chinesesimp.LiteRuntimeMissing=这是精简版安装包，不包含 .NET 10 桌面运行时，而本机未检测到该运行时。%n%n请改为下载并运行完整版安装包（文件名中不带 "lite"）。%n%n是否现在打开下载页面？
chinesetrad.LiteRuntimeMissing=這是精簡版安裝程式，不包含 .NET 10 桌面執行階段，而本機未偵測到該執行階段。%n%n請改為下載並執行完整版安裝程式（檔名中不含 "lite"）。%n%n是否現在開啟下載頁面？
german.LiteRuntimeMissing=Dies ist das Lite-Installationsprogramm. Es enthält die .NET 10 Desktop Runtime nicht, und die Runtime wurde auf diesem Computer nicht gefunden.%n%nBitte laden Sie stattdessen das vollständige Installationsprogramm herunter (ohne "lite" im Dateinamen).%n%nDie Download-Seite jetzt öffnen?
french.LiteRuntimeMissing=Ceci est l'installateur allégé (Lite), qui n'inclut pas le .NET 10 Desktop Runtime, et ce runtime est introuvable sur cet ordinateur.%n%nVeuillez télécharger et exécuter l'installateur complet (celui sans "lite" dans le nom du fichier).%n%nOuvrir la page de téléchargement maintenant ?
spanish.LiteRuntimeMissing=Este es el instalador Lite, que no incluye el .NET 10 Desktop Runtime, y el runtime no se encontró en este equipo.%n%nDescargue y ejecute en su lugar el instalador completo (el que no lleva "lite" en el nombre del archivo).%n%n¿Abrir la página de descarga ahora?
russian.LiteRuntimeMissing=Это облегчённый (Lite) установщик: он не содержит .NET 10 Desktop Runtime, а на этом компьютере среда выполнения не найдена.%n%nСкачайте и запустите полный установщик (без "lite" в имени файла).%n%nОткрыть страницу загрузки сейчас?
japanese.LiteRuntimeMissing=これは .NET 10 デスクトップ ランタイムを含まない Lite 版インストーラーで、このコンピューターにはランタイムが見つかりませんでした。%n%n代わりに完全版インストーラー（ファイル名に "lite" を含まないもの）をダウンロードして実行してください。%n%nダウンロード ページを今すぐ開きますか？
korean.LiteRuntimeMissing=이 설치 프로그램은 .NET 10 데스크톱 런타임이 포함되지 않은 Lite 버전이며, 이 컴퓨터에서 런타임을 찾을 수 없습니다.%n%n대신 전체 버전 설치 프로그램(파일 이름에 "lite"가 없는 것)을 다운로드하여 실행하세요.%n%n지금 다운로드 페이지를 여시겠습니까?
brazilianportuguese.LiteRuntimeMissing=Este é o instalador Lite, que não inclui o .NET 10 Desktop Runtime, e o runtime não foi encontrado neste computador.%n%nBaixe e execute o instalador completo (aquele sem "lite" no nome do arquivo).%n%nAbrir a página de download agora?
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; 路径相对于本 .iss 文件所在目录（installer\）
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Templates.db,Geothermometer.db,GraphMapList.json,GeoT-List.json,Logs\*,portable.flag"
#ifndef LITE
Source: "redist\{#DotNetRedistFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNet10DesktopInstalled
#endif

[InstallDelete]
Type: files; Name: "{app}\FileTypeIcons\diagram-template.ico"
Type: files; Name: "{app}\FileTypeIcons\geothermobarometer-template.ico"
Type: dirifempty; Name: "{app}\FileTypeIcons"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; 旧版登记过 .gndiag / .gngtm。升级时整键删除，不再创建新关联。
; 各用户 HKCU 下的 FileExts 残留不清理（管理员安装模式下 HKCU 不一定是目标用户，残留无实际影响）。
Root: HKLM; Subkey: "Software\Classes\.gndiag"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\.gngtm"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\GeoChemistryNexus.DiagramTemplate"; Flags: deletekey
Root: HKLM; Subkey: "Software\Classes\GeoChemistryNexus.Geothermometer"; Flags: deletekey

[Run]
#ifndef LITE
Filename: "{tmp}\{#DotNetRedistFile}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 10 Desktop Runtime..."; Check: not IsDotNet10DesktopInstalled; Flags: waituntilterminated
#endif
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DeleteUserData: Boolean;

{ .NET 运行时在注册表中以“值名称”记录已安装版本（如 10.0.15），位于 32 位视图（WOW6432Node）下的 x64\sharedfx 键。
  dotnet 宿主（hostfxr）读取该键时同样使用 KEY_WOW64_32KEY，故此处必须用 HKLM32，不能用 HKLM64。 }
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

#ifdef LITE
{ 精简版不内置运行时：缺少运行时就在复制任何文件之前中止，并引导用户去下载完整版 }
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDotNet10DesktopInstalled then
  begin
    { 静默安装不弹窗，直接以失败退出 }
    if not WizardSilent then
    begin
      if MsgBox(ExpandConstant('{cm:LiteRuntimeMissing}'), mbError, MB_YESNO) = IDYES then
        ShellExec('open', '{#MyAppURL}/releases/latest', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;
#endif

const
  { 品牌色 #3C4A98、进度条底色 #DFE2EE、分隔线色 #E3E5EE（Delphi 颜色为 BGR 顺序） }
  BrandColor = $984A3C;
  TrackColor = $EEE2DF;
  LineColor = $EEE5E3;

function SendMessage(hWnd: HWND; Msg: UINT; wParam: Longint; lParam: Longint): Longint;
  external 'SendMessageW@user32.dll stdcall';

var
  ProgressTrack: TPanel;
  ProgressFill: TPanel;
  ProgressPercent: TNewStaticText;

{ 原生进度条颜色跟随系统主题（绿色）且无法修改，这里隐藏它，用两个面板自绘靛蓝进度条 }
procedure CreateBrandProgressBar;
var
  Gauge: TNewProgressBar;
  BarHeight: Integer;
begin
  Gauge := WizardForm.ProgressGauge;
  BarHeight := ScaleY(8);

  ProgressTrack := TPanel.Create(WizardForm);
  ProgressTrack.Parent := WizardForm.InstallingPage;
  ProgressTrack.BevelOuter := bvNone;
  ProgressTrack.ParentBackground := False;
  ProgressTrack.Color := TrackColor;
  ProgressTrack.Left := Gauge.Left;
  ProgressTrack.Width := Gauge.Width;
  ProgressTrack.Height := BarHeight;
  ProgressTrack.Top := Gauge.Top + (Gauge.Height - BarHeight) div 2;

  ProgressFill := TPanel.Create(WizardForm);
  ProgressFill.Parent := ProgressTrack;
  ProgressFill.BevelOuter := bvNone;
  ProgressFill.ParentBackground := False;
  ProgressFill.Color := BrandColor;
  ProgressFill.Left := 0;
  ProgressFill.Top := 0;
  ProgressFill.Width := 0;
  ProgressFill.Height := BarHeight;

  ProgressPercent := TNewStaticText.Create(WizardForm);
  ProgressPercent.Parent := WizardForm.InstallingPage;
  ProgressPercent.Caption := '0%';
  ProgressPercent.Top := ProgressTrack.Top + BarHeight + ScaleY(8);
  ProgressPercent.Left := ProgressTrack.Left + ProgressTrack.Width - ProgressPercent.Width;

  Gauge.Visible := False;
end;

procedure InitializeWizard;
var
  Line: TPanel;
begin
  { 页面标题、欢迎页与结束页标题使用品牌色 }
  WizardForm.PageNameLabel.Font.Color := BrandColor;
  WizardForm.WelcomeLabel1.Font.Color := BrandColor;
  WizardForm.FinishedHeadingLabel.Font.Color := BrandColor;

  { 页面标题区底部加一条分隔线 }
  Line := TPanel.Create(WizardForm);
  Line.Parent := WizardForm.MainPanel;
  Line.Align := alBottom;
  Line.Height := ScaleY(1);
  Line.BevelOuter := bvNone;
  Line.ParentBackground := False;
  Line.Color := LineColor;

  { “准备安装”页摘要框默认是灰底，改为白底与许可协议框一致 }
  WizardForm.ReadyMemo.Color := clWindow;
  SendMessage(WizardForm.ReadyMemo.Handle, $0443 { EM_SETBKGNDCOLOR }, 0, $FFFFFF);

  CreateBrandProgressBar;
end;

{ 文件复制进度变化时同步自绘进度条与百分比 }
procedure CurInstallProgressChanged(CurProgress, MaxProgress: Integer);
var
  Done: Int64;
begin
  if MaxProgress <= 0 then
    Exit;
  Done := CurProgress;
  if Done > MaxProgress then
    Done := MaxProgress;
  ProgressFill.Width := Integer((Done * ProgressTrack.Width) div MaxProgress);
  ProgressPercent.Caption := IntToStr(Integer((Done * 100) div MaxProgress)) + '%';
  ProgressPercent.Left := ProgressTrack.Left + ProgressTrack.Width - ProgressPercent.Width;
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
