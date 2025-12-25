using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    public partial class AnnouncementViewModel : ObservableObject
    {
        [ObservableProperty]
        private string announcementText = "正在加载公告...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy = false;

        public bool IsNotBusy => !IsBusy;

        public AnnouncementViewModel()
        {
            _ = LoadAnnouncement();
        }

        [RelayCommand]
        private async Task LoadAnnouncement()
        {
            if (IsBusy) return;
            IsBusy = true;
            AnnouncementText = LanguageService.Instance["loading_announcements"];   // 正在加载公告....

            try
            {
                string json = await UpdateHelper.GetUrlContentAsync();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var serverInfo = JsonSerializer.Deserialize<ServerInfo>(json);
                    if (serverInfo != null && !string.IsNullOrWhiteSpace(serverInfo.Announcement))
                    {
                        AnnouncementText = serverInfo.Announcement;
                    }
                    else
                    {
                        // 暂无公告
                        AnnouncementText = LanguageService.Instance["no_announcements"];
                    }
                }
                else
                {
                    // 无法获取公告信息。
                    AnnouncementText = LanguageService.Instance["unable_to_get_announcements"];
                }
            }
            catch (Exception ex)
            {
                // 获取公告失败: Message
                AnnouncementText = LanguageService.Instance["failed_to_get_announcements"] + $" {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
