using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

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
                await CheckMissingTemplatesInList();
                await CheckMissingCustomTemplates();

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
                    JsonSerializer.Deserialize<List<GraphMapTemplateParser.JsonTemplateItem>>(json);
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

        private async Task CheckMissingTemplatesInList()
        {
            Log("Checking for missing default templates...");
            string listPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
            if (!File.Exists(listPath)) return;

            var json = File.ReadAllText(listPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<GraphMapTemplateParser.JsonTemplateItem>>(json, options);

            if (list == null) return;

            var itemsToRemove = new List<GraphMapTemplateParser.JsonTemplateItem>();
            string defaultDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Default");

            foreach (var item in list)
            {
                // Logic derived from MainPlotViewModel.cs (assumed path structure)
                
                string templateDir = Path.Combine(defaultDir, item.GraphMapPath);
                string templateFile = Path.Combine(templateDir, $"{item.GraphMapPath}.json");

                if (!File.Exists(templateFile))
                {
                    Log($"Missing template file: {templateFile}");
                    itemsToRemove.Add(item);
                }
            }

            if (itemsToRemove.Count > 0)
            {
                string msg = $"发现 {itemsToRemove.Count} 个默认模板在列表中存在但文件丢失。\n是否清除这些无效记录？";
                bool confirm = await MessageHelper.ShowAsyncDialog(msg, "取消", "清除");

                if (confirm)
                {
                    foreach (var item in itemsToRemove)
                    {
                        list.Remove(item);
                        Log($"Removed invalid default template: {item.GraphMapPath}");
                    }
                    
                    string newJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(listPath, newJson);
                    Log("GraphMapList.json updated.");
                }
            }
            else
            {
                Log("All default templates in list exist.");
            }
        }

        private async Task CheckMissingCustomTemplates()
        {
            Log("Checking for missing custom templates...");
            string listPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapCustomList.json");
            if (!File.Exists(listPath))
            {
                Log("GraphMapCustomList.json not found, skipping.");
                return;
            }

            var json = File.ReadAllText(listPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            List<GraphMapTemplateParser.JsonTemplateItem> list;
            try
            {
                list = JsonSerializer.Deserialize<List<GraphMapTemplateParser.JsonTemplateItem>>(json, options);
            }
            catch
            {
                Log("GraphMapCustomList.json is corrupted.");
                return; 
            }

            if (list == null) return;

            var itemsToRemove = new List<GraphMapTemplateParser.JsonTemplateItem>();
            string customDir = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "Custom");

            foreach (var item in list)
            {
                string templateDir = Path.Combine(customDir, item.GraphMapPath);
                string templateFile = Path.Combine(templateDir, $"{item.GraphMapPath}.json");

                if (!File.Exists(templateFile))
                {
                    Log($"Missing custom template file: {templateFile}");
                    itemsToRemove.Add(item);
                }
            }

            if (itemsToRemove.Count > 0)
            {
                string msg = $"发现 {itemsToRemove.Count} 个自定义模板在列表中存在但文件丢失。\n是否清除这些无效记录？";
                bool confirm = await MessageHelper.ShowAsyncDialog(msg, "取消", "清除");

                if (confirm)
                {
                    foreach (var item in itemsToRemove)
                    {
                        list.Remove(item);
                        Log($"Removed invalid custom template: {item.GraphMapPath}");
                    }

                    string newJson = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(listPath, newJson);
                    Log("GraphMapCustomList.json updated.");
                }
            }
            else
            {
                Log("All custom templates in list exist.");
            }
        }
    }
}
