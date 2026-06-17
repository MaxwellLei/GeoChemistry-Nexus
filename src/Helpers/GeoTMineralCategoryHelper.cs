using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 加载并解析官方温压计标签多语言配置（GeoTMineralCategories.json）。
    /// </summary>
    public static class GeoTMineralCategoryHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private static GeoTMineralCategoryConfig? _cachedConfig;
        private static DateTime _cachedWriteTimeUtc;

        public static string LocalConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Plugins", OfficialContentEndpoints.GeoTMineralCategoriesFileName);

        public static GeoTMineralCategoryConfig LoadConfig()
        {
            if (!File.Exists(LocalConfigPath))
                return new GeoTMineralCategoryConfig();

            var writeTime = File.GetLastWriteTimeUtc(LocalConfigPath);
            if (_cachedConfig != null && writeTime == _cachedWriteTimeUtc)
                return _cachedConfig;

            try
            {
                string json = File.ReadAllText(LocalConfigPath);
                _cachedConfig = JsonSerializer.Deserialize<GeoTMineralCategoryConfig>(json, JsonOptions)
                                ?? new GeoTMineralCategoryConfig();
                _cachedWriteTimeUtc = writeTime;
            }
            catch
            {
                _cachedConfig = new GeoTMineralCategoryConfig();
                _cachedWriteTimeUtc = writeTime;
            }

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
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));
        }

        public static GeoTMineralCategoryConfig LoadConfigFromPath(string path)
        {
            if (!File.Exists(path))
                return new GeoTMineralCategoryConfig();

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<GeoTMineralCategoryConfig>(json, JsonOptions)
                       ?? new GeoTMineralCategoryConfig();
            }
            catch
            {
                return new GeoTMineralCategoryConfig();
            }
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
