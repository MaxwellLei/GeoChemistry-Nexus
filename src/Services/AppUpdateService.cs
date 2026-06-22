using GeoChemistryNexus.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Services
{
    /// <summary>
    /// 启动时自动检查程序更新（仅通知，不自动下载安装）。
    /// </summary>
    public static class AppUpdateService
    {
        private static bool _hasChecked;

        public static async Task TryAutoCheckOnStartupAsync()
        {
            if (_hasChecked)
                return;

            _hasChecked = true;

            if (!bool.TryParse(ConfigHelper.GetConfig("auto_check_update"), out bool autoCheck) || !autoCheck)
                return;

            if (AppDataPathHelper.IsPortableMode() || AppDataPathHelper.IsDevMode())
                return;

            try
            {
                var info = await UpdateHelper.GetLatestReleaseInfoAsync();
                if (!info.HasUpdate)
                    return;

                string message = LanguageService.Instance["update_auto_check_notify"]
                    ?? $"New version {info.LatestVersion} is available. Open Settings to download and install.";

                MessageHelper.Info(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppUpdateService] Auto check failed: {ex.Message}");
            }
        }
    }
}
