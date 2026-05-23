using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// SpiderSamplePropertyControl.xaml 的交互逻辑
    /// 蛛网图样品线属性控件
    /// </summary>
    public partial class SpiderSamplePropertyControl : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(SpiderSamplePropertyControl),
                new PropertyMetadata(null));

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public List<MarkerShapeItem> MarkerShapes => MarkerShapeHelper.GetMarkerShapes();

        public SpiderSamplePropertyControl()
        {
            InitializeComponent();
        }
    }
}
