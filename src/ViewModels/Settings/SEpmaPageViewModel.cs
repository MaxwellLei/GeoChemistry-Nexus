using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Services.Epma;

namespace GeoChemistryNexus.ViewModels
{
    public partial class SEpmaPageViewModel : ObservableObject, IRecipient<DeveloperModeChangedMessage>
    {
        [ObservableProperty]
        private bool isDeveloperMode;

        [ObservableProperty]
        private bool autoCheckEpmaUpdate;

        [ObservableProperty]
        private int defaultWorksheetRowCount;

        /// <summary>当前软件支持的 EPMA 计算包格式版本（只读）。</summary>
        public string CurrentEpmaVersion { get; } = EpmaScriptPackage.CurrentFormatVersion.ToString();

        public ObservableCollection<int> DefaultWorksheetRowCounts { get; } =
            new(WorksheetDefaultsHelper.AllowedRowCounts);

        private bool isLoading = true;

        public SEpmaPageViewModel()
        {
            WeakReferenceMessenger.Default.RegisterAll(this);
            IsDeveloperMode = bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.DeveloperMode), out bool devMode) && devMode;
            // Same default as the other modules: automatic check is on unless the user turned it off.
            AutoCheckEpmaUpdate = !bool.TryParse(ConfigHelper.GetConfig(ConfigKeys.AutoCheckEpmaUpdate), out bool autoCheck) || autoCheck;
            DefaultWorksheetRowCount = WorksheetDefaultsHelper.GetDefaultRowCount(WorksheetDefaultsHelper.EpmaConfigKey);
            isLoading = false;
        }

        public void Receive(DeveloperModeChangedMessage message)
        {
            IsDeveloperMode = message.Value;
        }

        partial void OnAutoCheckEpmaUpdateChanged(bool value)
        {
            if (isLoading) return;
            ConfigHelper.SetConfig(ConfigKeys.AutoCheckEpmaUpdate, value.ToString());
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        partial void OnDefaultWorksheetRowCountChanged(int value)
        {
            if (isLoading) return;
            WorksheetDefaultsHelper.SaveDefaultRowCount(WorksheetDefaultsHelper.EpmaConfigKey, value);
            MessageHelper.Success(LanguageService.Instance["ModifedSuccess"]);
        }

        [RelayCommand]
        private async Task ResetEpmaDatabaseAsync()
        {
            bool isConfirmed = await MessageHelper.ShowAsyncDialog(
                EpmaLocalizer.Format("epma_confirm_reset_database"),
                EpmaLocalizer.Format("Cancel"),
                EpmaLocalizer.Format("clear"));

            if (!isConfirmed)
                return;

            try
            {
                await EpmaCompositionRoot.Templates.ResetDatabaseAsync();
                await Task.Run(EpmaUpdateService.DeleteLocalListCache);
                MessageHelper.Success(EpmaLocalizer.Format("epma_database_cleared"));
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.Message);
            }
        }
    }
}
