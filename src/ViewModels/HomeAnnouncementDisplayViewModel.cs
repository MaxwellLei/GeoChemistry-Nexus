using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 公告的展示态：文案已按当前界面语言解析完成。
    /// </summary>
    public partial class HomeAnnouncementDisplayViewModel : ObservableObject
    {
        private static readonly HttpClient ImageHttp = new()
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        public string Id { get; init; } = string.Empty;

        public string Tag { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public string DateText { get; init; } = string.Empty;

        public string ActionLabel { get; init; } = string.Empty;

        public string ActionUrl { get; init; } = string.Empty;

        /// <summary>
        /// 预设主题名（blue / purple / teal / orange）
        /// </summary>
        public string ThemeKey { get; init; } = "blue";

        /// <summary>
        /// 自定义背景色。为空时按 <see cref="ThemeKey"/> 取预设渐变。
        /// </summary>
        public string BackgroundColor { get; init; } = string.Empty;

        /// <summary>
        /// 使用网络图片作为背景。加载完成前仍先显示颜色渐变。
        /// </summary>
        public bool UseImageBackground { get; init; }

        public string BackgroundImageUrl { get; init; } = string.Empty;

        /// <summary>
        /// 颜色背景下是否绘制装饰图形。图片背景不绘制。
        /// </summary>
        public bool ShowPolygons { get; init; }

        /// <summary>
        /// 本版本实际绘制的样式 id，一定是 <see cref="HomeAnnouncementBackground.KnownPolygonStyles"/> 中的一项。
        /// </summary>
        public string PolygonStyle { get; init; } = HomeAnnouncementBackground.PolygonRing;

        public bool HasTag => !string.IsNullOrWhiteSpace(Tag);

        public bool HasBody => !string.IsNullOrWhiteSpace(Body);

        public bool HasDate => !string.IsNullOrWhiteSpace(DateText);

        public bool HasAction => !string.IsNullOrWhiteSpace(ActionLabel) && !string.IsNullOrWhiteSpace(ActionUrl);

        public bool HasBackgroundImage => BackgroundImage != null;

        [ObservableProperty]
        private ImageSource? backgroundImage;

        [ObservableProperty]
        private bool isCurrent;

        partial void OnBackgroundImageChanged(ImageSource? value)
        {
            OnPropertyChanged(nameof(HasBackgroundImage));
        }

        public async Task LoadBackgroundImageAsync()
        {
            if (!UseImageBackground || string.IsNullOrWhiteSpace(BackgroundImageUrl))
                return;

            try
            {
                byte[] bytes = await ImageHttp.GetByteArrayAsync(BackgroundImageUrl).ConfigureAwait(false);
                Dispatcher? dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                await dispatcher.InvokeAsync(() =>
                {
                    var image = new BitmapImage();
                    using var stream = new MemoryStream(bytes);
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    BackgroundImage = image;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncement] background image failed: {ex.Message}");
            }
        }
    }
}
