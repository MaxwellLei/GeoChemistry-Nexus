using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using HandyControl.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using unvell.ReoGrid;

namespace GeoChemistryNexus.ViewModels
{
    partial class SCommonPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private IReadOnlyList<CultureOption> _appLanguageOptions = AppCultureRegistry.GetAppUiOptions();

        //封面流
        [ObservableProperty]
        private CoverFlow? coverFlowMain;

        [ObservableProperty]
        private int coverFlowPageIndex;

        [ObservableProperty]
        private bool isDeleteStartImageVisible;

        [ObservableProperty]
        private string selectedAppLanguageCode = AppCultureRegistry.DefaultAppLanguage;

        [ObservableProperty]
        private int font;     // 字体设置(0是微软雅黑;1是添加字体)

        [ObservableProperty]
        private int autoOffTime;    // 通知自动关闭时间(0是5秒;1是4秒;2是3秒;3是2秒)

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

        public RelayCommand AutoOffTimeChangedCommand { get; private set; }   // 修改通知自动关闭时间命令
        public RelayCommand LanguageChangedCommand { get; private set; }   // 修改语言命令
        public RelayCommand CheckUpdateCommand { get; private set; }   // 检查更新命令
        public RelayCommand ForceUpdateCommand { get; private set; }   // 强制更新命令（开发者模式）
        public RelayCommand ExitProgrmModeCommand { get; private set; }   // 退出程序方式命令
        public RelayCommand AddStartImgCommand { get; private set; }    // 添加启动图
        public RelayCommand DeleteStartImgCommand { get; private set; } // 删除启动图

        /// <summary>
        /// 由 View 重建 CoverFlow（HandyControl CoverFlow 无公开 Clear API）。
        /// 参数：图片路径列表、目标页码。
        /// </summary>
        public Action<IReadOnlyList<string>, int>? RebuildCoverFlowAction { get; set; }

        private readonly List<string> _startImagePaths = new();

        private bool isLoading = true;
        private bool _isRefreshingLanguageOptions;

        // 初始化
        public SCommonPageViewModel()
        {
            LanguageChangedCommand = new RelayCommand(ExecuteLanguageChangedCommand);
            AutoOffTimeChangedCommand = new RelayCommand(ExecuteAutoOffTimeChangedCommand);
            CheckUpdateCommand = new RelayCommand(ExecuteCheckUpdateCommandAsync);
            ForceUpdateCommand = new RelayCommand(ExecuteForceUpdateCommandAsync);
            ExitProgrmModeCommand = new RelayCommand(ExecuteExitProgrmModeCommand);
            AddStartImgCommand = new RelayCommand(ExecuteAddStartImgCommand);
            DeleteStartImgCommand = new RelayCommand(ExecuteDeleteStartImgCommand, CanExecuteDeleteStartImgCommand);
            ReadConfig();   // 读取配置文件

            // 获取版本信息
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
                
        }

