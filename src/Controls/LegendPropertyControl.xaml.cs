using System.Windows;
using System.Windows.Controls;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.Controls
{
    public partial class LegendPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(LegendPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public LegendPropertyControl()
        {
            InitializeComponent();
        }
    }
}
