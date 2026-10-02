using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计类别（单矿物 / 矿物对 / 多矿物全岩多平衡）键与多语言显示名。
    /// </summary>
    public static class GeoTCategoryHelper
    {
        public const string DefaultCategoryKey = "single_mineral";

        private static readonly ResourceManager ResourceManager =
            new("GeoChemistryNexus.Data.Language.Language", typeof(LanguageService).Assembly);

        private static readonly string[] CategoryKeys =
        {
            "single_mineral",
            "mineral_pair",
            "multi_equilibrium"
        };

        private static readonly Dictionary<string, string> ResourceKeyMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["single_mineral"] = "geo_category_single_mineral",
                ["mineral_pair"] = "geo_category_mineral_pair",
                ["multi_equilibrium"] = "geo_category_multi_equilibrium"
            };

        public static IReadOnlyList<string> GetCategoryKeys() => CategoryKeys;

        public static string GetDisplayName(string categoryKey)
        {
            if (string.IsNullOrWhiteSpace(categoryKey))
                return categoryKey ?? string.Empty;

            string normalizedKey = NormalizeCategoryKey(categoryKey);
            if (ResourceKeyMap.TryGetValue(normalizedKey, out string? resourceKey))
            {
                string? localized = LanguageService.Instance[resourceKey];
                if (!string.IsNullOrWhiteSpace(localized))
                    return localized;
            }

            return categoryKey.Trim();
        }

        public static bool IsValidCategoryKey(string? categoryKey) => FindKey(categoryKey) != null;

        public static string NormalizeCategoryKey(string? categoryKey)
        {
            return FindKey(categoryKey) ?? DefaultCategoryKey;
        }

        private static string? FindKey(string? categoryKey)
        {
            if (string.IsNullOrWhiteSpace(categoryKey))
                return null;

            string key = categoryKey.Trim();
            foreach (string catKey in CategoryKeys)
            {
                if (string.Equals(catKey, key, StringComparison.OrdinalIgnoreCase))
                    return catKey;
            }

            foreach (string catKey in CategoryKeys)
            {
                if (!ResourceKeyMap.TryGetValue(catKey, out string? resourceKey))
                    continue;

                foreach (string langCode in AppCultureRegistry.AppUiCodes)
                {
                    try
                    {
                        string? localized = ResourceManager.GetString(resourceKey, new CultureInfo(langCode));
                        if (!string.IsNullOrWhiteSpace(localized)
                            && string.Equals(localized.Trim(), key, StringComparison.OrdinalIgnoreCase))
                        {
                            return catKey;
                        }
                    }
                    catch (CultureNotFoundException)
                    {
                        // ignore invalid culture
                    }
                }
            }

            return null;
        }
    }
}
