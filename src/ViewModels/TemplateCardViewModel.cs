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
        public string Name { get; set; }
        public Guid? TemplateId { get; set; }
        public string TemplatePath { get; set; }
        
        // 本地 JSON 文件完整路径
        public string LocalFilePath { get; set; }

        [ObservableProperty]
        private string _thumbnailPath;

        public string Category { get; set; }

        // 服务器端哈希 (用于校验)
        public string ServerHash { get; set; }

        [ObservableProperty]
        private bool _isCustomTemplate;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private bool _isCtrlOverlayVisible; // Ctrl键遮罩显示状态

        [ObservableProperty]
        private ImageSource _thumbnailImage; // 支持动态修改（下载前是默认图，下载后是真实图）

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StateText))]
        private TemplateState _state = TemplateState.NotDownloaded;

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
            _ => ""
        };

        // --- 委托事件 ---
        // 将具体的"打开"和"下载"逻辑交给 MainViewModel 实现
        public Func<TemplateCardViewModel, Task> DownloadHandler { get; set; }
        public Func<TemplateCardViewModel, Task> OpenHandler { get; set; }
        public Func<TemplateCardViewModel, Task> CheckUpdateHandler { get; set; }
        public Func<TemplateCardViewModel, Task> ToggleFavoriteHandler { get; set; }
        public Func<TemplateCardViewModel, Task> DeleteHandler { get; set; }
        public Func<TemplateCardViewModel, Task> EditHandler { get; set; }

        private bool _isProcessing = false;

        [RelayCommand]
        private async Task CardClick()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            try
            {
                if (State == TemplateState.Ready || State == TemplateState.Loading || State == TemplateState.UpdateAvailable)
                {
                    if (OpenHandler != null) await OpenHandler(this);
                }
                else if (State == TemplateState.NotDownloaded || State == TemplateState.Error)
                {
                    if (DownloadHandler != null) await DownloadHandler(this);
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
            _isProcessing = true;
            try
            {
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
        /// 快捷收藏 - Ctrl+卡片右侧点击
        /// </summary>
        [RelayCommand]
        private async Task QuickFavorite()
        {
            await ToggleFavorite();
        }

        /// <summary>
        /// 快捷删除 - Ctrl+卡片左侧点击
        /// </summary>
        [RelayCommand]
        private async Task QuickDelete()
        {
            // 只有自定义模板才能删除
            if (!IsCustomTemplate || !TemplateId.HasValue) return;

            // 直接调用删除处理，由 MainPlotViewModel 实现
            if (DeleteHandler != null)
            {
                await DeleteHandler(this);
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
        Loading         // 正在加载/检查中
    }
}
