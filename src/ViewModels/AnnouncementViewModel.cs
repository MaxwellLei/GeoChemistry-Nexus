using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Services;
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
            AnnouncementText = LanguageService.Instance["loading_announcements"];

            try
            {
                string text = await ServerAnnouncementService.LoadAnnouncementAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AnnouncementText = text;
                }
                else
                {
                    AnnouncementText = LanguageService.Instance["no_announcements"];
                }
            }
            catch (System.Exception ex)
            {
                AnnouncementText = LanguageService.Instance["failed_to_get_announcements"] + $" {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
