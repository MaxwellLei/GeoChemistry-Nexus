using GeoChemistryNexus.ViewModels;
using MahApps.Metro.Controls;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeoChemistryNexus
{
    /// <summary>
    /// 主窗体，啥也不是
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            // 链接 ViewModel
            this.DataContext = new MainWindowViewModel();
            // 初始化窗体
            InitializeComponent();

            var ta = WpfPlot1.Plot.Add.TriangularAxis();

            var regionPoints = new Coordinates[]
            {
                ta.GetCoordinates(0.2, 0.2),
                ta.GetCoordinates(0.8, 0.2),
                ta.GetCoordinates(0.5, 0.8),
                ta.GetCoordinates(0.2, 0.2) // 封闭区域，最后点回到起点
            };

            // 绘制区域
            var region = WpfPlot1.Plot.Add.Polygon(regionPoints);
            region.LineWidth = 2;

            Coordinates[] points = new Coordinates[]
            {
                ta.GetCoordinates(0.50, 0.40),
                ta.GetCoordinates(0.60, 0.40),
                ta.GetCoordinates(0.65, 0.50),
            };


            WpfPlot1.Plot.Add.Markers(points, MarkerShape.FilledCircle, 10, ScottPlot.Colors.Red);
        }
    }
}
