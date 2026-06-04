using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 蜘蛛图坐标轴定义，包含元素顺序和标准化方案等特殊属性
    /// </summary>
    public partial class SpiderAxisDefinition : CartesianAxisDefinition
    {
        public override bool ShowAxisStyleOptions => false;

        public override bool ShowMajorTickStyleOptions => false;

        public override bool ShowMinorTickStyleOptions => false;

        /// <summary>
        /// 蛛网图类型：REE 或 TraceElement
        /// </summary>
        [ObservableProperty]
        private string _spiderType = "REE";

        /// <summary>
        /// 元素显示顺序（从左到右）
        /// </summary>
        [ObservableProperty]
        private string _elementOrder = "";

        /// <summary>
        /// 当前选中的标准化方案名称
        /// </summary>
        [ObservableProperty]
        private string _normalizationStandard = "";

        /// <summary>
        /// 是否启用标准化（默认启用）
        /// </summary>
        [ObservableProperty]
        private bool _isNormalizationEnabled = true;
    }
}
