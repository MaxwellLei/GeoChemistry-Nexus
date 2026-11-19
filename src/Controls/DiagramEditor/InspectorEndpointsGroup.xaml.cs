using GeoChemistryNexus.Models;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// “端点”分组：起点 / 终点两行坐标（X、Y + 在图上拾取）和标题栏的“交换起终点”按钮。箭头、线条面板共用。
    /// </summary>
    public partial class InspectorEndpointsGroup : UserControl
    {
        public static readonly DependencyProperty StartProperty =
            DependencyProperty.Register(nameof(Start), typeof(PointDefinition), typeof(InspectorEndpointsGroup),
                new PropertyMetadata(null));

        public static readonly DependencyProperty EndProperty =
            DependencyProperty.Register(nameof(End), typeof(PointDefinition), typeof(InspectorEndpointsGroup),
                new PropertyMetadata(null));

        public PointDefinition? Start
        {
            get => (PointDefinition?)GetValue(StartProperty);
            set => SetValue(StartProperty, value);
        }

        public PointDefinition? End
        {
            get => (PointDefinition?)GetValue(EndProperty);
            set => SetValue(EndProperty, value);
        }

        public InspectorEndpointsGroup()
        {
            InitializeComponent();
        }

        /// <summary>交换起点与终点的坐标（直接修改原 PointDefinition 实例，保持引用不变）。</summary>
        private void SwapEndpointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Start == null || End == null) return;

            (Start.X, End.X) = (End.X, Start.X);
            (Start.Y, End.Y) = (End.Y, Start.Y);
        }
    }
}
