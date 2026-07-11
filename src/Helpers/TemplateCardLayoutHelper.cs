using System;
using System.Windows;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解模板卡片网格尺寸计算。
    /// 先按标准档确定列数，紧凑档再固定多 1 列，保证两种档位在常见宽度下始终有可见差异。
    /// </summary>
    public static class TemplateCardLayoutHelper
    {
        /// <summary>标准档偏好单元格边长（含卡片外边距）。</summary>
        public const double StandardPreferredSize = 280;

        /// <summary>单元格下限；紧凑多一列后若低于此值则不再加列。</summary>
        public const double MinCellSize = 160;

        /// <summary>单元格上限；超宽时标准档继续加列填满行宽。</summary>
        public const double MaxCellSize = 360;

        public const double DefaultCellSize = StandardPreferredSize;

        public static Size ComputeItemSize(double availableWidth, TemplateCardSizePreset preset)
        {
            if (availableWidth <= 0 || double.IsNaN(availableWidth) || double.IsInfinity(availableWidth))
                return new Size(DefaultCellSize, DefaultCellSize);

            int columns = ComputeStandardColumns(availableWidth);

            if (preset == TemplateCardSizePreset.Compact)
            {
                int compactColumns = columns + 1;
                if (availableWidth / compactColumns >= MinCellSize)
                    columns = compactColumns;
            }

            double cell = availableWidth / columns;
            if (cell < 1)
                cell = 1;

            return new Size(cell, cell);
        }

        /// <summary>
        /// 标准档列数：按偏好边长取最大可容纳列数，再因超宽上限继续加列。
        /// </summary>
        private static int ComputeStandardColumns(double availableWidth)
        {
            int columns = Math.Max(1, (int)Math.Floor(availableWidth / StandardPreferredSize));

            while (availableWidth / columns > MaxCellSize)
                columns++;

            return columns;
        }

        public static TemplateCardSizePreset ParseSizePreset(string? value)
        {
            if (string.Equals(value, nameof(TemplateCardSizePreset.Compact), StringComparison.OrdinalIgnoreCase)
                || value == "0")
            {
                return TemplateCardSizePreset.Compact;
            }

            return TemplateCardSizePreset.Standard;
        }

        public static TemplateCardLayoutSettings LoadFromConfig()
        {
            return new TemplateCardLayoutSettings
            {
                SizePreset = ParseSizePreset(ConfigHelper.GetConfig("template_card_size_preset"))
            };
        }

        public static void SaveToConfig(TemplateCardSizePreset preset)
        {
            ConfigHelper.SetConfig("template_card_size_preset", preset.ToString());
        }
    }
}
