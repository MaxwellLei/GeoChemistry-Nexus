using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// ScatterPropertyControl.xaml 的交互逻辑
    /// </summary>
    public partial class ScatterPropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(ScatterPropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public List<MarkerShapeItem> MarkerShapes => MarkerShapeHelper.GetMarkerShapes();

        public ScatterPropertyControl()
        {
            InitializeComponent();
        }
    }
}
