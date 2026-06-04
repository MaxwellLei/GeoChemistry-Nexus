using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public partial class TemplateRepairViewModel : ObservableObject
    {
        [ObservableProperty]
        private string logText = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy = false;

        public bool IsNotBusy => !IsBusy;

        private const string ServerGraphMapListUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/GraphMapList.json";
        private const string ServerCategoriesUrl = "https://geochemistrynexus-1303234197.cos.ap-hongkong.myqcloud.com/PlotTemplateCategories.json";

        public TemplateRepairViewModel()
        {
            Log("Ready to check and repair templates.");
        }

        private void Log(string message)
        {
            LogText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        [RelayCommand]
        private async Task CheckAndRepair()
        {
            if (IsBusy) return;
            IsBusy = true;
            Log("Starting check process...");

            try
            {
                await CheckAndRepairGraphMapList();
                await CheckAndRepairCategories();

                Log("Check and repair process completed.");
                HandyControl.Controls.Growl.Success("修复流程完成");
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                HandyControl.Controls.Growl.Error($"发生错误: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CheckAndRepairGraphMapList()
        {
            string path = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
            Log($"Checking {path}...");

            bool needDownload = false;
            if (!File.Exists(path))
            {
                Log("GraphMapList.json is missing.");
                needDownload = true;
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(path);
                    JsonSerializer.Deserialize<List<GraphMapTemplateService.JsonTemplateItem>>(json);
                }
                catch
                {
                    Log("GraphMapList.json is corrupted.");
                    needDownload = true;
                }
            }

            if (needDownload)
            {
                Log("Downloading GraphMapList.json from server...");
                await UpdateHelper.DownloadFileAsync(ServerGraphMapListUrl, path);
                Log("GraphMapList.json downloaded successfully.");
            }
            else
            {
                Log("GraphMapList.json is valid.");
            }
        }

        private async Task CheckAndRepairCategories()
        {
            string path = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");
            Log($"Checking {path}...");

            bool needDownload = false;
            if (!File.Exists(path))
            {
                Log("PlotTemplateCategories.json is missing.");
                needDownload = true;
            }
            else
            {
                try
                {
                    string json = File.ReadAllText(path);
                    JsonSerializer.Deserialize<PlotTemplateCategoryConfig>(json);
                }
                catch
                {
                    Log("PlotTemplateCategories.json is corrupted.");
                    needDownload = true;
                }
            }

            if (needDownload)
            {
                Log("Downloading PlotTemplateCategories.json from server...");
                await UpdateHelper.DownloadFileAsync(ServerCategoriesUrl, path);
                Log("PlotTemplateCategories.json downloaded successfully.");
            }
            else
            {
                Log("PlotTemplateCategories.json is valid.");
            }
        }
    }
}
