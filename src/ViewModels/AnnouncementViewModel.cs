using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
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
            AnnouncementText = "正在加载公告...";

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
                        AnnouncementText = "暂无公告。";
                    }
                }
                else
                {
                    AnnouncementText = "无法获取公告信息。";
                }
            }
            catch (Exception ex)
            {
                AnnouncementText = $"获取公告失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
