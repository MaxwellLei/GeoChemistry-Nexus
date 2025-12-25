using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public partial class StartViewModel : ObservableObject
    {
        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private string _progressText;

        public StartViewModel()
        {
            ProgressValue = 0;
            ProgressText = LanguageService.Instance["StartStatus"];
        }

        public void UpdateProgress(double value, string text)
        {
            ProgressValue = value;
            ProgressText = text;
        }
    }
}
