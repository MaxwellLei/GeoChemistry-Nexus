using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.PropertyEditor;
using HandyControl.Controls;
using System.ComponentModel;

namespace GeoChemistryNexus.Models
{
    public partial class TitleDefinition : ObservableObject
    {
        /// <summary>
        /// 图表标题样式 标题内容
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Content")] // 内容
        [property: Editor(typeof(LocalizedStringPropertyEditor), typeof(PropertyEditorBase))]
        private LocalizedString _label = new LocalizedString();

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Font Family")] // 字体
        private string _family = "Arial";

        /// <summary>
        /// 图表标题样式 标题字体
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Font Size")] // 字体大小
        private float _size = 12;

        /// <summary>
        /// 图表标题样式 字体颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Font Color")] // 字体颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 图表标题样式 粗体样式
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Is Bold")] // 粗体
        private bool _isBold = false;

        /// <summary>
        /// 图表标题样式 斜体样式
        /// </summary>
        [ObservableProperty]
        [property: Category("Chart Title Style")] // 图表标题样式
        [property: DisplayName("Is Italic")] // 斜体
        private bool _isItalic = false;
    }
}