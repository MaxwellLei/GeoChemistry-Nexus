using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.PropertyEditor;
using HandyControl.Controls;
using ScottPlot;

namespace GeoChemistryNexus.Models
{
    public partial class LegendDefinition : ObservableObject
    {
        /// <summary>
        /// 图例位置
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Location")] // 位置
        private Alignment _alignment;

        /// <summary>
        /// 图例条目排列方式
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Orientation")] // 排列方式
        private Orientation _orientation;

        /// <summary>
        /// 图例是否可见
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Is Visible")] // 是否隐藏 (Translating "是否隐藏" directly would be "Is Hidden". "Is Visible" is the common and more intuitive opposite for this property's function.)
        private bool _isVisible;

        /// <summary>
        /// 图例是否可见
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")] // 样式
        [property: DisplayName("Font")] // 是否隐藏 (This comment seems to be a copy-paste error from the previous property. Assuming "Font" is the intended display name for the font property.)
        [property: Editor(typeof(FontFamilyPropertyEditor), typeof(PropertyEditorBase))]
        private string _font = "Arial";
    }
}