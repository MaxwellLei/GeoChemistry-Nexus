using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
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
        public string TemplatePath { get; set; }
        
        // 本地 JSON 文件完整路径
        public string LocalFilePath { get; set; }

        [ObservableProperty]
        private string _thumbnailPath;

        public string Category { get; set; }

        // 服务器端哈希 (用于校验)
        public string ServerHash { get; set; }

        public bool IsCustomTemplate { get; set; }

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
            TemplateState.NotDownloaded => "点击下载",
            TemplateState.UpdateAvailable => "发现新版本",
            TemplateState.Downloading => "下载中...",
            TemplateState.Error => "重试",
            TemplateState.Loading => "加载中...",
            _ => ""
        };

        // --- 委托事件 ---
        // 将具体的“打开”和“下载”逻辑交给 MainViewModel 实现
        public Func<TemplateCardViewModel, Task> DownloadHandler { get; set; }
        public Func<TemplateCardViewModel, Task> OpenHandler { get; set; }

        [RelayCommand]
        private async Task CardClick()
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

        [RelayCommand]
        private async Task Update()
        {
            if (DownloadHandler != null) await DownloadHandler(this);
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

            // 如果是自定义模板，始终为 Ready
            if (IsCustomTemplate)
            {
                State = TemplateState.Ready;
                return;
            }

            // 如果没有设置本地路径，默认为未下载
            if (string.IsNullOrEmpty(LocalFilePath))
            {
                State = TemplateState.NotDownloaded;
                return;
            }

            await Task.Run(() =>
            {
                if (!File.Exists(LocalFilePath))
                {
                    // 文件不存在
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        State = TemplateState.NotDownloaded;
                    });
                }
                else
                {
                    // 文件存在，计算 MD5
                    string localHash = UpdateHelper.ComputeFileMd5(LocalFilePath);

                    // 只有哈希完全匹配才认为是 Ready
                    
                    bool isReady = false;
                    if (!string.IsNullOrEmpty(ServerHash) && 
                        string.Equals(localHash, ServerHash, StringComparison.OrdinalIgnoreCase))
                    {
                        isReady = true;
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        State = isReady ? TemplateState.Ready : TemplateState.UpdateAvailable;
                    });
                }
            });
        }
    }

    // 模板状态枚举
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
