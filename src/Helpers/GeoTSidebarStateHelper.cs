using System;
using System.Collections.Generic;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 温压计侧栏展开/选中状态持久化
    /// </summary>
    public static class GeoTSidebarStateHelper
    {
        private const string ConfigKey = "geot_sidebar_state";

        public static GeoTSidebarState Load()
        {
            try
            {
                string? json = ConfigHelper.GetConfig(ConfigKey);
                if (string.IsNullOrWhiteSpace(json))
                    return new GeoTSidebarState();

                var state = JsonHelper.Deserialize<GeoTSidebarState>(json)
                            ?? new GeoTSidebarState();
                state.ExpandedCategoryKeys ??= new List<string>();
                return state;
            }
            catch
            {
                return new GeoTSidebarState();
            }
        }

        public static void Save(GeoTSidebarState state)
        {
            try
            {
                string json = JsonHelper.Serialize(state);
                ConfigHelper.SetConfig(ConfigKey, json);
            }
            catch
            {
                // 持久化失败不影响主流程
            }
        }
    }

    public class GeoTSidebarState
    {
        /// <summary>
        /// 当前展开的一级区块：Favorite / Official / Custom
        /// </summary>
        public string? ExpandedSection { get; set; }

        /// <summary>
        /// 已展开的二级类别键，格式为 "{SectionKey}:{CategoryKey}"
        /// </summary>
        public List<string> ExpandedCategoryKeys { get; set; } = new();

        /// <summary>
        /// 上次选中的温压计 PluginId（已弃用，仅兼容旧配置）
        /// </summary>
        public string? SelectedPluginId { get; set; }
    }

    public static class GeoTSidebarSectionKeys
    {
        public const string Favorite = "Favorite";
        public const string Official = "Official";
        public const string Custom = "Custom";
    }
}
