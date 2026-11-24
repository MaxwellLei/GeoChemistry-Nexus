using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Converter;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 官方发布器：编辑语言、草稿自动保存、发布单辅助操作、按页发布。
    /// 维护者的日常流程是“改内容 → 看预览 → 发布”，这里负责让这条路上不丢东西、少点几下。
    /// </summary>
    public partial class OfficialTemplatePublisherViewModel
    {
        // ───────────────────────── 编辑语言 ─────────────────────────

        /// <summary>编辑多语言内容时可选的语言。</summary>
        public System.Collections.Generic.IReadOnlyList<CultureOption> EditLanguageOptions { get; } =
            AppCultureRegistry.GetOptions(false);

        /// <summary>
        /// 当前正在编辑的内容语言。公告、导航链接、矿物分类共用同一个，切换一次所有编辑器一起变。
        /// </summary>
        [ObservableProperty]
        private string selectedEditLanguage = AppCultureRegistry.DefaultContentLanguage;

        partial void OnSelectedEditLanguageChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            SelectedHomeLinksLanguage = value;
            SelectedGeoTMineralCategoriesLanguage = value;
            SelectedAnnouncementsLanguage = value;
            RefreshAnnouncementTranslationChips();
            RebuildAnnouncementLivePreview();
        }

        // ───────────────────────── 发布单状态 ─────────────────────────

        private bool AnnouncementPackHasChanges =>
            AnnouncementHasRemoteChanges || MinimumSupportedVersionHasRemoteChanges || LatestAppVersionHasRemoteChanges
            || InstallerHasRemoteChanges;

        private bool HomeLinksPackHasChanges => HomeLinksHasRemoteChanges;

        private bool DiagramPackHasChanges => DiagramNewCount + DiagramUpdatedCount + DiagramRemovedCount > 0;

        private bool GeoPackHasChanges => GeoNewCount + GeoUpdatedCount + GeoRemovedCount > 0;

        private bool EpmaPackHasChanges => EpmaNewCount + EpmaUpdatedCount + EpmaRemovedCount > 0;

        private int SelectedPackCount =>
            (PublishAnnouncement ? 1 : 0)
            + (PublishHomeLinks ? 1 : 0)
            + (PublishDiagrams ? 1 : 0)
            + (PublishGeothermometers ? 1 : 0)
            + (PublishEpma ? 1 : 0);

        private int ChangedSelectedPackCount =>
            (PublishAnnouncement && AnnouncementPackHasChanges ? 1 : 0)
            + (PublishHomeLinks && HomeLinksPackHasChanges ? 1 : 0)
            + (PublishDiagrams && DiagramPackHasChanges ? 1 : 0)
            + (PublishGeothermometers && GeoPackHasChanges ? 1 : 0)
            + (PublishEpma && EpmaPackHasChanges ? 1 : 0);

        /// <summary>发布单顶部的一句话摘要：选了几项、几项有改动、发到哪里。</summary>
        public string ReleaseSummaryText
        {
            get
            {
                int selected = SelectedPackCount;
                if (selected == 0)
                    return "还没有选择要发布的内容。勾选下面的条目，或点“勾选有改动的”。";

                int changed = ChangedSelectedPackCount;
                string change = changed == 0 ? "都没有检测到改动" : $"其中 {changed} 项有改动";
                string servers = EnabledTargetCount == 0
                    ? "，但没有启用的服务器"
                    : $"，将发布到 {EnabledTargetCount} 台服务器";
                return $"已选 {selected} 项，{change}{servers}。";
            }
        }

        /// <summary>只勾选检测到有改动的内容。</summary>
        [RelayCommand]
        private void SelectChangedPacks()
        {
            PublishAnnouncement = AnnouncementPackHasChanges;
            PublishHomeLinks = HomeLinksPackHasChanges;
            PublishDiagrams = DiagramPackHasChanges;
            PublishGeothermometers = GeoPackHasChanges;
            PublishEpma = EpmaPackHasChanges;

            if (SelectedPackCount == 0)
            {
                Log("没有检测到改动。");
                ShowWarning("没有检测到改动。如果刚改过本地模板，先点右上角“刷新预览”。");
            }
        }

        [RelayCommand]
        private void ClearPackSelection()
        {
            PublishAnnouncement = false;
            PublishHomeLinks = false;
            PublishDiagrams = false;
            PublishGeothermometers = false;
            PublishEpma = false;
        }

        // ───────────────────────── 按页发布 ─────────────────────────

        /// <summary>
        /// 只发布当前这一页的内容，不动发布单里的勾选。发一条公告不用再回发布单取消其他项。
        /// </summary>
        [RelayCommand]
        private async Task PublishSectionOnly(string? section)
        {
            if (IsBusy)
                return;

            if (section is not (SectionAnnouncement or SectionHomeLinks or SectionDiagrams or SectionGeothermometers or SectionEpma))
                return;

            var snapshot = (PublishAnnouncement, PublishHomeLinks, PublishDiagrams, PublishGeothermometers, PublishEpma);

            PublishAnnouncement = section == SectionAnnouncement;
            PublishHomeLinks = section == SectionHomeLinks;
            PublishDiagrams = section == SectionDiagrams;
            PublishGeothermometers = section == SectionGeothermometers;
            PublishEpma = section == SectionEpma;

            try
            {
                await PublishToCos();
            }
            finally
            {
                (PublishAnnouncement, PublishHomeLinks, PublishDiagrams, PublishGeothermometers, PublishEpma) = snapshot;
            }
        }

        // ───────────────────────── 启动流程 ─────────────────────────

        private bool _keepAnnouncementBaseline;

        /// <summary>
        /// 打开发布器时：先拉服务器上的公告与版本，再恢复上次没发布的草稿，最后自动刷新一次预览，
        /// 这样发布单上的数字一打开就是真实的，不用再手动点“刷新预览”。
        /// </summary>
        private async Task InitializeRemoteStateAsync()
        {
            try
            {
                await LoadAnnouncementFromServerAsync();
                await LoadAnnouncementsCatalogFromServerCoreAsync();

                if (TryApplyAnnouncementDraft())
                {
                    PublishAnnouncement = true;
                    Log("已恢复上次没发布的公告草稿。如果服务器内容之后又更新过，请点“重新载入”放弃草稿。");
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            finally
            {
                _draftSavingSuspended = false;
            }

            if (IsDeveloperMode)
                await RefreshPreview();
        }

        /// <summary>
        /// 放弃编辑器里没发布的公告修改，重新以服务器为准。有未发布修改时先确认。
        /// </summary>
        [RelayCommand]
        private async Task LoadAnnouncementsEditorFromServerAsync()
        {
            if (IsBusy)
                return;

            if (_announcementsCatalogRemoteKnown && AnnouncementPackHasChanges)
            {
                bool confirmed = await ShowConfirmAsync(
                    "重新载入会丢掉还没发布的公告和版本修改，继续吗？",
                    "取消",
                    "重新载入");
                if (!confirmed)
                    return;
            }

            await LoadAnnouncementFromServerAsync();
            await LoadAnnouncementsCatalogFromServerCoreAsync();
        }

        // ───────────────────────── 草稿自动保存 ─────────────────────────

        [Flags]
        private enum PublisherDraftKinds
        {
            None = 0,
            Announcements = 1,
            HomeLinks = 2,
            GeoTCategories = 8
        }

        // 启动阶段（正在从服务器加载）不保存，避免把还没加载完的空编辑器写成草稿
        private bool _draftSavingSuspended = true;
        private PublisherDraftKinds _pendingDrafts;
        private CancellationTokenSource? _draftSaveCts;

        private static string AnnouncementDraftPath =>
            AppDataPathHelper.GetDataPath("Config", "Announcements.publisher.json");

        private static JsonSerializerOptions AnnouncementDraftJsonOptions =>
            JsonHelper.CreateRelaxedIndentedOptions(new LocalizedStringJsonConverter());

        /// <summary>
        /// 编辑后稍等片刻自动保存，关闭窗口或崩溃后重新打开不会丢内容。
        /// </summary>
        private void RequestDraftSave(PublisherDraftKinds kind)
        {
            if (_draftSavingSuspended)
                return;

            _pendingDrafts |= kind;
            _draftSaveCts?.Cancel();
            var cts = new CancellationTokenSource();
            _draftSaveCts = cts;
            _ = SaveDraftsAfterDelayAsync(cts.Token);
        }

        private async Task SaveDraftsAfterDelayAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(800, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            FlushPendingDrafts();
        }

        /// <summary>立即写出所有待保存的草稿。窗口关闭时调用。</summary>
        public void FlushPendingDrafts()
        {
            _draftSaveCts?.Cancel();
            _draftSaveCts = null;

            var pending = _pendingDrafts;
            _pendingDrafts = PublisherDraftKinds.None;
            if (pending == PublisherDraftKinds.None)
                return;

            try
            {
                if (pending.HasFlag(PublisherDraftKinds.HomeLinks))
                    SaveHomeLinksToLocal();
                if (pending.HasFlag(PublisherDraftKinds.GeoTCategories))
                    SaveGeoTMineralCategoriesToLocal();
                if (pending.HasFlag(PublisherDraftKinds.Announcements))
                    SaveAnnouncementDraft();
            }
            catch (Exception ex)
            {
                Log("自动保存草稿失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 公告草稿：和服务器不一致时写入本地文件，一致（已发布或已放弃修改）时删除。
        /// </summary>
        private void SaveAnnouncementDraft()
        {
            string path = AnnouncementDraftPath;

            if (_announcementsCatalogRemoteKnown)
            {
                if (!AnnouncementPackHasChanges)
                {
                    DeleteAnnouncementDraft(path);
                    return;
                }
            }
            else if (AnnouncementEntries.Count == 0)
            {
                // 没拿到服务器内容、编辑器又是空的：不要用空内容覆盖已有草稿
                return;
            }

            var draft = new AnnouncementPublisherDraft
            {
                Catalog = new HomeAnnouncementCatalog
                {
                    Version = 1,
                    Announcements = AnnouncementEntries.Select(item => item.Entry).ToList()
                },
                MinimumSupportedVersion = MinimumSupportedVersionText ?? string.Empty,
                LatestAppVersion = LatestAppVersionText ?? string.Empty,
                FullInstallerSha256 = FullInstallerSha256Text ?? string.Empty,
                LiteInstallerSha256 = LiteInstallerSha256Text ?? string.Empty,
                RequiredRuntimeMajor = RequiredRuntimeMajorText ?? string.Empty,
                LegacyText = AnnouncementText ?? string.Empty
            };

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonSerializer.Serialize(draft, AnnouncementDraftJsonOptions));
        }

        private static void DeleteAnnouncementDraft(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PublisherDraft] delete failed: {ex.Message}");
            }
        }

        private bool TryApplyAnnouncementDraft()
        {
            string path = AnnouncementDraftPath;
            if (!File.Exists(path))
                return false;

            try
            {
                var draft = JsonSerializer.Deserialize<AnnouncementPublisherDraft>(
                    File.ReadAllText(path),
                    AnnouncementDraftJsonOptions);
                if (draft?.Catalog?.Announcements == null)
                    return false;

                ReplaceAnnouncementEntries(draft.Catalog);
                MinimumSupportedVersionText = draft.MinimumSupportedVersion ?? string.Empty;
                LatestAppVersionText = draft.LatestAppVersion ?? string.Empty;
                FullInstallerSha256Text = draft.FullInstallerSha256 ?? string.Empty;
                LiteInstallerSha256Text = draft.LiteInstallerSha256 ?? string.Empty;
                RequiredRuntimeMajorText = string.IsNullOrWhiteSpace(draft.RequiredRuntimeMajor)
                    ? DefaultRequiredRuntimeMajorText
                    : draft.RequiredRuntimeMajor;
                AnnouncementText = draft.LegacyText ?? string.Empty;
                UpdateServerConfigChangeState();
                return true;
            }
            catch (Exception ex)
            {
                Log("公告草稿读取失败，已忽略：" + ex.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// 公告编辑器的本地草稿：公告目录加上版本号、旧版单行公告。
    /// </summary>
    public sealed class AnnouncementPublisherDraft
    {
        [JsonPropertyName("catalog")]
        public HomeAnnouncementCatalog Catalog { get; set; } = new();

        [JsonPropertyName("minimumSupportedVersion")]
        public string MinimumSupportedVersion { get; set; } = string.Empty;

        [JsonPropertyName("latestAppVersion")]
        public string LatestAppVersion { get; set; } = string.Empty;

        [JsonPropertyName("fullInstallerSha256")]
        public string FullInstallerSha256 { get; set; } = string.Empty;

        [JsonPropertyName("liteInstallerSha256")]
        public string LiteInstallerSha256 { get; set; } = string.Empty;

        [JsonPropertyName("requiredRuntimeMajor")]
        public string RequiredRuntimeMajor { get; set; } = string.Empty;

        [JsonPropertyName("legacyText")]
        public string LegacyText { get; set; } = string.Empty;
    }
}
