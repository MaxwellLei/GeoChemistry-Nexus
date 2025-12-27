using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Views;
using GeoChemistryNexus.Views.Widgets;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomePageViewModel : ObservableObject, IDropTarget
    {
        private readonly DispatcherTimer _timer;
        private readonly ObservableCollection<HomeAppItem> _sourceApps = new();
        private readonly Dictionary<string, Window> _openedWindows = new();

        public ICollectionView HomeApps { get; }

        [ObservableProperty]
        private string currentTime = string.Empty;

        [ObservableProperty]
        private string currentDate = string.Empty;

        [ObservableProperty]
        private string searchString = string.Empty;

        [ObservableProperty]
        private bool isEditMode = false;

        partial void OnSearchStringChanged(string value)
        {
            HomeApps.Refresh();
        }

        public HomePageViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateNow();

            // Initialize CollectionView
            HomeApps = CollectionViewSource.GetDefaultView(_sourceApps);
            HomeApps.Filter = FilterApps;

            LoadApps();
            UpdateNow();
        }

        private bool FilterApps(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchString)) return true;

            if (obj is HomeAppItem app)
            {
                var q = SearchString.Trim();
                return (app.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) || 
                       (app.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            return false;
        }

        [RelayCommand]
        private void Loaded()
        {
            UpdateNow();
            _timer.Start();
        }

        [RelayCommand]
        private void Unloaded()
        {
            _timer.Stop();
        }

        private void LoadApps()
        {
            var apps = HomeAppService.LoadApps();
            var filtered = apps.Where(a => !(a.Type == HomeAppType.Widget &&
                                             (string.Equals(a.WidgetKey, "CalendarWidget", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(a.WidgetKey, "SystemInfoWidget", StringComparison.OrdinalIgnoreCase)))).ToList();
            
            _sourceApps.Clear();
            foreach (var app in filtered)
            {
                _sourceApps.Add(app);
            }

            if (filtered.Count != apps.Count)
            {
                HomeAppService.SaveApps(_sourceApps);
            }
        }

        private void UpdateNow()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm");
            CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }

        [RelayCommand]
        private void EditApp(HomeAppItem app)
        {
            if (app == null) return;
            
            if (app.Type == HomeAppType.WebLink)
            {
                var dialog = new AddLinkWindow();
                dialog.Owner = Application.Current.MainWindow;
                dialog.TitleBox.Text = app.Title;
                dialog.UrlBox.Text = app.Url;
                dialog.DescBox.Text = app.Description;
                dialog.IconBox.SelectedValue = app.Icon;
                dialog.Title = LanguageService.Instance["edit_link"];

                if (dialog.ShowDialog() == true)
                {
                    app.Title = dialog.Result.Title;
                    app.Url = dialog.Result.Url;
                    app.Description = dialog.Result.Description;
                    app.Icon = dialog.Result.Icon;
                    
                    HomeAppService.SaveApps(_sourceApps);
                }
            }
        }

        public void SaveOrder()
        {
            HomeAppService.SaveApps(_sourceApps);
        }

        [RelayCommand]
        private void SearchStarted()
        {
            var q = (searchString ?? string.Empty).Trim();
            if (q.Length == 0) return;
            var url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(q);
            try
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            catch (Exception ex)
            {
                MessageHelper.Warning("OpenBrowserError: " + ex.Message);
            }
        }

        [RelayCommand]
        private void OpenApp(HomeAppItem app)
        {
            if (app == null || IsEditMode) return;

            if (app.Type == HomeAppType.WebLink && !string.IsNullOrWhiteSpace(app.Url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {app.Url}") { CreateNoWindow = true });
                }
                catch (Exception ex)
                {
                    MessageHelper.Warning("OpenBrowserError: " + ex.Message);
                }
            }
            else if (app.Type == HomeAppType.Widget)
            {
                try
                {
                    // 检查窗口是否已打开
                    if (!string.IsNullOrEmpty(app.WidgetKey) && _openedWindows.TryGetValue(app.WidgetKey, out var existingWindow))
                    {
                        if (existingWindow.IsLoaded)
                        {
                            if (existingWindow.WindowState == WindowState.Minimized)
                                existingWindow.WindowState = WindowState.Normal;
                            existingWindow.Activate();
                            return;
                        }
                        else
                        {
                            _openedWindows.Remove(app.WidgetKey);
                        }
                    }

                    Window window = null;

                    if (app.WidgetKey == "TemplateTranslatorWidget")
                    {
                        window = new Window
                        {
                            Title = LanguageService.Instance["template_translator"],
                            Width = 900,
                            Height = 600,
                            Content = new TemplateTranslatorWidget { DataContext = new TemplateTranslatorViewModel() }
                        };
                    }
                    else if (app.WidgetKey == "TemplateRepairWidget")
                    {
                        window = new Window
                        {
                            Title = LanguageService.Instance["diagram_template_repair_tool"],
                            Width = 600,
                            Height = 400,
                            Content = new TemplateRepairWidget { DataContext = new TemplateRepairViewModel() }
                        };
                    }
                    else if (app.WidgetKey == "AnnouncementWidget")
                    {
                        window = new Window
                        {
                            Title = LanguageService.Instance["server_announcement"],
                            Width = 500,
                            Height = 350,
                            Content = new AnnouncementWidget { DataContext = new AnnouncementViewModel() }
                        };
                    }
                    else if (app.WidgetKey == "DeveloperToolWidget")
                    {
                        window = new Window
                        {
                            Title = LanguageService.Instance["developer_maintenance_tool"],
                            Width = 1200,
                            Height = 600,
                            Content = new GeoChemistryNexus.Views.Widgets.DeveloperToolWidget()
                        };
                    }

                    if (window != null)
                    {
                        var mainWindow = Application.Current.MainWindow;
                        if (mainWindow != null && mainWindow.IsVisible)
                        {
                            window.Owner = mainWindow;
                            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        }
                        else
                        {
                            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        }
                        
                        window.Show();

                        if (!string.IsNullOrEmpty(app.WidgetKey))
                        {
                            _openedWindows[app.WidgetKey] = window;
                            window.Closed += (s, e) => _openedWindows.Remove(app.WidgetKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 打开小组件失败
                    MessageHelper.Error(LanguageService.Instance["failed_to_open_widget"] + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        [RelayCommand]
        private void RemoveApp(HomeAppItem app)
        {
            if (app != null && _sourceApps.Contains(app))
            {
                _sourceApps.Remove(app);
                HomeAppService.SaveApps(_sourceApps);
            }
        }

        [RelayCommand]
        private void AddWebLink()
        {
            var dialog = new AddLinkWindow();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var newItem = dialog.Result;
                if (newItem != null)
                {
                    _sourceApps.Add(newItem);
                    HomeAppService.SaveApps(_sourceApps);
                }
            }
        }

        [RelayCommand]
        private void AddWidget()
        {
            var widgets = HomeAppService.GetAvailableWidgets();
            var dialog = new AddWidgetWindow(widgets);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var widget = dialog.SelectedWidget;
                if (widget != null)
                {
                    // 创建一个新的实例
                    var newItem = new HomeAppItem
                    {
                        Type = HomeAppType.Widget,
                        Title = widget.Title,
                        Description = widget.Description,
                        WidgetKey = widget.WidgetKey,
                        Icon = widget.Icon
                    };
                    _sourceApps.Add(newItem);
                    HomeAppService.SaveApps(_sourceApps);
                }
            }
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (!string.IsNullOrEmpty(SearchString)) return;

            if (IsEditMode && dropInfo.Data is HomeAppItem && dropInfo.TargetItem is HomeAppItem)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (IsEditMode && dropInfo.Data is HomeAppItem sourceItem && dropInfo.TargetItem is HomeAppItem targetItem)
            {
                int sourceIndex = _sourceApps.IndexOf(sourceItem);
                int targetIndex = _sourceApps.IndexOf(targetItem);

                if (sourceIndex != -1 && targetIndex != -1)
                {
                    _sourceApps.Move(sourceIndex, targetIndex);
                    
                    if (string.IsNullOrEmpty(SearchString))
                    {
                        HomeAppService.SaveApps(_sourceApps);
                    }
                }
            }
        }
    }
}
