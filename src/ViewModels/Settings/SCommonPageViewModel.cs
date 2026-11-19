using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    partial class SCommonPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private IReadOnlyList<AppLanguageOption> _appLanguageOptions = AppLanguageOptionFactory.Create();

        [ObservableProperty]
        private string selectedAppLanguageCode = AppCultureRegistry.DefaultAppLanguage;

        [ObservableProperty]
        private int font;     // 字体设置(0是微软雅黑;1是添加字体)

        [ObservableProperty]
        private int autoOffSeconds = 5;    // 通知自动关闭时间（秒，2 ~ 5）；配置项 auto_off_time 仍保存索引(0是5秒;1是4秒;2是3秒;3是2秒)

        [ObservableProperty]
        private bool boot;  // 开机自动启动

        [ObservableProperty]
        private bool autoCheck;  // 自动检查更新

        [ObservableProperty]
        private int exitMode;    // 退出方式(0是最小化到托盘;1是退出程序)

        [ObservableProperty]
        private string version;     // 软件版本

        [ObservableProperty]
        private bool _developerMode; // 开发者模式

        [ObservableProperty]
        private bool isAppUpdateOverlayVisible;

        [ObservableProperty]
        private double appUpdateProgress;

        [ObservableProperty]
        private bool isAppUpdateProgressIndeterminate = true;

        [ObservableProperty]
        private string appUpdateStatusText = string.Empty;

        [ObservableProperty]
        private int mainSidebarCollapseMode; // 主窗体侧边栏收起样式(0是全部收起;1是保留图标)

        /// <summary>
        /// 界面缩放：0=自动，1=100%，2=125%，3=150%，4=200%
        /// </summary>
        [ObservableProperty]
        private int uiScaleModeIndex;

        /// <summary>
        /// 可选的主题颜色（内置色块）。
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<AppThemeOption> _themeOptions = AppThemeHelper.CreateOptions();

        /// <summary>
        /// 当前选中的主题 Id。
        /// </summary>
        [ObservableProperty]
        private string selectedThemeId = AppThemeHelper.DefaultThemeId;

        public RelayCommand TryAutoOffTimeCommand { get; private set; }   // 试一下：弹出一条按当前秒数自动关闭的示例通知
        public RelayCommand LanguageChangedCommand { get; private set; }   // 修改语言命令
        public AsyncRelayCommand CheckUpdateCommand { get; private set; }   // 检查更新命令
        public AsyncRelayCommand ForceUpdateCommand { get; private set; }   // 强制更新命令（开发者模式）
        public RelayCommand ExitProgrmModeCommand { get; private set; }   // 退出程序方式命令
        private bool isLoading = true;

        // 初始化
        public SCommonPageViewModel()
        {
            LanguageChangedCommand = new RelayCommand(ExecuteLanguageChangedCommand);
            TryAutoOffTimeCommand = new RelayCommand(ExecuteTryAutoOffTimeCommand);
            CheckUpdateCommand = new AsyncRelayCommand(ExecuteCheckUpdateCommandAsync);
            ForceUpdateCommand = new AsyncRelayCommand(ExecuteForceUpdateCommandAsync);
            ExitProgrmModeCommand = new RelayCommand(ExecuteExitProgrmModeCommand);
            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
            ReadConfig();   // 读取配置文件

            // 获取版本信息
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
                
        }

        // 退出程序方式
        private void ExecuteExitProgrmModeCommand()
        {
            ConfigHelper.SetConfig(ConfigKeys.ExitProgramMode, ExitMode.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 检查更新
        private async Task ExecuteCheckUpdateCommandAsync()
        {
            if (IsAppUpdateOverlayVisible)
                return;

            try
            {
                var updateInfo = await UpdateHelper.GetLatestAppUpdateInfoAsync();

                if (updateInfo.HasUpdate)
                {
                    bool hasCachedInstaller = UpdateHelper.TryGetCachedInstallerPath(updateInfo, out _);
                    string confirmMessage = hasCachedInstaller
                        ? string.Format(
                            GetLanguageText(
                                "update_cached_installer_confirm",
                                "Installer for version {0} is already downloaded. Install now?"),
                            updateInfo.LatestVersion)
                        : string.Format(
                            LanguageService.Instance["new_version_available_github"] ?? "New version {0} is available. Download and install now?",
                            updateInfo.LatestVersion);

                    bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                        confirmMessage,
                        LanguageService.Instance["Cancel"],
                        LanguageService.Instance["go_to_download"]);

                    if (!isConfirmed)
                        return;

                    await DownloadAndInstallAppUpdateAsync(updateInfo, hasCachedInstaller: hasCachedInstaller);
                    return;
                }

                try
                {
                    await UpdateHelper.CheckAndUpdatePlotCategoriesAsync();
                }
                catch
                {
                    // 忽略更新 PlotTemplateCategories.json 的错误
                }

                MessageHelper.Success(LanguageService.Instance["already_latest_version"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["unknown_error_checking_for_updates"] + $": {ex.Message}");
            }
        }

        // 强制更新（开发者模式，不比对版本号）
        private async Task ExecuteForceUpdateCommandAsync()
        {
            if (!DeveloperMode)
                return;

            if (IsAppUpdateOverlayVisible)
                return;

            try
            {
                var updateInfo = await UpdateHelper.GetLatestAppUpdateInfoAsync(forceDownload: true);

                if (string.IsNullOrWhiteSpace(updateInfo.InstallerDownloadUrl))
                {
                    MessageHelper.Warning(LanguageService.Instance["update_installer_not_found"]
                        ?? "Installer package not found in the latest release.");
                    UpdateHelper.OpenLatestReleasePage();
                    return;
                }

                bool hasCachedInstaller = UpdateHelper.TryGetCachedInstallerPath(updateInfo, out _);
                string confirmMessage = hasCachedInstaller
                    ? string.Format(
                        GetLanguageText(
                            "update_cached_installer_confirm",
                            "Installer for version {0} is already downloaded. Install now?"),
                        updateInfo.LatestVersion)
                    : string.Format(
                        LanguageService.Instance["force_update_app_confirm"]
                            ?? "Download and install the latest version {0}?",
                        updateInfo.LatestVersion);

                bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                    confirmMessage,
                    LanguageService.Instance["Cancel"],
                    LanguageService.Instance["go_to_download"]);

                if (!isConfirmed)
                    return;

                await DownloadAndInstallAppUpdateAsync(updateInfo, bypassPortableCheck: true, hasCachedInstaller: hasCachedInstaller);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["unknown_error_checking_for_updates"] + $": {ex.Message}");
            }
        }

        private async Task DownloadAndInstallAppUpdateAsync(
            AppUpdateInfo updateInfo,
            bool bypassPortableCheck = false,
            bool hasCachedInstaller = false)
        {
            ShowAppUpdateOverlay(
                hasCachedInstaller
                    ? GetLanguageText("update_cached_installer_ready", "Ready")
                    : LanguageService.Instance["downloading_ellipsis"] ?? "Downloading...",
                !hasCachedInstaller,
                hasCachedInstaller ? 100 : 0);

            try
            {
                var progress = new Progress<double>(value =>
                {
                    IsAppUpdateProgressIndeterminate = false;
                    AppUpdateProgress = Math.Clamp(value, 0, 100);
                });

                await AppUpdateService.TryDownloadAndInstallUpdateAsync(updateInfo, bypassPortableCheck, progress);
            }
            finally
            {
                HideAppUpdateOverlay();
            }
        }

        private void ShowAppUpdateOverlay(string statusText, bool indeterminate, double progress = 0)
        {
            AppUpdateStatusText = statusText;
            IsAppUpdateProgressIndeterminate = indeterminate;
            AppUpdateProgress = progress;
            IsAppUpdateOverlayVisible = true;
        }

        private void HideAppUpdateOverlay()
        {
            IsAppUpdateOverlayVisible = false;
            IsAppUpdateProgressIndeterminate = true;
            AppUpdateProgress = 0;
            AppUpdateStatusText = string.Empty;
        }

        private static string GetLanguageText(string key, string fallback)
        {
            string value = LanguageService.Instance[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        // 开发者模式
        partial void OnDeveloperModeChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.DeveloperMode, DeveloperMode.ToString());
            WeakReferenceMessenger.Default.Send(new DeveloperModeChangedMessage(value));
            RefreshAppLanguageOptions();

            if (value)
            {
                string warning = LanguageService.Instance["developer_mode_warning"];
                if (string.IsNullOrEmpty(warning))
                {
                    warning = LanguageService.Instance["dev_mode_warning_non_developers"];
                }
                MessageHelper.Warning(warning);
            }
            else
            {
                MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
            }
        }

        // 是否开机启动
        partial void OnBootChanged(bool value)
        {
            if (isLoading) return;
            BootHelper.SetAutoRun(Boot);
            ConfigHelper.SetConfig(ConfigKeys.Boot, Boot.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 是否检查更新
        partial void OnAutoCheckChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.AutoCheckUpdate, AutoCheck.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 主窗体侧边栏收起样式
        partial void OnMainSidebarCollapseModeChanged(int value)
        {
            if (isLoading) return;

            ConfigHelper.SetConfig(ConfigKeys.MainSidebarCollapseMode, value.ToString());
            WeakReferenceMessenger.Default.Send(new MainSidebarCollapseModeChangedMessage(value == 1));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 界面语言切换后刷新主题名称（色块的悬浮提示）与语言选择器中的语言名称
        private void OnAppLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Item[]")
                return;

            foreach (var option in ThemeOptions)
                option.RefreshDisplayName();

            RefreshAppLanguageOptions();
        }

        // 主题颜色
        partial void OnSelectedThemeIdChanged(string value)
        {
            if (isLoading || string.IsNullOrEmpty(value))
                return;

            if (string.Equals(value, AppThemeHelper.CurrentId, StringComparison.Ordinal))
                return;

            AppThemeHelper.SetTheme(value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 界面缩放
        partial void OnUiScaleModeIndexChanged(int value)
        {
            if (isLoading) return;
            UiScaleHelper.SetMode(IndexToUiScaleMode(value));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        private static UiScaleMode IndexToUiScaleMode(int index) => index switch
        {
            0 => UiScaleMode.Percent90,
            2 => UiScaleMode.Percent125,
            3 => UiScaleMode.Percent150,
            _ => UiScaleMode.Percent100
        };

        private static int UiScaleModeToIndex(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent90 => 0,
            UiScaleMode.Percent125 => 2,
            UiScaleMode.Percent150 => 3,
            _ => 1
        };

        // 修改消息通知时间
        partial void OnAutoOffSecondsChanged(int value)
        {
            if (isLoading) return;

            // 数值未变化则不提示、不重复保存
            if (value == MessageHelper.WaitTime)
                return;

            MessageHelper.WaitTime = value;
            ConfigHelper.SetConfig(ConfigKeys.AutoOffTime,
                MessageHelper.GetIndexBySeconds(value).ToString(CultureInfo.InvariantCulture));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 试一下：按当前设置的秒数弹出示例通知
        private void ExecuteTryAutoOffTimeCommand()
        {
            MessageHelper.WaitTime = AutoOffSeconds;
            string template = GetLanguageText(
                "MessageAutoClose_try_message",
                "This is a sample notification. It will close automatically in {0} seconds.");
            MessageHelper.Info(string.Format(template, AutoOffSeconds));
        }

        // 刷新语言选择器的选项（开发者模式切换显示文化代码、界面语言切换后语言名称变化）；
        // 选择器按 SelectedAppLanguageCode 自动保持当前项
        private void RefreshAppLanguageOptions()
        {
            AppLanguageOptions = AppLanguageOptionFactory.Create();
        }

        // 修改语言
        private void ExecuteLanguageChangedCommand()
        {
            if (isLoading)
                return;

            string selectedCode = AppCultureRegistry.ResolveAppLanguage(SelectedAppLanguageCode);
            string currentCode = AppCultureRegistry.ResolveAppLanguage(ConfigHelper.GetConfig(ConfigKeys.Language));
            if (string.Equals(selectedCode, currentCode, StringComparison.OrdinalIgnoreCase))
                return;

            SelectedAppLanguageCode = selectedCode;
            ConfigHelper.SetConfig(ConfigKeys.Language, selectedCode);
            LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo(selectedCode));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }



        // 读取配置文件
        void ReadConfig()
        {
            // 读取语言设置
            string? langConfig = Helpers.ConfigHelper.GetConfig(ConfigKeys.Language);
            SelectedAppLanguageCode = AppCultureRegistry.ResolveAppLanguage(langConfig ?? string.Empty);

            AutoOffSeconds = MessageHelper.GetWaitSecondsByIndex(
                int.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.AutoOffTime), NumberStyles.Integer, CultureInfo.InvariantCulture, out int autoOffTime)
                    ? autoOffTime
                    : 0);

            if (bool.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.DeveloperMode), out bool devMode))
            {
                DeveloperMode = devMode;
            }

            Boot = bool.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.Boot), out bool boot) && boot;
            AutoCheck = bool.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.AutoCheckUpdate), out bool autoCheck) && autoCheck;
            ExitMode = int.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.ExitProgramMode), NumberStyles.Integer, CultureInfo.InvariantCulture, out int exitMode)
                ? exitMode
                : 0;
            if (int.TryParse(Helpers.ConfigHelper.GetConfig(ConfigKeys.MainSidebarCollapseMode), out int sidebarCollapseMode)
                && sidebarCollapseMode >= 0 && sidebarCollapseMode <= 1)
            {
                MainSidebarCollapseMode = sidebarCollapseMode;
            }
            else
            {
                MainSidebarCollapseMode = 0;
            }

            UiScaleModeIndex = UiScaleModeToIndex(UiScaleHelper.GetMode());
            SelectedThemeId = AppThemeHelper.CurrentId;

            isLoading = false;
        }

    }
}
