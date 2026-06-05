using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SGeothermometerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool autoCheckGtmUpdate;

        /// <summary>当前软件支持的地质温压计格式版本（只读）。</summary>
        public string CurrentGeothermometerVersion { get; } = ContentVersionHelper.GetGeothermometerFormatVersion();

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
