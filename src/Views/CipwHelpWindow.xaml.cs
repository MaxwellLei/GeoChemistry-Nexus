using System.Windows;
using GeoChemistryNexus.Helpers;

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
            UiScaleHelper.Attach(this);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
