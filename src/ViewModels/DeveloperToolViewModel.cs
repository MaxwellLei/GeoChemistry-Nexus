using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using HandyControl.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class PendingChangeItem : ObservableObject
    {
        [ObservableProperty]
        private string _sourceZipPath;

        [ObservableProperty]
        private string _sourceFileName;

        [ObservableProperty]
        private string _internalGraphMapPath;

        [ObservableProperty]
        private GraphMapTemplateParser.JsonTemplateItem _targetTemplateItem;

        [ObservableProperty]
        private string _status;

        // Shared source for the ComboBox
        public ObservableCollection<GraphMapTemplateParser.JsonTemplateItem> AvailableTemplates { get; set; }
    }

    public partial class DeveloperToolViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _announcement;

        [ObservableProperty]
        private string _serverInfoResult;

        [ObservableProperty]
        private string _targetDirectory;

        [ObservableProperty]
        private bool _isUpdateMode = true; // True = Update, False = Change

        [ObservableProperty]
        private string _selectedZipPaths;

        [ObservableProperty]
        private ObservableCollection<PendingChangeItem> _pendingChanges = new();

        private List<string> _zipFiles = new List<string>();

        [ObservableProperty]
        private ObservableCollection<GraphMapTemplateParser.JsonTemplateItem> _templateList;

        [ObservableProperty]
        private GraphMapTemplateParser.JsonTemplateItem _selectedTemplateItem; // Kept for reference but mostly replaced by PendingChanges

        [ObservableProperty]
        private string _logText;

        public DeveloperToolViewModel()
        {
            LoadTemplateList();
            TargetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GeoChemistryNexus_Update");
        }

        private void Log(string message)
        {
            LogText = $"{DateTime.Now:HH:mm:ss} - {message}\n{LogText}";
        }

        private void LoadTemplateList()
        {
            try
            {
                string path = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = JsonSerializer.Deserialize<List<GraphMapTemplateParser.JsonTemplateItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    TemplateList = new ObservableCollection<GraphMapTemplateParser.JsonTemplateItem>(list ?? new List<GraphMapTemplateParser.JsonTemplateItem>());
                }
                else
                {
                    TemplateList = new ObservableCollection<GraphMapTemplateParser.JsonTemplateItem>();
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading template list: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GenerateServerInfo()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Announcement))
                {
                    System.Windows.MessageBox.Show("Please enter an announcement.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string listPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
                string categoriesPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");

                if (!File.Exists(listPath) || !File.Exists(categoriesPath))
                {
                    Log("Error: Source configuration files not found.");
                    return;
                }

                string listHash = UpdateHelper.ComputeFileMd5(listPath);
                string categoriesHash = UpdateHelper.ComputeFileMd5(categoriesPath);

                var serverInfo = new ServerInfo
                {
                    ListHash = listHash,
                    ListPlotCategoriesHash = categoriesHash,
                    Announcement = Announcement
                };

                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
                string json = JsonSerializer.Serialize(serverInfo, options);

                ServerInfoResult = json;

                // Export to target
                EnsureTargetDirectory();
                await File.WriteAllTextAsync(Path.Combine(TargetDirectory, "server_info.json"), json);
                
                Log("server_info.json generated successfully.");
            }
            catch (Exception ex)
            {
                Log($"Error generating server info: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SyncCategories()
        {
            try
            {
                string localPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "PlotTemplateCategories.json");
                if (!File.Exists(localPath))
                {
                    Log("Local PlotTemplateCategories.json not found.");
                    return;
                }

                // Calculate Local Hash
                string localHash = UpdateHelper.ComputeFileMd5(localPath);
                Log($"Local Hash: {localHash}");

                // Get Server Hash
                try 
                {
                    string serverJson = await UpdateHelper.GetUrlContentAsync();
                    var serverInfo = JsonSerializer.Deserialize<ServerInfo>(serverJson);
                    
                    if (serverInfo != null)
                    {
                        Log($"Server Hash: {serverInfo.ListPlotCategoriesHash}");

                        if (string.Equals(localHash, serverInfo.ListPlotCategoriesHash, StringComparison.OrdinalIgnoreCase))
                        {
                            if (System.Windows.MessageBox.Show("Local file matches server version. Export anyway?", "Sync", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                            {
                                Log("Sync cancelled by user (Hashes match).");
                                return;
                            }
                        }
                        else
                        {
                            Log("Hashes differ. Proceeding with export.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not check server version ({ex.Message}). Proceeding with export.");
                }

                EnsureTargetDirectory();
                string destPath = Path.Combine(TargetDirectory, "PlotTemplateCategories.json");
                File.Copy(localPath, destPath, true);
                
                Log($"PlotTemplateCategories.json synced to {destPath}");
            }
            catch (Exception ex)
            {
                Log($"Error syncing categories: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SelectZip()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Zip Files (*.zip)|*.zip",
                Title = "Select Template Zip",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                _zipFiles = dialog.FileNames.ToList();
                SelectedZipPaths = string.Join("; ", _zipFiles.Select(Path.GetFileName));
                Log($"Selected {_zipFiles.Count} Zip(s)");

                await PreparePendingChanges();
            }
        }

        private async Task PreparePendingChanges()
        {
            PendingChanges.Clear();

            foreach (var zipPath in _zipFiles)
            {
                var item = new PendingChangeItem
                {
                    SourceZipPath = zipPath,
                    SourceFileName = Path.GetFileName(zipPath),
                    AvailableTemplates = TemplateList,
                    Status = "Pending"
                };

                try 
                {
                    // Peek inside zip to get GraphMapPath
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    ZipFile.ExtractToDirectory(zipPath, tempDir);

                    string[] jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                    if (jsonFiles.Length > 0)
                    {
                        string jsonPath = jsonFiles[0];
                        string jsonContent = await File.ReadAllTextAsync(jsonPath);
                        // Just simple check or parse?
                        // We need GraphMapPath which is usually filename without extension
                        item.InternalGraphMapPath = Path.GetFileNameWithoutExtension(jsonPath);
                        
                        // Try to auto-match
                        if (!IsUpdateMode)
                        {
                            var match = TemplateList.FirstOrDefault(x => x.GraphMapPath.Equals(item.InternalGraphMapPath, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                item.TargetTemplateItem = match;
                                item.Status = "Auto-Matched";
                            }
                            else
                            {
                                item.Status = "No Match Found";
                            }
                        }
                        else
                        {
                            item.Status = "Ready to Add";
                        }
                    }
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    item.Status = $"Error: {ex.Message}";
                }

                PendingChanges.Add(item);
            }
        }

        partial void OnIsUpdateModeChanged(bool value)
        {
            // Refresh auto-match logic when mode changes
            if (_zipFiles.Count > 0)
            {
                _ = PreparePendingChanges();
            }
        }

        [RelayCommand]
        private void SelectTargetDirectory()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TargetDirectory = dialog.SelectedPath;
                }
            }
        }

        [RelayCommand]
        private void RemovePendingItem(PendingChangeItem item)
        {
            if (item != null && PendingChanges.Contains(item))
            {
                PendingChanges.Remove(item);
                Log($"Removed pending item: {item.SourceFileName}");
            }
        }

        [RelayCommand]
        private async Task ProcessTemplate()
        {
            if (PendingChanges.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select zip files first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                EnsureTargetDirectory();

                foreach (var item in PendingChanges)
                {
                    if (!File.Exists(item.SourceZipPath)) continue;

                    Log($"Processing {item.SourceFileName}...");

                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    ZipFile.ExtractToDirectory(item.SourceZipPath, tempDir);

                    string[] jsonFiles = Directory.GetFiles(tempDir, "*.json", SearchOption.AllDirectories);
                    if (jsonFiles.Length == 0)
                    {
                        Log($"Error: No JSON file found in {item.SourceFileName}.");
                        Directory.Delete(tempDir, true);
                        continue;
                    }

                    string templateJsonPath = jsonFiles[0];
                    string jsonContent = await File.ReadAllTextAsync(templateJsonPath);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    };
                    var templateObj = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, options);

                    if (templateObj == null || templateObj.NodeList == null)
                    {
                        Log($"Error: Invalid template JSON structure in {item.SourceFileName}.");
                        Directory.Delete(tempDir, true);
                        continue;
                    }

                    // Determine GraphMapPath
                    string graphMapPath = Path.GetFileNameWithoutExtension(templateJsonPath);

                    // User Requirement: Calculate MD5 of the JSON file
                    string jsonHash = UpdateHelper.ComputeFileMd5(templateJsonPath);
                    Log($"Calculated JSON Hash for {graphMapPath}: {jsonHash}");

                    if (IsUpdateMode)
                    {
                        // Add New Logic
                        var newItem = new GraphMapTemplateParser.JsonTemplateItem
                        {
                            NodeList = templateObj.NodeList,
                            GraphMapPath = graphMapPath,
                            FileHash = jsonHash
                        };

                        var existing = TemplateList.FirstOrDefault(x => x.GraphMapPath == graphMapPath);
                        if (existing != null)
                        {
                            TemplateList.Remove(existing);
                            TemplateList.Add(newItem);
                            Log($"Updated existing template (by name): {graphMapPath}");
                        }
                        else
                        {
                            TemplateList.Add(newItem);
                            Log($"Added new template: {graphMapPath}");
                        }
                    }
                    else
                    {
                        // Change Mode Logic
                        // Use user-selected TargetTemplateItem if available
                        var target = item.TargetTemplateItem;
                        
                        if (target == null)
                        {
                            // Fallback to name matching if user didn't select
                            target = TemplateList.FirstOrDefault(x => x.GraphMapPath == graphMapPath);
                        }

                        if (target == null)
                        {
                            Log($"Warning: No target selected or matched for {item.SourceFileName}. Skipping.");
                            Directory.Delete(tempDir, true);
                            continue;
                        }

                        // Update the target item
                        target.FileHash = jsonHash;
                        target.NodeList = templateObj.NodeList;
                        target.GraphMapPath = graphMapPath; 

                        // Trigger UI refresh
                        int index = TemplateList.IndexOf(target);
                        if (index >= 0) TemplateList[index] = target;

                        Log($"Replaced template entry. New Hash: {jsonHash}");
                    }

                    // Export Zip (Renamed to match the inner GraphMapPath)
                    string targetZipPath = Path.Combine(TargetDirectory, $"{graphMapPath}.zip");
                    File.Copy(item.SourceZipPath, targetZipPath, true);

                    Directory.Delete(tempDir, true);
                }

                // Save Lists
                string listPath = Path.Combine(FileHelper.GetAppPath(), "Data", "PlotData", "GraphMapList.json");
                var writeOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
                string newListJson = JsonSerializer.Serialize(TemplateList, writeOptions);
                await File.WriteAllTextAsync(listPath, newListJson);
                await File.WriteAllTextAsync(Path.Combine(TargetDirectory, "GraphMapList.json"), newListJson);

                Log($"Batch processing completed. Exported to: {TargetDirectory}");
            }
            catch (Exception ex)
            {
                Log($"Error processing templates: {ex.Message}");
            }
        }

        private void EnsureTargetDirectory()
        {
            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
            }
        }
    }
}
