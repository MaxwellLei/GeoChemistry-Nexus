using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 外部链接（网页 / mailto）打开帮助类，失败时统一给出提示。
    /// </summary>
    public static class ExternalLinkHelper
    {
        public static void OpenUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageHelper.Warning((LanguageService.Instance["unable_to_open_link"] ?? "无法打开链接：") + ex.Message);
            }
        }
    }
}
