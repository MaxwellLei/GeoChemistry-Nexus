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
using System.Windows.Controls;

namespace GeoChemistryNexus.ViewModels
{
    public partial class GeothermometerPageViewModel : ObservableObject
    {
        // 导航对象
        [ObservableProperty]
        private object? currentView;
        
        // 遮罩显示
        [ObservableProperty]
        private bool isMaskVisible = true;

        private RichTextBox _richTextBox;

        // 初始化
        public GeothermometerPageViewModel(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;
        }

        // 加载到了计算方法触发
        partial void OnCurrentViewChanged(object value)
        {
            if (currentView == null)
            {
                IsMaskVisible = true;
            }
            else
            {
                IsMaskVisible = false;
            }
        }


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

        // 石英 - TitaniQ 温度计
        [RelayCommand]
        public void QuatzTi()
        {
            CurrentView = QuatzTiPageView.GetPage();
        }

        // 毒砂 温度计
        [RelayCommand]
        public void ArsenopyriteM(string typeK)
        {
            CurrentView = ArsenopyritePageView.GetPage(typeK);
        }

        // 角闪石 - 主量温度计
        [RelayCommand]
        public void AmphiboleM()
        {
            CurrentView = AmphiboleMPageView.GetPage();
        }

        // 绿泥石 - Jowett (1991)
        [RelayCommand]
        public void ChloriteM()
        {
            CurrentView = ChloritePageView.GetPage();
        }
    }
}