        // 退出程序方式
        private void ExecuteExitProgrmModeCommand()
        {
            ConfigHelper.SetConfig("exit_program_mode", ExitMode.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 检查更新
        private async void ExecuteCheckUpdateCommandAsync()
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
        private async void ExecuteForceUpdateCommandAsync()
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
            ConfigHelper.SetConfig("developer_mode", DeveloperMode.ToString());
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
            ConfigHelper.SetConfig("boot", Boot.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 是否检查更新
        partial void OnAutoCheckChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("auto_check_update", AutoCheck.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 主窗体侧边栏收起样式
        partial void OnMainSidebarCollapseModeChanged(int value)
        {
            if (isLoading) return;

            ConfigHelper.SetConfig("main_sidebar_collapse_mode", value.ToString());
            WeakReferenceMessenger.Default.Send(new MainSidebarCollapseModeChangedMessage(value == 1));
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
            1 => UiScaleMode.Percent100,
            2 => UiScaleMode.Percent125,
            3 => UiScaleMode.Percent150,
            4 => UiScaleMode.Percent200,
            _ => UiScaleMode.Auto
        };

        private static int UiScaleModeToIndex(UiScaleMode mode) => mode switch
        {
            UiScaleMode.Percent100 => 1,
            UiScaleMode.Percent125 => 2,
            UiScaleMode.Percent150 => 3,
            UiScaleMode.Percent200 => 4,
            _ => 0
        };

        // 修改消息通知时间
        private void ExecuteAutoOffTimeChangedCommand()
        {
            switch (AutoOffTime)
            {
                case 0:
                    MessageHelper.WaitTime = 5;
                    break;
                case 1:
                    MessageHelper.WaitTime = 4;
                    break;
                case 2:
                    MessageHelper.WaitTime = 3;
                    break;
                case 3:
                    MessageHelper.WaitTime = 2;
                    break;
            }
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        // 刷新语言下拉列表（开发者模式切换时显示名会变化），并保持当前选中项
        private void RefreshAppLanguageOptions()
        {
            _isRefreshingLanguageOptions = true;
            try
            {
                string currentCode = AppCultureRegistry.ResolveAppLanguage(ConfigHelper.GetConfig("language"));
                AppLanguageOptions = AppCultureRegistry.GetAppUiOptions();
                // ItemsSource 替换后 ComboBox 可能丢失选中状态，需先清空再恢复以触发重新匹配
                SelectedAppLanguageCode = string.Empty;
                SelectedAppLanguageCode = currentCode;
            }
            finally
            {
                _isRefreshingLanguageOptions = false;
            }
        }

        // 修改语言
        private void ExecuteLanguageChangedCommand()
        {
            if (_isRefreshingLanguageOptions || isLoading)
                return;

            string selectedCode = AppCultureRegistry.ResolveAppLanguage(SelectedAppLanguageCode);
            string currentCode = AppCultureRegistry.ResolveAppLanguage(ConfigHelper.GetConfig("language"));
            if (string.Equals(selectedCode, currentCode, StringComparison.OrdinalIgnoreCase))
                return;

            SelectedAppLanguageCode = selectedCode;
            ConfigHelper.SetConfig("language", selectedCode);
            LanguageService.Instance.ChangeLanguage(new System.Globalization.CultureInfo(selectedCode));
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }



        // 读取配置文件
        void ReadConfig()
        {
            // 读取语言设置
            string? langConfig = Helpers.ConfigHelper.GetConfig("language");
            SelectedAppLanguageCode = AppCultureRegistry.ResolveAppLanguage(langConfig ?? string.Empty);

            AutoOffTime = int.Parse(Helpers.ConfigHelper.GetConfig("auto_off_time") ?? "0");    //读取消息通知时间
            
            if (bool.TryParse(Helpers.ConfigHelper.GetConfig("developer_mode"), out bool devMode))
            {
                DeveloperMode = devMode;
            }

            Boot = bool.Parse(Helpers.ConfigHelper.GetConfig("boot") ?? "false");     //读取是否自动开机
            AutoCheck = bool.Parse(Helpers.ConfigHelper.GetConfig("auto_check_update") ?? "false");   //读取是否自动检查更新
            ExitMode = int.Parse(Helpers.ConfigHelper.GetConfig("exit_program_mode") ?? "0");  //读取退出方式
            if (int.TryParse(Helpers.ConfigHelper.GetConfig("main_sidebar_collapse_mode"), out int sidebarCollapseMode)
                && sidebarCollapseMode >= 0 && sidebarCollapseMode <= 1)
            {
                MainSidebarCollapseMode = sidebarCollapseMode;
            }
            else
            {
                MainSidebarCollapseMode = 0;
            }

            UiScaleModeIndex = UiScaleModeToIndex(UiScaleHelper.GetMode());

            isLoading = false;
        }

        // 添加启动图
        private void ExecuteAddStartImgCommand()
        {
            string? sourceFilePath = FileHelper.GetFilePath("ImageFile(*.jpg,*.png)|*.jpg;*.png");
            if (sourceFilePath == null)
                return;

            string sourceFileName = Path.GetFileName(sourceFilePath);
            StartPicHelper.EnsureFolderExists();
            string destinationFilePath = Path.Combine(StartPicHelper.FolderPath, sourceFileName);

            try
            {
                File.Copy(sourceFilePath, destinationFilePath, true);

                int existingIndex = _startImagePaths.FindIndex(p =>
                    string.Equals(Path.GetFileName(p), sourceFileName, StringComparison.OrdinalIgnoreCase));
                if (existingIndex >= 0)
                    _startImagePaths[existingIndex] = destinationFilePath;
                else
                    _startImagePaths.Add(destinationFilePath);

                // 覆盖同名文件或新增后重建，避免 CoverFlow 内部索引与文件列表不一致
                int targetIndex = _startImagePaths.FindIndex(p =>
                    string.Equals(Path.GetFileName(p), sourceFileName, StringComparison.OrdinalIgnoreCase));
                if (targetIndex < 0)
                    targetIndex = Math.Max(0, _startImagePaths.Count - 1);

                RebuildCoverFlow(targetIndex);
                MessageHelper.Success(LanguageService.Instance["start_image_copy_successfully"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Warning(LanguageService.Instance["start_image_copy_error"] + ex.Message);
            }
        }

        // 删除当前预览的自定义启动图（官方图不可删，按钮亦不显示）
        private bool CanExecuteDeleteStartImgCommand() => IsDeleteStartImageVisible;

        private void ExecuteDeleteStartImgCommand()
        {
            if (!CanExecuteDeleteStartImgCommand())
                return;

            int index = CoverFlowPageIndex;
            if (index < 0 || index >= _startImagePaths.Count)
                return;

            string filePath = _startImagePaths[index];
            if (!StartPicHelper.IsCustomImage(filePath))
                return;

            // 先从列表移除并重建 CoverFlow，释放图片文件句柄后再删除磁盘文件
            _startImagePaths.RemoveAt(index);
            int newIndex = _startImagePaths.Count == 0
                ? 0
                : Math.Min(index, _startImagePaths.Count - 1);

            RebuildCoverFlow(newIndex);

            if (StartPicHelper.TryDeleteCustomImage(filePath, out string? errorMessage))
            {
                MessageHelper.Success(LanguageService.Instance["start_image_delete_successfully"]
                    ?? "Startup image deleted successfully.");
            }
            else
            {
                // 删除失败时把路径加回并刷新，避免界面与磁盘不一致
                if (index > _startImagePaths.Count)
                    index = _startImagePaths.Count;
                _startImagePaths.Insert(index, filePath);
                RebuildCoverFlow(index);
                MessageHelper.Warning((LanguageService.Instance["start_image_delete_error"]
                    ?? "Failed to delete startup image: ") + (errorMessage ?? string.Empty));
            }
        }

        partial void OnCoverFlowPageIndexChanged(int value)
        {
            UpdateDeleteStartImageVisibility();
        }

        partial void OnIsDeleteStartImageVisibleChanged(bool value)
        {
            DeleteStartImgCommand.NotifyCanExecuteChanged();
        }

        private void UpdateDeleteStartImageVisibility()
        {
            if (CoverFlowPageIndex >= 0 && CoverFlowPageIndex < _startImagePaths.Count)
            {
                IsDeleteStartImageVisible = StartPicHelper.IsCustomImage(_startImagePaths[CoverFlowPageIndex]);
            }
            else
            {
                IsDeleteStartImageVisible = false;
            }
        }

        private void RebuildCoverFlow(int pageIndex)
        {
            int safeIndex = _startImagePaths.Count == 0
                ? 0
                : Math.Clamp(pageIndex, 0, _startImagePaths.Count - 1);

            // 先更新页码，再建控件，确保绑定与删除按钮状态一致
            if (CoverFlowPageIndex != safeIndex)
                CoverFlowPageIndex = safeIndex;

            RebuildCoverFlowAction?.Invoke(_startImagePaths, safeIndex);
            UpdateDeleteStartImageVisibility();
        }

        // 获取启动封面图
        public void GetFlowPic()
        {
            _startImagePaths.Clear();
            _startImagePaths.AddRange(StartPicHelper.GetImageFiles());
            RebuildCoverFlow(0);
        }
    }
}
