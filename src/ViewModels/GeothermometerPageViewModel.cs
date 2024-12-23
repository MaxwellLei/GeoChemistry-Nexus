using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Views.Geothermometer;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? currentView;


        // 锆石微量 - Zr 温度计
        [RelayCommand]
        public void ZirconZr()
        {
            CurrentView = ZirconZrPageView.GetPage();
        }

        // 锆石微量 - Ti 温度计
        [RelayCommand]
        public void ZirconTi()
        {
            CurrentView = ZirconTiPageView.GetPage();
        }

        // 闪锌矿 - FeS 温度计
        [RelayCommand]
        public void SphaleriteFeS()
        {
            CurrentView = SphaleriteFeSPageView.GetPage();
        }

        // 闪锌矿 - GGIMFis 温度计
        [RelayCommand]
        public void SphaleriteGGIMFis()
        {
            CurrentView = SphaleriteGGIMFisPageView.GetPage();
        }
    }
}
