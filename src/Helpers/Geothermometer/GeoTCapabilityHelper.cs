using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计能力标签（P / T / fO2 及自定义简写）规范化与展示。
    /// </summary>
    public static class GeoTCapabilityHelper
    {
        public const int MaxLength = 12;

        public static readonly string[] BuiltInCapabilities = { "P", "T", "fO2" };

        private static readonly Dictionary<string, string> AliasToCanonical =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["P"] = "P",
                ["Pressure"] = "P",
                ["压力"] = "P",

                ["T"] = "T",
                ["Temperature"] = "T",
                ["Temp"] = "T",
                ["温度"] = "T",

                ["fO2"] = "fO2",
                ["FO2"] = "fO2",
                ["fo2"] = "fO2",
                ["fO₂"] = "fO2",
                ["oxygen fugacity"] = "fO2",
                ["氧逸度"] = "fO2"
            };

        private static readonly Dictionary<string, string> TooltipResourceKeys =
            new(StringComparer.Ordinal)
            {
                ["P"] = "geo_capability_tooltip_p",
                ["T"] = "geo_capability_tooltip_t",
                ["fO2"] = "geo_capability_tooltip_fo2"
            };

        public static IReadOnlyList<string> GetBuiltInCapabilities() => BuiltInCapabilities;

        public static bool IsBuiltIn(string? capability)
        {
            string? canonical = TryGetBuiltInCanonical(capability);
            return canonical != null;
        }

        /// <summary>
        /// 规范化单个能力标签；无效或空白时返回 null。
        /// 内置别名归一到 P / T / fO2；自定义保留去空白后的原文（长度受限）。
        /// </summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string trimmed = raw.Trim();
            if (TryGetBuiltInCanonical(trimmed) is string builtIn)
                return builtIn;

            if (trimmed.Length > MaxLength)
                trimmed = trimmed.Substring(0, MaxLength);

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        /// <summary>
        /// 规范化列表：去重（忽略大小写）、内置在前按固定顺序、自定义按显示名排序。
        /// </summary>
        public static List<string> NormalizeList(IEnumerable<string>? raw)
        {
            if (raw == null)
                return new List<string>();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var builtIns = new List<string>();
            var customs = new List<string>();

            foreach (var item in raw)
            {
                string? normalized = Normalize(item);
                if (normalized == null || !seen.Add(normalized))
                    continue;

                if (IsBuiltIn(normalized))
                    builtIns.Add(normalized);
                else
                    customs.Add(normalized);
            }

            var orderedBuiltIns = BuiltInCapabilities
                .Where(b => builtIns.Any(x => string.Equals(x, b, StringComparison.Ordinal)))
                .ToList();

            customs.Sort(StringComparer.CurrentCultureIgnoreCase);
            orderedBuiltIns.AddRange(customs);
            return orderedBuiltIns;
        }

        public static string GetDisplayName(string? capability)
        {
            string? normalized = Normalize(capability);
            return normalized ?? (capability?.Trim() ?? string.Empty);
        }

        public static string GetTooltip(string? capability)
        {
            string? normalized = Normalize(capability);
            if (normalized == null)
                return string.Empty;

            if (TooltipResourceKeys.TryGetValue(normalized, out string? resourceKey))
            {
                string? localized = LanguageService.Instance[resourceKey];
                if (!string.IsNullOrWhiteSpace(localized))
                    return localized;
            }

            return normalized;
        }

        /// <summary>
        /// 下拉建议：尚未选用的内置能力。
        /// </summary>
        public static List<string> GetBuiltInSuggestions(IEnumerable<string>? selected)
        {
            var selectedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in selected ?? Enumerable.Empty<string>())
            {
                string? normalized = Normalize(item);
                if (!string.IsNullOrEmpty(normalized))
                    selectedSet.Add(normalized);
            }

            return BuiltInCapabilities
                .Where(b => !selectedSet.Contains(b))
                .ToList();
        }

        private static string? TryGetBuiltInCanonical(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string trimmed = raw.Trim();
            if (AliasToCanonical.TryGetValue(trimmed, out string? canonical))
                return canonical;

            foreach (string builtIn in BuiltInCapabilities)
            {
                if (string.Equals(builtIn, trimmed, StringComparison.OrdinalIgnoreCase))
                    return builtIn;
            }

            return null;
        }
    }
}
