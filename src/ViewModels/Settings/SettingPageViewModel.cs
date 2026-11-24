using System;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Views;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SettingPageViewModel : ObservableObject
    {
        private readonly Frame _nav;

        public SettingPageViewModel(Frame nav)
        {
            _nav = nav;
            NavigateTo(SCommonPageView.GetPage());
        }

        private void NavigateTo(object pageContent)
        {
            if (_nav == null || ReferenceEquals(_nav.Content, pageContent))
                return;

            _nav.Navigate(pageContent);
            _nav.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                while (_nav.CanGoBack)
                {
                    _nav.RemoveBackEntry();
                }
            }));
        }

        [RelayCommand]
        private void CommonPage()
        {
            NavigateTo(SCommonPageView.GetPage());
        }

        [RelayCommand]
        private void PlotPage()
        {
            NavigateTo(SPlotPageView.GetPage());
        }

        [RelayCommand]
        private void ShortcutPage()
        {
            NavigateTo(SShortcutPageView.GetPage());
        }

        [RelayCommand]
        private void AboutPage()
        {
            NavigateTo(SAboutPageView.GetPage());
        }

        [RelayCommand]
        private void GeothermometerPage()
        {
            NavigateTo(SGeothermometerPageView.GetPage());
        }

        [RelayCommand]
        private void EpmaPage()
        {
            NavigateTo(SEpmaPageView.GetPage());
        }
    }
}
