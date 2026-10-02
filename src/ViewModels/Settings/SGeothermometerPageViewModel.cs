using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SGeothermometerPageViewModel : ObservableObject, IRecipient<DeveloperModeChangedMessage>
    {
        [ObservableProperty]
        private bool isDeveloperMode;

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
            WeakReferenceMessenger.Default.RegisterAll(this);
            LoadConfig();
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        private void LoadConfig()
        {
            isLoading = true;

            IsDeveloperMode = bool.TryParse(ConfigHelper.GetConfig("developer_mode"), out bool devMode) && devMode;

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

        [RelayCommand]
        private async Task ResetGeothermometerDatabaseAsync()
        {
            string message = LanguageService.Instance["confirm_reset_geothermometer_database"]
                ?? "Are you sure you want to clear the local geothermometer template database? All local geothermometer template data will be removed. This action cannot be undone.";

            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                message,
                LanguageService.Instance["Cancel"] ?? "Cancel",
                LanguageService.Instance["clear"] ?? "Clear");

            if (!isConfirmed)
                return;

            try
            {
                GeothermometerDatabaseService.Instance.ClearDatabase();
                GeothermometerService.ReloadPlugins();
                WeakReferenceMessenger.Default.Send(new GeoTMineralCategoryUpdatedMessage("Reset"));
                MessageHelper.Success(LanguageService.Instance["geothermometer_database_cleared"] ?? "Local geothermometer template database cleared successfully.");
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }
    }
}
