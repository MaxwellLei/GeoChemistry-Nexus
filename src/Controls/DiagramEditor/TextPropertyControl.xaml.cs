using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class TextPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(TextPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public TextPropertyControl()
        {
            InitializeComponent();
        }
    }
}
