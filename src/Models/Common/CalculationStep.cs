using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 右侧检查器（计算详情侧边栏）的一行：名称 / 值 / 说明，或带折叠箭头的分组标题。
    /// 地质温压计与 EPMA 共用；由脚本的 calculateDetailed 返回对象映射而来。
    /// </summary>
    public class CalculationStep : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsResult { get; set; }
        public bool IsHighlight { get; set; }
        public bool IsSeparator { get; set; }

        /// <summary>
        /// 自定义背景色（#RGB / #RRGGBB / #AARRGGBB）；优先于语义高亮色。
        /// </summary>
        public string? BackgroundColor { get; set; }

        public bool HasCustomBackground => !string.IsNullOrWhiteSpace(BackgroundColor);

        private bool _isCollapsed;

        /// <summary>
        /// 分隔标题是否折叠其后续分组（仅 IsSeparator 有效）。
        /// </summary>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set => SetProperty(ref _isCollapsed, value);
        }

        private bool _isVisible = true;

        /// <summary>
        /// 是否因分组折叠而隐藏。
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
