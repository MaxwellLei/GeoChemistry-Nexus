using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using unvell.ReoGrid;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermomrNewPageView.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerNewPageView : Page
    {
        private static GeothermometerNewPageView commonPage = null;

        public GeothermometerNewPageView()
        {
            InitializeComponent();
            var viewModel = new GeothermometerNewPageViewModel();
            this.DataContext = viewModel;

            // 将 RichTextBox 控件的引用传递给 ViewModel
            viewModel.SetHelpRichTextBox(this.HelpRichTextBox);
        }

        public static Page GetPage()
        {
            if (commonPage == null)
            {
                commonPage = new GeothermometerNewPageView();
            }
            return commonPage;
        }

    }
}
