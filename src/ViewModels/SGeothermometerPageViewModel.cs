using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SGeothermometerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool autoCheckGtmUpdate;

        private bool isLoading = true;

        public SGeothermometerPageViewModel()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_gtm_update"), out bool checkGtm))
            {
                AutoCheckGtmUpdate = checkGtm;
            }

            isLoading = false;
        }

        partial void OnAutoCheckGtmUpdateChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("auto_check_gtm_update", value.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }
    }
}
