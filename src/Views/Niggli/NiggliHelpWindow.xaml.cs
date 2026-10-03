using System.Windows;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// NiggliHelpWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NiggliHelpWindow : Window
    {
        public NiggliHelpWindow()
        {
            InitializeComponent();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
