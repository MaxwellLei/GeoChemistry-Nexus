using System.Collections.Concurrent;
using System.Linq;
using GeoChemistryNexus.Models;
using SkiaSharp;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解文本对象的字体自适应：根据文本内容挑选能显示全部字符的字体，避免中文、上下标等字符显示为方块。
    /// 统一封装 <see cref="ScottPlot.Fonts.Detect(string)"/>，并处理“文本为空”“当前字体已可用”等情况。
    /// </summary>
    public static class DiagramFontHelper
    {
        private static readonly ConcurrentDictionary<(string Font, string Text), string> ResolvedCache = new();

        /// <summary>
        /// 按文本内容检测字体；文本为空时保持当前字体不变。
        /// </summary>
        public static string Detect(string? text, string? currentFont = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.IsNullOrWhiteSpace(currentFont) ? ScottPlot.Fonts.Default : currentFont;
            }

            return ScottPlot.Fonts.Detect(text);
        }

        /// <summary>
        /// 坐标轴标题字体：同时考虑标题与副标题（副标题与标题共用同一个字体）。
        /// </summary>
        public static string DetectAxisFamily(BaseAxisDefinition axis, ContentLanguageContext? context)
        {
            string text = axis.Label?.Get(context) ?? string.Empty;

            string? subLabel = axis switch
            {
                CartesianAxisDefinition cartesian => cartesian.SubLabel?.Get(context),
                TernaryAxisDefinition ternary => ternary.SubLabel?.Get(context),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(subLabel))
            {
                text += subLabel;
            }

            return Detect(text, axis.Family);
        }

        /// <summary>
        /// 首选字体能显示该文本时沿用首选字体，否则按文本内容检测字体。用于图例条目这类内容来自数据、
        /// 字体由用户在属性面板里指定的文本：既尊重用户选择，又避免字符缺失。
        /// </summary>
        public static string Resolve(string? preferredFont, string? text)
        {
            string preferred = string.IsNullOrWhiteSpace(preferredFont) ? ScottPlot.Fonts.Default : preferredFont;

            if (string.IsNullOrWhiteSpace(text) || text.All(c => c < 0x80))
            {
                return preferred;
            }

            return ResolvedCache.GetOrAdd((preferred, text), key =>
            {
                string visibleText = string.Concat(key.Text.Where(c => !char.IsControl(c)));
                using var typeface = SKTypeface.FromFamilyName(key.Font);
                return typeface != null && typeface.ContainsGlyphs(visibleText)
                    ? key.Font
                    : ScottPlot.Fonts.Detect(visibleText);
            });
        }
    }
}
