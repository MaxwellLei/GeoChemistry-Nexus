using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using System.Collections.ObjectModel;
using System.IO;

namespace GeoChemistryNexus.ViewModels
{
    public partial class ExportPanelViewModel : ObservableObject
    {
        private readonly MainPlotViewModel _mainViewModel;

        public ExportPanelViewModel(MainPlotViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            SelectedThirdPartyApp = _mainViewModel.SelectedThirdPartyApp;
        }

        [ObservableProperty]
        private string _selectedFormat = "png";

        public ObservableCollection<string> Formats { get; } = new() { "png", "jpg", "bmp", "svg" };

        [ObservableProperty]
        private string _selectedThirdPartyApp;

        public ObservableCollection<string> ThirdPartyApps => _mainViewModel.ThirdPartyApps;

        [RelayCommand]
        private void Export()
        {
            _mainViewModel.ExportImg(SelectedFormat);
        }

        [RelayCommand]
        private void OpenThirdParty()
        {
            _mainViewModel.SelectedThirdPartyApp = SelectedThirdPartyApp;
            
            if (_mainViewModel.OpenWithThirdPartyCommand.CanExecute(null))
            {
                _mainViewModel.OpenWithThirdPartyCommand.Execute(null);
            }
        }
    }
}
