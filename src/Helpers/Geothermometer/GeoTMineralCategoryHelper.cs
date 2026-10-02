using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 加载并解析官方温压计标签多语言配置（GeoTMineralCategories.json）。
    /// </summary>
    public static class GeoTMineralCategoryHelper
    {
        private static GeoTMineralCategoryConfig? _cachedConfig;
        private static DateTime _cachedWriteTimeUtc;

        public static string LocalConfigPath =>
            AppDataPathHelper.GetDataPath("Plugins", OfficialContentEndpoints.GeoTMineralCategoriesFileName);

        public static string PublisherConfigPath =>
            AppDataPathHelper.GetDataPath("Config", "GeoTMineralCategories.publisher.json");

        /// <summary>
        /// 发布器工作副本：优先本地草稿，其次运行时目录文件。
        /// </summary>
        public static GeoTMineralCategoryConfig LoadPublisherConfig()
        {
            return PublisherConfigHelper.LoadPublisherConfig<GeoTMineralCategoryConfig>(
                PublisherConfigPath,
                LocalConfigPath);
        }

        public static void SavePublisherConfig(GeoTMineralCategoryConfig config)
        {
            PublisherConfigHelper.SavePublisherConfig(config, PublisherConfigPath);
        }

        /// <summary>
        /// 导出/发布时使用的分类文件路径。
        /// </summary>
        public static string ResolveExportConfigPath()
        {
            return PublisherConfigHelper.ResolveExportConfigPath(PublisherConfigPath, LocalConfigPath);
        }

        public static GeoTMineralCategoryConfig LoadConfig()
        {
            string path = LocalConfigPath;
            if (!File.Exists(path))
            {
                // 开发/便携环境下 Local 与 Bundled 可能同路径；安装版则回退到程序目录随包文件
                string bundled = AppDataPathHelper.GetBundledDataPath(
                    "Plugins", OfficialContentEndpoints.GeoTMineralCategoriesFileName);
                if (!File.Exists(bundled))
                    return new GeoTMineralCategoryConfig();
                path = bundled;
            }

            var writeTime = File.GetLastWriteTimeUtc(path);
            if (_cachedConfig != null && writeTime == _cachedWriteTimeUtc)
                return _cachedConfig;

            _cachedConfig = PublisherConfigHelper.LoadFromPath<GeoTMineralCategoryConfig>(path);
            _cachedWriteTimeUtc = writeTime;
            return _cachedConfig;
        }

        public static void InvalidateCache()
        {
            _cachedConfig = null;
            _cachedWriteTimeUtc = default;
        }

        /// <summary>
        /// 按当前界面语言解析标签显示名；未命中翻译表时返回原始名称。
        /// </summary>
        public static string GetDisplayName(string mineralKey)
        {
            if (string.IsNullOrWhiteSpace(mineralKey))
                return mineralKey ?? string.Empty;

            var entry = FindEntry(mineralKey);
            if (entry == null)
                return mineralKey.Trim();

            string localized = AppCultureRegistry.GetLocalizedValue(
                entry,
                LanguageService.CurrentLanguage,
                AppCultureRegistry.DefaultAppLanguage);

            return string.IsNullOrWhiteSpace(localized) ? mineralKey.Trim() : localized;
        }

        public static Dictionary<string, string>? FindEntry(string mineralKey)
        {
            if (string.IsNullOrWhiteSpace(mineralKey))
                return null;

            string key = mineralKey.Trim();
            foreach (var entry in LoadConfig().Minerals)
            {
                if (entry == null)
                    continue;

                foreach (var value in entry.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value)
                        && string.Equals(value.Trim(), key, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取全部标签建议项（用于编辑器自动补全）。
        /// </summary>
        public static List<Dictionary<string, string>> GetAllTagSuggestions()
        {
            return LoadConfig().Minerals
                .Where(entry => entry != null)
                .ToList();
        }

        /// <summary>
        /// 确保官方标签均存在于配置中（发布导出时使用，缺失项以 zh-CN 填充）。
        /// </summary>
        public static GeoTMineralCategoryConfig MergeMissingMinerals(
            GeoTMineralCategoryConfig config,
            IEnumerable<string> mineralNames)
        {
            config ??= new GeoTMineralCategoryConfig();
            config.Minerals ??= new List<Dictionary<string, string>>();

            foreach (string mineral in mineralNames.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (FindEntryInConfig(config, mineral) != null)
                    continue;

                config.Minerals.Add(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["zh-CN"] = mineral.Trim(),
                    ["en-US"] = mineral.Trim()
                });
            }

            return config;
        }

        public static void SaveConfig(GeoTMineralCategoryConfig config, string path)
        {
            PublisherConfigHelper.SaveToPath(config, path);

            if (string.Equals(path, LocalConfigPath, StringComparison.OrdinalIgnoreCase))
                InvalidateCache();
        }

        public static GeoTMineralCategoryConfig LoadConfigFromPath(string path)
        {
            return PublisherConfigHelper.LoadFromPath<GeoTMineralCategoryConfig>(path);
        }

        private static Dictionary<string, string>? FindEntryInConfig(GeoTMineralCategoryConfig config, string mineralKey)
        {
            if (string.IsNullOrWhiteSpace(mineralKey))
                return null;

            string key = mineralKey.Trim();
            foreach (var entry in config.Minerals)
            {
                if (entry == null)
                    continue;

                foreach (var value in entry.Values)
                {
                    if (!string.IsNullOrWhiteSpace(value)
                        && string.Equals(value.Trim(), key, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }
    }
}
