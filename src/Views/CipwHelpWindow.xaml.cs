using System.Windows;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// CipwHelpWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CipwHelpWindow : Window
    {
        public CipwHelpWindow()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
