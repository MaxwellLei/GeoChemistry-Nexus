using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using GeoChemistryNexus.PropertyEditor;
using HandyControl.Controls;

namespace GeoChemistryNexus.Models
{
    public partial class LineDefinition : ObservableObject
    {
        /// <summary>
        /// 起始点
        /// </summary>
        [ObservableProperty]    
        [property: Category("Position")]      // 位置
        [property: DisplayName("Start Coordinates")]     // 起始坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _start = new PointDefinition();

        /// <summary>
        /// 终止点
        /// </summary>
        [ObservableProperty]
        [property: Category("Position")]      // 位置
        [property: DisplayName("End Coordinates")]      // 终止坐标
        [property: Editor(typeof(PointDefinitionPropertyEditor), typeof(PropertyEditorBase))]
        private PointDefinition _end = new PointDefinition();

        /// <summary>
        /// 颜色
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")]       // 样式
        [property: DisplayName("Color")]        // 颜色
        [property: Editor(typeof(ColorPropertyEditor), typeof(ColorPropertyEditor))]
        private string _color = "#000000";

        /// <summary>
        /// 宽度
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")]       // 样式
        [property: DisplayName("Width")]    // 宽度
        private float _width = 1.5f;


        /// <summary>
        /// 线条的样式，例如：虚线，点线等
        /// </summary>
        [ObservableProperty]
        [property: Category("Style")]       // 样式
        [property: DisplayName("Type")]     // 类型
        public LineType _style;

        // 枚举类型
        public enum LineType
        {
            Solid,
            Dash,
            DenselyDashed,
            Dot
        }
    }
}
