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
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomePageViewModel : ObservableObject, IDropTarget
    {
        private readonly ObservableCollection<HomeAppItem> _widgets = new();
        private readonly Dictionary<string, Window> _openedWindows = new();
        private HomeLinkGroupViewModel _personalGroup;

        public ObservableCollection<HomeLinkGroupViewModel> OfficialLinkGroups { get; } = new();

        public ObservableCollection<HomeAppItem> Widgets => _widgets;

        public HomeLinkGroupViewModel PersonalLinkGroup => _personalGroup;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectedGroupPersonal))]
        private HomeLinkGroupViewModel selectedLinkGroup;

        [ObservableProperty]
        private HomeLinkGroupViewModel selectedOfficialGroup;

        partial void OnSelectedOfficialGroupChanged(HomeLinkGroupViewModel value)
        {
            if (value != null)
                SelectedLinkGroup = value;
        }

        partial void OnSelectedLinkGroupChanged(HomeLinkGroupViewModel value)
        {
            if (value?.IsPersonal == true)
                SelectedOfficialGroup = null;
            else if (value != null && !value.IsPersonal)
                SelectedOfficialGroup = value;

            if (value?.IsPersonal != true && IsEditMode)
                IsEditMode = false;
        }

        [ObservableProperty]
        private bool isEditMode;

        public bool IsSelectedGroupPersonal => SelectedLinkGroup?.IsPersonal == true;

        [ObservableProperty]
        private string announcementText = string.Empty;

        [ObservableProperty]
        private bool hasAnnouncement;

        [ObservableProperty]
        private bool isAnnouncementBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCatalogIdle))]
        private bool isCatalogBusy;

        public bool IsAnnouncementIdle => !IsAnnouncementBusy;

        public bool IsCatalogIdle => !IsCatalogBusy;

        public HomePageViewModel()
        {
            LanguageService.Instance.PropertyChanged += OnAppLanguageChanged;
            RebuildGroups();
        }

        private void OnAppLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Item[]")
                RebuildGroups();
        }

        [RelayCommand]
        private async Task Loaded()
        {
            await RefreshHomeDataAsync(showUpdateMessage: false);
        }

        [RelayCommand]
        private async Task RefreshCatalog()
        {
            await RefreshHomeDataAsync(showUpdateMessage: true);
        }

        [RelayCommand]
        private async Task RefreshAnnouncement()
        {
            await LoadAnnouncementAsync();
        }

        private async Task RefreshHomeDataAsync(bool showUpdateMessage)
        {
            if (IsCatalogBusy)
                return;

            IsCatalogBusy = true;
            try
            {
                bool updated = await HomeLinksCatalogService.SyncFromServerAsync();
                await LoadAnnouncementAsync();
                RebuildGroups();

                if (showUpdateMessage)
                {
                    if (updated)
                        MessageHelper.Success(LanguageService.Instance["home_catalog_updated"]);
                    else
                        MessageHelper.Info(LanguageService.Instance["home_catalog_already_latest"]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomePageViewModel] Refresh failed: {ex.Message}");
                if (showUpdateMessage)
                    MessageHelper.Warning(LanguageService.Instance["home_catalog_sync_failed"]);
            }
            finally
            {
                IsCatalogBusy = false;
            }
        }

        private async Task LoadAnnouncementAsync()
        {
            if (IsAnnouncementBusy)
                return;

            IsAnnouncementBusy = true;
            OnPropertyChanged(nameof(IsAnnouncementIdle));
            try
            {
                string text = await ServerAnnouncementService.LoadAnnouncementAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AnnouncementText = text;
                    HasAnnouncement = true;
                }
                else
                {
                    AnnouncementText = string.Empty;
                    HasAnnouncement = false;
                }
            }
            catch
            {
                AnnouncementText = string.Empty;
                HasAnnouncement = false;
            }
            finally
            {
                IsAnnouncementBusy = false;
                OnPropertyChanged(nameof(IsAnnouncementIdle));
            }
        }

        private void RebuildGroups()
        {
            string previousGroupId = SelectedLinkGroup?.GroupId;

            var catalog = HomeLinksCatalogService.LoadLocalCatalog();
            var userConfig = HomeUserConfigService.Load();

            OfficialLinkGroups.Clear();
            _widgets.Clear();

            foreach (var group in catalog.Groups ?? Enumerable.Empty<HomeLinkGroup>())
            {
                if (group.Links == null || group.Links.Count == 0)
                    continue;

                var groupVm = new HomeLinkGroupViewModel
                {
                    GroupId = group.Id,
                    Title = HomeLinksLocalization.ResolveForApp(group.Title),
                    IsPersonal = false,
                    IsVisible = true
                };

                foreach (var link in group.Links)
                    groupVm.Items.Add(ToOfficialAppItem(link));

                OfficialLinkGroups.Add(groupVm);
            }

            _personalGroup = new HomeLinkGroupViewModel
            {
                GroupId = "personal",
                Title = LanguageService.Instance["home_personal_group"],
                IsPersonal = true,
                IsVisible = true
            };

            foreach (var link in userConfig.PersonalLinks ?? Enumerable.Empty<HomeAppItem>())
            {
                link.IsOfficial = false;
                link.Type = HomeAppType.WebLink;
                _personalGroup.Items.Add(link);
            }

            OnPropertyChanged(nameof(PersonalLinkGroup));

            var availableWidgets = HomeAppService.GetAvailableWidgets()
                .Where(w => !string.IsNullOrEmpty(w.WidgetKey))
                .ToDictionary(w => w.WidgetKey, w => w, StringComparer.OrdinalIgnoreCase);

            foreach (var widget in userConfig.Widgets ?? Enumerable.Empty<HomeAppItem>())
            {
                widget.IsOfficial = false;
                widget.Type = HomeAppType.Widget;
                if (availableWidgets.TryGetValue(widget.WidgetKey ?? string.Empty, out var template))
                {
                    widget.Title = template.Title;
                    widget.Description = template.Description;
                    widget.Icon = template.Icon;
                }
                _widgets.Add(widget);
            }

            RestoreSelectedLinkGroup(previousGroupId);
        }

        private void RestoreSelectedLinkGroup(string previousGroupId)
        {
            if (string.Equals(previousGroupId, "personal", StringComparison.OrdinalIgnoreCase))
            {
                SelectedLinkGroup = _personalGroup;
                return;
            }

            var official = OfficialLinkGroups.FirstOrDefault(g => g.GroupId == previousGroupId);
            if (official != null)
            {
                SelectedLinkGroup = official;
                return;
            }

            if (OfficialLinkGroups.Count > 0)
                SelectedLinkGroup = OfficialLinkGroups[0];
            else
                SelectedLinkGroup = _personalGroup;
        }

        [RelayCommand]
        private void SelectPersonalGroup()
        {
            SelectedLinkGroup = _personalGroup;
        }

        private static HomeAppItem ToOfficialAppItem(HomeLinkEntry entry)
        {
            return new HomeAppItem
            {
                Id = string.IsNullOrWhiteSpace(entry.Id) ? Guid.NewGuid().ToString() : entry.Id,
                Type = HomeAppType.WebLink,
                Title = HomeLinksLocalization.ResolveForApp(entry.Title),
                Description = HomeLinksLocalization.ResolveForApp(entry.Description),
                Url = entry.Url ?? string.Empty,
                Icon = HomeIconHelper.ResolveIcon(entry.Icon),
                IsOfficial = true
            };
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }

        [RelayCommand]
        private void EditApp(HomeAppItem app)
        {
            if (app == null || app.IsReadOnly)
                return;

            if (app.Type != HomeAppType.WebLink)
                return;

            var dialog = new AddLinkWindow();
            dialog.Owner = Application.Current.MainWindow;
            dialog.TitleBox.Text = app.Title;
            dialog.UrlBox.Text = app.Url;
            dialog.DescBox.Text = app.Description;
            dialog.LoadIcon(app.Icon);
            dialog.Title = LanguageService.Instance["edit_link"];

            if (dialog.ShowDialog() == true)
            {
                app.Title = dialog.Result.Title;
                app.Url = dialog.Result.Url;
                app.Description = dialog.Result.Description;
                app.Icon = dialog.Result.Icon;
                SavePersonalLinks();
            }
        }

        [RelayCommand]
        private void OpenApp(HomeAppItem app)
        {
            if (app == null || IsEditMode)
                return;

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
                OpenWidget(app);
            }
        }

        private void OpenWidget(HomeAppItem app)
        {
            try
            {
                if (!string.IsNullOrEmpty(app.WidgetKey) && _openedWindows.TryGetValue(app.WidgetKey, out var existingWindow))
                {
                    if (existingWindow.IsLoaded)
                    {
                        if (existingWindow.WindowState == WindowState.Minimized)
                            existingWindow.WindowState = WindowState.Normal;
                        existingWindow.Activate();
                        return;
                    }

                    _openedWindows.Remove(app.WidgetKey);
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
                else if (app.WidgetKey == "OfficialTemplatePublisherWidget")
                {
                    window = new Window
                    {
                        Title = LanguageService.Instance["official_template_publisher"],
                        Width = 920,
                        Height = 700,
                        Content = new OfficialTemplatePublisherWidget { DataContext = new OfficialTemplatePublisherViewModel() }
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

                if (window == null)
                    return;

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
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["failed_to_open_widget"] + ex.Message + "\n" + ex.StackTrace);
            }
        }

        [RelayCommand]
        private void RemoveApp(HomeAppItem app)
        {
            if (app == null || app.IsReadOnly)
                return;

            if (app.Type == HomeAppType.Widget && _widgets.Contains(app))
            {
                _widgets.Remove(app);
                SaveWidgets();
                return;
            }

            if (_personalGroup?.Items.Contains(app) == true)
            {
                _personalGroup.Items.Remove(app);
                SavePersonalLinks();
            }
        }

        [RelayCommand]
        private void AddWebLink()
        {
            EnsurePersonalGroup();
            SelectedLinkGroup = _personalGroup;

            var dialog = new AddLinkWindow();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _personalGroup.Items.Add(dialog.Result);
                SavePersonalLinks();
            }
        }

        [RelayCommand]
        private void AddWidget()
        {
            var widgets = HomeAppService.GetAvailableWidgets();
            var dialog = new AddWidgetWindow(widgets);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true && dialog.SelectedWidget != null)
            {
                var widget = dialog.SelectedWidget;
                _widgets.Add(new HomeAppItem
                {
                    Type = HomeAppType.Widget,
                    Title = widget.Title,
                    Description = widget.Description,
                    WidgetKey = widget.WidgetKey,
                    Icon = widget.Icon
                });
                SaveWidgets();
            }
        }

        private void EnsurePersonalGroup()
        {
            if (_personalGroup != null)
                return;

            _personalGroup = new HomeLinkGroupViewModel
            {
                GroupId = "personal",
                Title = LanguageService.Instance["home_personal_group"],
                IsPersonal = true,
                IsVisible = true
            };
            OnPropertyChanged(nameof(PersonalLinkGroup));
        }

        private void SavePersonalLinks()
        {
            EnsurePersonalGroup();
            HomeUserConfigService.SavePersonalLinks(_personalGroup.Items);
        }

        private void SaveWidgets()
        {
            HomeUserConfigService.SaveWidgets(_widgets);
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (!IsEditMode || dropInfo.Data is not HomeAppItem source || dropInfo.TargetItem is not HomeAppItem target)
                return;

            if (source.IsReadOnly || target.IsReadOnly)
                return;

            if (!IsSameReorderScope(source, target))
                return;

            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            if (!IsEditMode || dropInfo.Data is not HomeAppItem sourceItem || dropInfo.TargetItem is not HomeAppItem targetItem)
                return;

            if (sourceItem.IsReadOnly || targetItem.IsReadOnly)
                return;

            if (_widgets.Contains(sourceItem) && _widgets.Contains(targetItem))
            {
                int sourceIndex = _widgets.IndexOf(sourceItem);
                int targetIndex = _widgets.IndexOf(targetItem);
                if (sourceIndex != -1 && targetIndex != -1)
                {
                    _widgets.Move(sourceIndex, targetIndex);
                    SaveWidgets();
                }

                return;
            }

            if (SelectedLinkGroup?.IsPersonal == true
                && SelectedLinkGroup.Items.Contains(sourceItem)
                && SelectedLinkGroup.Items.Contains(targetItem))
            {
                int sourceIndex = SelectedLinkGroup.Items.IndexOf(sourceItem);
                int targetIndex = SelectedLinkGroup.Items.IndexOf(targetItem);
                if (sourceIndex != -1 && targetIndex != -1)
                {
                    SelectedLinkGroup.Items.Move(sourceIndex, targetIndex);
                    SavePersonalLinks();
                }
            }
        }

        private bool IsSameReorderScope(HomeAppItem a, HomeAppItem b)
        {
            if (_widgets.Contains(a) && _widgets.Contains(b))
                return true;

            return SelectedLinkGroup?.IsPersonal == true
                   && SelectedLinkGroup.Items.Contains(a)
                   && SelectedLinkGroup.Items.Contains(b);
        }
    }
}
