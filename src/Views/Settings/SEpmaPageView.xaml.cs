using GeoChemistryNexus.ViewModels;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// SEpmaPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SEpmaPageView : Page
    {
        private static SEpmaPageView instance = null!;

        public SEpmaPageView()
        {
            InitializeComponent();
            this.DataContext = new SEpmaPageViewModel();
        }

        public static SEpmaPageView GetPage()
        {
            if (instance == null)
            {
                instance = new SEpmaPageView();
            }
            return instance;
        }
    }
}
