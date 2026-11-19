using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 线条属性面板。端点坐标 / 交换 / 拾取由 <see cref="InspectorEndpointsGroup"/> 与 <see cref="PointDefinitionControl"/> 负责，
    /// 颜色 / 宽度 / 线型由 <see cref="InspectorStrokeEditor"/> 负责。
    /// </summary>
    public partial class LinePropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(LinePropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public LinePropertyControl()
        {
            InitializeComponent();
        }
    }
}
