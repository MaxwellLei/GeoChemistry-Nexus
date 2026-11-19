using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class AxisPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(AxisPropertyControl),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public AxisPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 切换到另一个坐标轴时回到第一个标签页，避免上一个轴的标签页在新轴上不可用（如范围 / 刻度）。
        /// </summary>
        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AxisPropertyControl control && control.TabTitle != null)
            {
                control.TabTitle.IsChecked = true;
            }
        }
    }
}
