using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class TernaryAxisPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(TernaryAxisPropertyControl),
                new PropertyMetadata(null, OnSelectedObjectChanged));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public TernaryAxisPropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 换到另一条边时回到标题页。
        /// </summary>
        private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TernaryAxisPropertyControl control && control.TabTitle != null)
            {
                control.TabTitle.IsChecked = true;
            }
        }
    }
}
