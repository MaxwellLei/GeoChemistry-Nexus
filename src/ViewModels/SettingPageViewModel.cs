using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Views;
using System.Windows.Controls;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SettingPageViewModel : ObservableObject
    {
        private readonly Frame _nav;

        public SettingPageViewModel(Frame nav)
        {
            _nav = nav;
            nav.Navigate(SCommonPageView.GetPage());
        }

        [RelayCommand]
        private void CommonPage()
        {
            _nav.Navigate(SCommonPageView.GetPage());
        }

        [RelayCommand]
        private void PlotPage()
        {
            _nav.Navigate(SPlotPageView.GetPage());
        }

        [RelayCommand]
        private void ShortcutPage()
        {
            _nav.Navigate(SShortcutPageView.GetPage());
        }

        [RelayCommand]
        private void AboutPage()
        {
            _nav.Navigate(SAboutPageView.GetPage());
        }

        [RelayCommand]
        private void GeothermometerPage()
        {
            _nav.Navigate(SGeothermometerPageView.GetPage());
        }
    }
}
