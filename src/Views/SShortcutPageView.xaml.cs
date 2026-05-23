using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SShortcutPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SShortcutPageView : Page
    {
        private static SShortcutPageView instance = null;

        public SShortcutPageView()
        {
            InitializeComponent();
            this.DataContext = new SShortcutPageViewModel();
        }

        public static SShortcutPageView GetPage()
        {
            if (instance == null)
            {
                instance = new SShortcutPageView();
            }
            return instance;
        }
    }
}
