using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 计算详情步骤（<see cref="CalculationStep"/>）的公共处理：多语言取值、颜色规范化、分组折叠可见性，
    /// 以及把脚本返回的 JSON 数组映射为步骤列表。地质温压计与 EPMA 的检查器共用。
    /// 脚本约定的字段：name / nameLang、value、desc / descLang、isResult、isHighlight、isSeparator、collapsed、backgroundColor。
    /// </summary>
    public static class CalculationStepHelper
    {
        /// <summary>单次返回的步骤数量上限，避免脚本撑爆检查器。</summary>
        public const int MaximumSteps = 512;

        /// <summary>
        /// 把脚本返回的 JSON 数组映射为步骤列表（未知字段忽略）。
        /// </summary>
        /// <param name="array">脚本返回值，必须是对象数组。</param>
        /// <param name="formatNumber">数字值的格式化方式；为空时按往返精度输出。</param>
        public static List<CalculationStep> FromJson(JsonElement array, Func<double, string>? formatNumber = null)
        {
            if (array.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("calculateDetailed must return an array.");
            if (array.GetArrayLength() > MaximumSteps)
                throw new InvalidOperationException($"calculateDetailed returned more than {MaximumSteps} steps.");

            var steps = new List<CalculationStep>(array.GetArrayLength());
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    throw new InvalidOperationException("Each calculateDetailed step must be an object.");

                var step = new CalculationStep
                {
                    Name = ResolveText(item, "name", "nameLang"),
                    Value = FormatValue(item, formatNumber),
                    Description = ResolveText(item, "desc", "descLang"),
                    IsResult = GetBoolean(item, "isResult"),
                    IsHighlight = GetBoolean(item, "isHighlight"),
                    IsSeparator = GetBoolean(item, "isSeparator"),
                    IsCollapsed = GetBoolean(item, "collapsed")
                };

                if (item.TryGetProperty("backgroundColor", out var color) && color.ValueKind == JsonValueKind.String
                    && TryNormalizeHexColor(color.GetString(), out string hex))
                    step.BackgroundColor = hex;

                steps.Add(step);
            }

            ApplyGroupVisibility(steps);
            return steps;
        }

        /// <summary>
        /// 按分隔标题的折叠状态，刷新组内行的可见性；第一个分隔标题之前的行始终可见。
        /// </summary>
        public static void ApplyGroupVisibility(IList<CalculationStep> steps)
        {
            if (steps == null || steps.Count == 0)
                return;

            bool hiding = false;
            foreach (var step in steps)
            {
                if (step.IsSeparator)
                {
                    step.IsVisible = true;
                    hiding = step.IsCollapsed;
                    continue;
                }

                step.IsVisible = !hiding;
            }
        }

        /// <summary>
        /// 规范化十六进制颜色：支持 #RGB / #RRGGBB / #AARRGGBB，可省略 #。
        /// </summary>
        public static bool TryNormalizeHexColor(string? input, out string hex)
        {
            hex = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string s = input.Trim();
            if (!s.StartsWith("#", StringComparison.Ordinal))
                s = "#" + s;

            if (Regex.IsMatch(s, @"^#[0-9A-Fa-f]{3}$"))
            {
                hex = $"#{s[1]}{s[1]}{s[2]}{s[2]}{s[3]}{s[3]}";
                return true;
            }

            if (Regex.IsMatch(s, @"^#[0-9A-Fa-f]{6}$") || Regex.IsMatch(s, @"^#[0-9A-Fa-f]{8}$"))
            {
                hex = s;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按当前界面语言从 { 语言代码: 文本 } 字典取值，未命中时回退默认文本。
        /// </summary>
        public static string ResolveLocalized(IReadOnlyDictionary<string, string> translations, string defaultText)
        {
            if (translations.Count == 0)
                return defaultText;

            var map = new Dictionary<string, string>(translations, StringComparer.OrdinalIgnoreCase);
            string requested = LanguageService.CurrentLanguage;
            if (AppCultureRegistry.TryNormalize(requested, out string normalized) &&
                map.TryGetValue(normalized, out string? localized) &&
                !string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            if (map.TryGetValue(requested, out string? exact) && !string.IsNullOrEmpty(exact))
                return exact;

            return defaultText;
        }

        private static string ResolveText(JsonElement item, string key, string langKey)
        {
            string defaultText = item.TryGetProperty(key, out var text) && text.ValueKind == JsonValueKind.String
                ? text.GetString() ?? string.Empty
                : string.Empty;

            if (!item.TryGetProperty(langKey, out var lang) || lang.ValueKind != JsonValueKind.Object)
                return defaultText;

            var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in lang.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(property.Value.GetString()))
                    translations[property.Name] = property.Value.GetString()!;
            }

            return ResolveLocalized(translations, defaultText);
        }

        private static string FormatValue(JsonElement item, Func<double, string>? formatNumber)
        {
            if (!item.TryGetProperty("value", out var value))
                return string.Empty;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => formatNumber != null
                    ? formatNumber(value.GetDouble())
                    : value.GetDouble().ToString("R", CultureInfo.InvariantCulture),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Undefined or JsonValueKind.Null => string.Empty,
                _ => value.GetRawText()
            };
        }

        private static bool GetBoolean(JsonElement item, string key)
            => item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.True;
    }
}
