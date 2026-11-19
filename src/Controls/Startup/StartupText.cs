using GeoChemistryNexus.Services;
using System;
using System.Globalization;
using System.Linq;

namespace GeoChemistryNexus.Controls.Startup
{
    public enum StartupStatus
    {
        Starting, InitializingConfiguration, LoadingInterface, InitializingData, LoadingFonts,
        ReadingConfiguration, LoadingDiagrams, LoadingGeothermobarometers, Ready, EnteringMainWindow, Completed
    }

    /// <summary>
    /// 启动动画文案。统一从 <see cref="LanguageService"/>（Data/Language/*.resx，键以 startup_ 开头）读取，
    /// 与主界面共用同一套多语言资源。调用前需已执行 <see cref="LanguageService.InitializeLanguage"/>。
    /// </summary>
    internal sealed record StartupText(string LanguageCode, string[] Stages, string[] Minerals,
        string[] Headings, string[] Sites, string[] Trends, string Tagline, string Features, string[] Statuses)
    {
        public CultureInfo Culture => CultureInfo.GetCultureInfo(LanguageCode);
        public string FontFamily => LanguageCode switch
        {
            "ja-JP" => "Yu Gothic UI, Segoe UI, Microsoft YaHei UI",
            "ko-KR" => "Malgun Gothic, Segoe UI, Microsoft YaHei UI",
            "zh-CN" or "zh-TW" => "Microsoft YaHei UI, Segoe UI",
            _ => "Segoe UI, Microsoft YaHei UI"
        };
        public string Status(StartupStatus status) => Statuses[(int)status];

        /// <summary>按 LanguageService 当前语言读取启动文案。</summary>
        public static StartupText Create()
        {
            var language = LanguageService.Instance;
            string[] Many(string prefix, int count) =>
                Enumerable.Range(0, count).Select(i => language[$"{prefix}_{i}"]).ToArray();

            return new StartupText(
                LanguageService.CurrentLanguage,
                Many("startup_stage", 5),
                Many("startup_mineral", 4),
                Many("startup_heading", 4),
                Many("startup_site", 2),
                Many("startup_trend", 2),
                language["startup_tagline"],
                language["startup_features"],
                Enum.GetNames<StartupStatus>().Select(name => language["startup_status_" + name]).ToArray());
        }
    }
}
