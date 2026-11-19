using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 箭头属性面板。端点坐标 / 交换 / 拾取由 <see cref="InspectorEndpointsGroup"/> 与 <see cref="PointDefinitionControl"/> 负责。
    /// </summary>
    public partial class ArrowPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ArrowPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public ArrowPropertyControl()
        {
            InitializeComponent();
        }
    }
}
