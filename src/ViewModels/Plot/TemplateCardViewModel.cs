using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GeoChemistryNexus.ViewModels
{
    // 模板卡片视图模型
    public partial class TemplateCardViewModel : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public Guid? TemplateId { get; set; }
        public string TemplatePath { get; set; } = string.Empty;

        [ObservableProperty]
        private string _thumbnailPath = string.Empty;

        public string Category { get; set; } = string.Empty;

        // 服务器端哈希 (用于校验)
        public string ServerHash { get; set; } = string.Empty;

        // 服务器端版本 (用于 z 修订比较)
        public string ServerVersion { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isCustomTemplate;

        /// <summary>
        /// 是否为由个人图解转换为内置模板且尚未推送到服务器的卡片（开发者模式标识用）
        /// </summary>
        [ObservableProperty]
        private bool _isNewTemplate;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private bool _isCtrlOverlayVisible; // 右 Alt 按住时的操作遮罩

        [ObservableProperty]
        private ImageSource? _thumbnailImage; // 支持动态修改（下载前是默认图，下载后是真实图）

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        [NotifyPropertyChangedFor(nameof(IsNotDownloaded))]
        private TemplateState _state = TemplateState.NotDownloaded;

        /// <summary>
        /// 官方模板尚未下载（只有目录占位，没有内容）：右键菜单隐藏编辑、导出、移除官方等需要本地内容的操作。
        /// </summary>
        public bool IsNotDownloaded => State is TemplateState.NotDownloaded or TemplateState.RequiresAppUpgrade or TemplateState.Downloading;

        [ObservableProperty]
        private double _downloadProgress;

        // 辅助文本，用于前端显示提示
        public string StateText => State switch
        {
            // 点击下载
            TemplateState.NotDownloaded => LanguageService.Instance["click_to_download"],
            // 发现新版本
            TemplateState.UpdateAvailable => LanguageService.Instance["new_version_found"],
            // 下载中
            TemplateState.Downloading => LanguageService.Instance["downloading_status"],
            // 重试
            TemplateState.Error => LanguageService.Instance["retry_action"],
            // 加载中
            TemplateState.Loading => LanguageService.Instance["loading_status"],
            // 服务器版本需要升级软件（本地未安装 / 本地已有旧版）
            TemplateState.RequiresAppUpgrade => LanguageService.Instance["status_requires_app_upgrade"],
            TemplateState.UpdateRequiresAppUpgrade => LanguageService.Instance["status_update_requires_app_upgrade"],
            _ => ""
        };

        // --- 委托事件 ---
        // 将具体的"打开"和"下载"逻辑交给 MainViewModel 实现
        public Func<TemplateCardViewModel, Task>? DownloadHandler { get; set; }
        /// <summary>更新已下载的官方模板前的确认（更新会覆盖本地修改）；返回 false 则取消。</summary>
        public Func<TemplateCardViewModel, Task<bool>>? ConfirmUpdateHandler { get; set; }
        public Func<TemplateCardViewModel, Task>? OpenHandler { get; set; }
        public Func<TemplateCardViewModel, Task>? CheckUpdateHandler { get; set; }
        public Func<TemplateCardViewModel, Task>? ToggleFavoriteHandler { get; set; }
        public Func<TemplateCardViewModel, bool, Task>? DeleteHandler { get; set; }
        public Func<TemplateCardViewModel, Task>? EditHandler { get; set; }

        private bool _isProcessing = false;

        [RelayCommand]
        private async Task CardClick()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            try
            {
                if (State == TemplateState.Ready || State == TemplateState.Loading || State == TemplateState.UpdateAvailable
                    || State == TemplateState.UpdateRequiresAppUpgrade)
                {
                    if (OpenHandler != null) await OpenHandler(this);
                }
                else if (State == TemplateState.NotDownloaded || State == TemplateState.Error)
                {
                    if (DownloadHandler != null) await DownloadHandler(this);
                }
                else if (State == TemplateState.RequiresAppUpgrade)
                {
                    // 本地没有可用版本，不下载也不打开，只提示升级软件
                    MessageHelper.Warning(LanguageService.GetString("status_requires_app_upgrade_hint", "需升级软件"));
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        [RelayCommand]
        private async Task Update()
        {
            if (_isProcessing) return;
            if (State is TemplateState.RequiresAppUpgrade or TemplateState.UpdateRequiresAppUpgrade) return;
            _isProcessing = true;
            try
            {
                if (State == TemplateState.UpdateAvailable && ConfirmUpdateHandler != null
                    && !await ConfirmUpdateHandler(this))
                    return;

                if (DownloadHandler != null) await DownloadHandler(this);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 检查文件状态 (Lazy Load)
        /// 当卡片显示在视图中时触发
        /// </summary>
        [RelayCommand]
        private async Task CheckReadiness()
        {
            // 只有在 Loading 状态下才进行检查 (避免重复检查)
            if (State != TemplateState.Loading) return;
            if (_isProcessing) return;

            _isProcessing = true;
            try
            {
                if (CheckUpdateHandler != null)
                {
                    await CheckUpdateHandler(this);
                    return;
                }

                // 如果是数据库模板，默认为 Ready (具体状态应由 CheckUpdateHandler 判断)
                if (TemplateId.HasValue)
                {
                    State = TemplateState.Ready;
                    return;
                }

                // 非数据库模板视为错误或未下载
                State = TemplateState.NotDownloaded;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavorite()
        {
            if (ToggleFavoriteHandler != null)
            {
                await ToggleFavoriteHandler(this);
            }
        }

        /// <summary>
        /// 快捷收藏 - 按住右 Alt 后点击卡片右侧
        /// </summary>
        [RelayCommand]
        private async Task QuickFavorite()
        {
            await ToggleFavorite();
        }

        /// <summary>
        /// 快捷删除 - 按住右 Alt 后点击卡片左侧（跳过二次确认）
        /// </summary>
        [RelayCommand]
        private async Task QuickDelete()
        {
            // 仅自定义模板支持右 Alt 快捷删除
            if (!IsCustomTemplate || !TemplateId.HasValue) return;

            // 直接调用删除处理，由 MainPlotViewModel 实现，跳过二次确认
            if (DeleteHandler != null)
            {
                await DeleteHandler(this, true);
            }
        }
    }

    // 模板状态枚举
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum TemplateState
    {
        Ready,          // 已就绪，可直接打开
        NotDownloaded,  // 本地不存在，需要下载
        UpdateAvailable,// 本地存在但哈希不匹配，需要更新
        Downloading,    // 正在下载/解压中
        Error,          // 下载或校验失败
        Loading,        // 正在加载/检查中
        RequiresAppUpgrade,       // 本地未安装，服务器版本需要升级软件：不能下载也不能打开
        UpdateRequiresAppUpgrade  // 本地已有旧版，服务器新版需要升级软件：仍可打开旧版，不能更新
    }
}
