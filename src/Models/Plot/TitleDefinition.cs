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
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        private string _family = "Arial";

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        private float _size = 18;

        /// <summary>
        /// 图表标题样式 字体颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 图表标题样式 粗体样式
        /// </summary>
        [ObservableProperty]
        private bool _isBold = true;

        /// <summary>
        /// 图表标题样式 斜体样式
        /// </summary>
        [ObservableProperty]
        private bool _isItalic = false;
    }
}