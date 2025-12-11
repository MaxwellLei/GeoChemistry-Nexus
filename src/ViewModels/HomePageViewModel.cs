using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Views;
using GeoChemistryNexus.Views.Widgets;
using GongSolutions.Wpf.DragDrop;
using System;
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
            HomeApps = new ObservableCollection<HomeAppItem>(filtered);
            if (filtered.Count != apps.Count)
            {
                HomeAppService.SaveApps(HomeApps);
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
                dialog.TitleBox.Text = app.Title;
                dialog.UrlBox.Text = app.Url;
                dialog.DescBox.Text = app.Description;
                dialog.IconBox.SelectedValue = app.Icon;
                dialog.Title = "编辑链接";

                if (dialog.ShowDialog() == true)
                {
                    app.Title = dialog.Result.Title;
                    app.Url = dialog.Result.Url;
                    app.Description = dialog.Result.Description;
                    app.Icon = dialog.Result.Icon;
                    
                    // Trigger update if needed, though ObservableObject handles property changes
                    HomeAppService.SaveApps(HomeApps);
                }
            }
            // Widgets editing logic if needed
        }

        public void SaveOrder()
        {
            HomeAppService.SaveApps(HomeApps);
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
                if (app.WidgetKey == "TemplateTranslatorWidget")
                {
                    var window = new Window
                    {
                        Title = "模板翻译器",
                        Width = 900,
                        Height = 600,
                        Content = new TemplateTranslatorControl { DataContext = new TemplateTranslatorViewModel() },
                        Owner = Application.Current.MainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    window.Show();
                }
            }
        }

        [RelayCommand]
        private void RemoveApp(HomeAppItem app)
        {
            if (app != null && HomeApps.Contains(app))
            {
                HomeApps.Remove(app);
                HomeAppService.SaveApps(HomeApps);
            }
        }

        [RelayCommand]
        private void AddWebLink()
        {
            var dialog = new AddLinkWindow();
            if (dialog.ShowDialog() == true)
            {
                var newItem = dialog.Result;
                if (newItem != null)
                {
                    HomeApps.Add(newItem);
                    HomeAppService.SaveApps(HomeApps);
                }
            }
        }

        [RelayCommand]
        private void AddWidget()
        {
            var widgets = HomeAppService.GetAvailableWidgets();
            var dialog = new AddWidgetWindow(widgets);
            
            if (dialog.ShowDialog() == true)
            {
                var widget = dialog.SelectedWidget;
                if (widget != null)
                {
                    // Create a new instance/copy so we can have multiple of the same widget if desired,
                    // or just to give it a unique ID.
                    var newItem = new HomeAppItem
                    {
                        Type = HomeAppType.Widget,
                        Title = widget.Title,
                        Description = widget.Description,
                        WidgetKey = widget.WidgetKey,
                        Icon = widget.Icon
                    };
                    HomeApps.Add(newItem);
                    HomeAppService.SaveApps(HomeApps);
                }
            }
        }
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
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
                    HomeAppService.SaveApps(HomeApps);
                }
            }
        }
    }
}
