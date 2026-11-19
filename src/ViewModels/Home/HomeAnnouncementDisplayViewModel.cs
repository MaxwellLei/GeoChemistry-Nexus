using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
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
        /// 预设主题名，见 <see cref="GeoChemistryNexus.Converter.AnnouncementBrushes.ThemeKeys"/>。
        /// 为空时跟随软件当前主题色。
        /// </summary>
        public string ThemeKey { get; init; } = string.Empty;

        /// <summary>
        /// 自定义背景色。为空时按 <see cref="ThemeKey"/> 取预设渐变。
        /// </summary>
        public string BackgroundColor { get; init; } = string.Empty;

        /// <summary>
        /// 使用网络图片作为背景。没有缓存且尚未下载完成时，仍先显示颜色渐变。
        /// </summary>
        public bool UseImageBackground { get; init; }

        public string BackgroundImageUrl { get; init; } = string.Empty;

        public bool HasTag => !string.IsNullOrWhiteSpace(Tag);

        public bool HasBody => !string.IsNullOrWhiteSpace(Body);

        public bool HasDate => !string.IsNullOrWhiteSpace(DateText);

        public bool HasAction => !string.IsNullOrWhiteSpace(ActionLabel) && !string.IsNullOrWhiteSpace(ActionUrl);

        public bool HasBackgroundImage => BackgroundImage != null;

        /// <summary>
        /// 把目录条目解析成当前语言下的展示项。文案由调用方按目标语言解析。
        /// </summary>
        public static HomeAnnouncementDisplayViewModel FromEntry(
            HomeAnnouncementEntry entry,
            Func<LocalizedString?, string> resolveText)
        {
            bool useImage = HomeAnnouncementBackground.IsImageMode(entry.BackgroundMode)
                && HomeAnnouncementBackground.IsHttpImageUrl(entry.BackgroundImageUrl);

            return new HomeAnnouncementDisplayViewModel
            {
                Id = entry.Id,
                Tag = resolveText(entry.Tag),
                Title = resolveText(entry.Title),
                Body = resolveText(entry.Body),
                DateText = entry.Date ?? string.Empty,
                ActionLabel = resolveText(entry.Action?.Label),
                ActionUrl = entry.Action?.Url ?? string.Empty,
                // 未指定预设主题，或仍是历史默认值 "blue" 时留空：公告配色跟随软件当前主题色
                ThemeKey = string.IsNullOrWhiteSpace(entry.Theme)
                    || string.Equals(entry.Theme.Trim(), "blue", StringComparison.OrdinalIgnoreCase)
                        ? string.Empty
                        : entry.Theme,
                BackgroundColor = entry.BackgroundColor?.Trim() ?? string.Empty,
                UseImageBackground = useImage,
                BackgroundImageUrl = entry.BackgroundImageUrl?.Trim() ?? string.Empty
            };
        }

        [ObservableProperty]
        private ImageSource? backgroundImage;

        [ObservableProperty]
        private bool isCurrent;

        partial void OnBackgroundImageChanged(ImageSource? value)
        {
            OnPropertyChanged(nameof(HasBackgroundImage));
        }

        public async Task LoadBackgroundImageAsync(int loadVersion, CancellationToken cancellationToken)
        {
            if (!UseImageBackground || string.IsNullOrWhiteSpace(BackgroundImageUrl))
                return;

            if (cancellationToken.IsCancellationRequested || !HomeAnnouncementImageCache.IsLoadCurrent(loadVersion))
                return;

            string url = BackgroundImageUrl.Trim();
            byte[]? cached = HomeAnnouncementImageCache.TryRead(url);
            byte[]? shown = null;
            if (cached is { Length: > 0 })
            {
                switch (await TryApplyImageAsync(cached))
                {
                    case ImageApplyResult.Applied:
                        shown = cached;
                        break;
                    case ImageApplyResult.DecodeFailed:
                        HomeAnnouncementImageCache.Delete(url, loadVersion);
                        break;
                }
            }

            if (cancellationToken.IsCancellationRequested || !HomeAnnouncementImageCache.IsLoadCurrent(loadVersion))
                return;

            try
            {
                byte[] downloaded = await ImageHttp.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                if (downloaded.Length == 0)
                    return;

                if (cancellationToken.IsCancellationRequested || !HomeAnnouncementImageCache.IsLoadCurrent(loadVersion))
                    return;

                if (shown != null && shown.AsSpan().SequenceEqual(downloaded))
                    return;

                if (await TryApplyImageAsync(downloaded) != ImageApplyResult.Applied)
                    return;

                if (cancellationToken.IsCancellationRequested || !HomeAnnouncementImageCache.IsLoadCurrent(loadVersion))
                    return;

                HomeAnnouncementImageCache.Save(url, downloaded, loadVersion);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncement] background image failed: {ex.Message}");
            }
        }

        private async Task<ImageApplyResult> TryApplyImageAsync(byte[] bytes)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                return ImageApplyResult.Unavailable;

            try
            {
                return await dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        var image = new BitmapImage();
                        using var stream = new MemoryStream(bytes);
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze();
                        BackgroundImage = image;
                        return ImageApplyResult.Applied;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[HomeAnnouncement] background image decode failed: {ex.Message}");
                        return ImageApplyResult.DecodeFailed;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HomeAnnouncement] background image apply failed: {ex.Message}");
                return ImageApplyResult.Unavailable;
            }
        }

        private enum ImageApplyResult
        {
            Applied,
            DecodeFailed,
            Unavailable
        }
    }
}
