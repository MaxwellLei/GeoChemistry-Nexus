using System.Collections.Generic;
using System.Linq;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 图解 / 温压计数据表格默认初始行数（设置项与运行时读取共用）。
    /// </summary>
    public static class WorksheetDefaultsHelper
    {
        public const string DiagramConfigKey = "diagram_default_worksheet_row_count";
        public const string GtmConfigKey = "gtm_default_worksheet_row_count";
        public const int FallbackRowCount = 500;

        public static readonly IReadOnlyList<int> AllowedRowCounts = new[] { 100, 200, 500, 1000, 2000, 5000 };

        public static int GetDefaultRowCount(string configKey)
        {
            if (int.TryParse(ConfigHelper.GetConfig(configKey), out int rowCount)
                && AllowedRowCounts.Contains(rowCount))
            {
                return rowCount;
            }

            return FallbackRowCount;
        }

        public static void SaveDefaultRowCount(string configKey, int rowCount)
        {
            if (!AllowedRowCounts.Contains(rowCount))
                rowCount = FallbackRowCount;

            ConfigHelper.SetConfig(configKey, rowCount.ToString());
        }
    }
}
