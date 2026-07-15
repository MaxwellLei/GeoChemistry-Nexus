using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SGeothermometerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool autoCheckGtmUpdate;

        [ObservableProperty]
        private int defaultWorksheetRowCount;

        /// <summary>当前软件支持的地质温压计格式版本（只读）。</summary>
        public string CurrentGeothermometerVersion { get; } = ContentVersionHelper.GetGeothermometerFormatVersion();

        public ObservableCollection<int> DefaultWorksheetRowCounts { get; } =
            new(WorksheetDefaultsHelper.AllowedRowCounts);

        private bool isLoading = true;

        public SGeothermometerPageViewModel()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            isLoading = true;

            if (bool.TryParse(ConfigHelper.GetConfig("auto_check_gtm_update"), out bool checkGtm))
            {
                AutoCheckGtmUpdate = checkGtm;
            }

            DefaultWorksheetRowCount = WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.GtmConfigKey);

            isLoading = false;
        }

        partial void OnAutoCheckGtmUpdateChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig("auto_check_gtm_update", value.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnDefaultWorksheetRowCountChanged(int value)
        {
            if (isLoading) return;
            WorksheetDefaultsHelper.SaveDefaultRowCount(WorksheetDefaultsHelper.GtmConfigKey, value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }
    }
}
