using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System.ComponentModel;

namespace GeoChemistryNexus.Models
{
    public partial class TitleDefinition : ObservableObject
    {
        /// <summary>
        /// 图表标题样式 标题内容
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("content")] // 内容
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("font_family")] // 字体
        private string _family = "Arial";

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("font_size")] // 字体大小
        private float _size = 20;

        /// <summary>
        /// 图表标题样式 字体颜色
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("font_color")] // 字体颜色
        private string _color = "#000000";

        /// <summary>
        /// 图表标题样式 粗体样式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("is_bold")] // 粗体
        private bool _isBold = true;

        /// <summary>
        /// 图表标题样式 斜体样式
        /// </summary>
        [ObservableProperty]
        [property: LocalizedCategory("chart_title_style")] // 图表标题样式
        [property: LocalizedDisplayName("is_italic")] // 斜体
        private bool _isItalic = false;
    }
}