using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 顶端 / 右侧图框边的属性面板，仅显示轴线属性
    /// </summary>
    public partial class FrameAxisPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(FrameAxisPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public FrameAxisPropertyControl()
        {
            InitializeComponent();
        }
    }
}
