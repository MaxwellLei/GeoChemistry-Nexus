using GeoChemistryNexus.ViewModels;
using HandyControl.Controls;
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

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// MainPlot.xaml 的交互逻辑
    /// </summary>
    public partial class MainPlotPage : Page
    {
        private static MainPlotPage homePage = null;

        public MainPlotPage()
        {
            InitializeComponent();
            // 链接 ViewModel
            this.DataContext = new MainPlotViewModel(this.WpfPlot1,this.Drichtextbox);
        }

        public static MainPlotPage GetPage()
        {
            if (homePage == null)
            {
                homePage = new MainPlotPage();
            }
            return homePage;
        }

        private void Color_Pick(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker.IsOpen = true;
        }

        private void Color_Pick0000(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker0000.IsOpen = true;
        }

        private void Color_Pick000(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker000.IsOpen = true;
        }

        private void Color_Pick00(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker00.IsOpen = true;
        }

        private void Color_Pick0(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker0.IsOpen = true;
        }

        private void Color_Pick1(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker1.IsOpen = true;
        }

        private void Color_Pick2(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker2.IsOpen = true;
        }

        private void Color_Pick3(object sender, RoutedEventArgs e)
        {
            // Show the popup
            ColorPicker3.IsOpen = true;
        }

    }
}
