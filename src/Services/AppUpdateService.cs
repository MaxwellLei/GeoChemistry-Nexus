using GeoChemistryNexus.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 启动时自动检查程序更新；发现新版本时询问是否立即下载安装。
    /// </summary>
    public static class AppUpdateService
    {
        private static bool _hasChecked;
        private static bool _hasAllowedUnsupportedVersionThisSession;

        public static async Task TryAutoCheckOnStartupAsync()
        {
            if (_hasChecked)
                return;

            _hasChecked = true;

            bool handledMinimumVersion = await TryHandleMinimumSupportedVersionAsync();
            if (handledMinimumVersion)
                return;

            if (!bool.TryParse(ConfigHelper.GetConfig("auto_check_update"), out bool autoCheck) || !autoCheck)
                return;

            if (AppDataPathHelper.IsPortableMode() || AppDataPathHelper.IsDevMode())
                return;

            try
            {
                var info = await UpdateHelper.GetLatestAppUpdateInfoAsync();
                if (!info.HasUpdate)
                    return;

                string message = string.Format(
                    LanguageService.Instance["update_auto_check_notify"]
                        ?? "New version {0} is available. Download and install now?",
                    info.LatestVersion);

                bool confirmed = await MessageHelper.ShowAsyncDialog(
                    message,
                    LanguageService.Instance["Cancel"] ?? "Cancel",
                    LanguageService.Instance["go_to_download"] ?? "Update now");

                if (!confirmed)
                    return;

                await TryDownloadAndInstallUpdateAsync(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppUpdateService] Auto check failed: {ex.Message}");
            }
        }

        public static async Task<bool> TryHandleMinimumSupportedVersionAsync()
        {
            if (_hasAllowedUnsupportedVersionThisSession)
                return false;

            try
            {
                var result = await UpdateHelper.CheckMinimumSupportedVersionAsync();
                if (!result.IsVersionUnsupported)
                    return false;

                string title = LanguageService.Instance["minimum_version_dialog_title"]
                    ?? "Update required";
                string message = string.Format(
                    LanguageService.Instance["minimum_version_dialog_message"]
                        ?? "Your current version {0} is lower than the minimum supported version {1}. Please update GeoChemistry Nexus.",
                    result.CurrentVersion,
                    result.MinimumSupportedVersion);

                int action = await NotificationManager.Instance.ShowThreeButtonDialogAsync(
                    title,
                    message,
                    LanguageService.Instance["minimum_version_auto_update"] ?? "Auto update",
                    LanguageService.Instance["minimum_version_force_use"] ?? "Force use",
                    LanguageService.Instance["minimum_version_manual_update"] ?? "Manual update",
                    true);

                if (action == 0)
                {
                    await TryDownloadAndInstallLatestAsync(forceDownload: true);
                    return true;
                }

                if (action == 2)
                {
                    UpdateHelper.OpenLatestReleasePage();
                    return true;
                }

                bool confirmed = await MessageHelper.ShowAsyncDialog(
                    LanguageService.Instance["minimum_version_force_confirm_message"]
                        ?? "This version is no longer supported. Continuing may cause unpredictable problems. Continue anyway?",
                    LanguageService.Instance["Cancel"] ?? "Cancel",
                    LanguageService.Instance["minimum_version_force_confirm"] ?? "Continue anyway");

                if (confirmed)
                    _hasAllowedUnsupportedVersionThisSession = true;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppUpdateService] Minimum version check failed: {ex.Message}");
                return false;
            }
        }

        public static async Task TryDownloadAndInstallLatestAsync(
            bool forceDownload = false,
            bool bypassPortableCheck = false,
            IProgress<double>? progress = null)
        {
            var updateInfo = await UpdateHelper.GetLatestAppUpdateInfoAsync(forceDownload: forceDownload);
            await TryDownloadAndInstallUpdateAsync(updateInfo, bypassPortableCheck, progress);
        }

        public static async Task TryDownloadAndInstallUpdateAsync(
            AppUpdateInfo updateInfo,
            bool bypassPortableCheck = false,
            IProgress<double>? progress = null)
        {
            if (!bypassPortableCheck && AppDataPathHelper.IsPortableMode())
            {
                string portableHint = LanguageService.Instance["update_portable_mode_hint"]
                    ?? "Portable mode detected. Please download the installer or portable package from the Releases page.";
                MessageHelper.Warning(portableHint);
                UpdateHelper.OpenLatestReleasePage();
                return;
            }

            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.InstallerDownloadUrl))
            {
                MessageHelper.Warning(LanguageService.Instance["update_installer_not_found"]
                    ?? "Installer package not found in the latest release.");
                UpdateHelper.OpenLatestReleasePage();
                return;
            }

            try
            {
                string installerPath = UpdateHelper.GetInstallerDownloadPath(updateInfo.InstallerFileName);

                if (UpdateHelper.TryGetCachedInstallerPath(updateInfo, out string cachedInstallerPath))
                {
                    installerPath = cachedInstallerPath;
                    progress?.Report(100);
                }
                else
                {
                    MessageHelper.Info(LanguageService.Instance["downloading_ellipsis"] ?? "Downloading...");

                    await UpdateHelper.DownloadInstallerAsync(
                        updateInfo.InstallerDownloadUrl,
                        installerPath,
                        progress,
                        fallbackDownloadUrl: updateInfo.FallbackInstallerDownloadUrl);
                }

                string shutdownMessage = LanguageService.Instance["update_download_confirm_shutdown"]
                    ?? "Download complete. The application will exit and launch the installer wizard. Continue?";

                bool readyToInstall = await MessageHelper.ShowAsyncDialog(
                    shutdownMessage,
                    LanguageService.Instance["Cancel"] ?? "Cancel",
                    LanguageService.Instance["Confirm"] ?? "Confirm");

                if (!readyToInstall)
                {
                    MessageHelper.Info(LanguageService.Instance["update_installer_saved"]
                        ?? $"Installer saved to: {installerPath}");
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

                UpdateHelper.LaunchInstallerAndShutdown(installerPath);
            }
            catch (Exception downloadEx)
            {
                MessageHelper.Error(LanguageService.Instance["unknown_error_occurred"] + $": {downloadEx.Message}");
                UpdateHelper.OpenLatestReleasePage();
            }
        }
    }
}
