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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomePageViewModel : ObservableObject, IDropTarget
    {
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private string currentTime = string.Empty;

        [ObservableProperty]
        private string currentDate = string.Empty;

        [ObservableProperty]
        private string searchString = string.Empty;

        [ObservableProperty]
        private bool isEditMode = false;

        [ObservableProperty]
        private ObservableCollection<HomeAppItem> homeApps = new();

        private List<HomeAppItem> _allApps = new();

        partial void OnSearchStringChanged(string value)
        {
            UpdateHomeApps();
        }

        private void UpdateHomeApps()
        {
            if (string.IsNullOrWhiteSpace(SearchString))
            {
                HomeApps = new ObservableCollection<HomeAppItem>(_allApps);
            }
            else
            {
                var q = SearchString.Trim();
                var filtered = _allApps.Where(a => (a.Title?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) || 
                                                   (a.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
                HomeApps = new ObservableCollection<HomeAppItem>(filtered);
            }
        }

        public HomePageViewModel()
        {
            UpdateNow();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateNow();
            _timer.Start();

            LoadApps();
        }

        private void LoadApps()
        {
            var apps = HomeAppService.LoadApps();
            var filtered = apps.Where(a => !(a.Type == HomeAppType.Widget &&
                                             (string.Equals(a.WidgetKey, "CalendarWidget", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(a.WidgetKey, "SystemInfoWidget", StringComparison.OrdinalIgnoreCase)))).ToList();
            _allApps = filtered;
            UpdateHomeApps();

            if (filtered.Count != apps.Count)
            {
                HomeAppService.SaveApps(_allApps);
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
                    
                    // Trigger update if needed, though ObservableObject handles property changes
                    HomeAppService.SaveApps(_allApps);
                }
            }
            // Widgets editing logic if needed
        }

        public void SaveOrder()
        {
            HomeAppService.SaveApps(_allApps);
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
            if (app == null || IsEditMode) return; // Prevent opening when in edit mode

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
                            Content = new GeoChemistryNexus.Views.Widgets.DeveloperToolWidget() // Ensure correct namespace
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
                    }
                }
                catch (Exception ex)
                {
                    MessageHelper.Error("Failed to open widget: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        [RelayCommand]
        private void RemoveApp(HomeAppItem app)
        {
            if (app != null && _allApps.Contains(app))
            {
                _allApps.Remove(app);
                UpdateHomeApps();
                HomeAppService.SaveApps(_allApps);
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
                    _allApps.Add(newItem);
                    UpdateHomeApps();
                    HomeAppService.SaveApps(_allApps);
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
                    _allApps.Add(newItem);
                    UpdateHomeApps();
                    HomeAppService.SaveApps(_allApps);
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
                int sourceIndex = HomeApps.IndexOf(sourceItem);
                int targetIndex = HomeApps.IndexOf(targetItem);

                if (sourceIndex != -1 && targetIndex != -1)
                {
                    HomeApps.Move(sourceIndex, targetIndex);
                    
                    if (string.IsNullOrEmpty(SearchString))
                    {
                        _allApps = new System.Collections.Generic.List<HomeAppItem>(HomeApps);
                        HomeAppService.SaveApps(_allApps);
                    }
                }
            }
        }
    }
}
