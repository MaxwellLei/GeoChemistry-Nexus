using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// SpiderAxisPropertyControl.xaml 的交互逻辑
    /// 蛛网图坐标轴属性控件
    /// </summary>
    public partial class SpiderAxisPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(SpiderAxisPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public SpiderAxisPropertyControl()
        {
            InitializeComponent();
        }
    }
}
