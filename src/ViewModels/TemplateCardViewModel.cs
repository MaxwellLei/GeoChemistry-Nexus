using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
        //public string ThumbnailPath { get; set; }
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

        // 辅助文本，用于前端显示提示（可选）
        public string StateText => State switch
        {
            TemplateState.NotDownloaded => "点击下载",
            TemplateState.UpdateAvailable => "发现新版本",
            TemplateState.Downloading => "下载中...",
            TemplateState.Error => "重试",
            _ => ""
        };

        // --- 委托事件 ---
        // 为了解耦，我们将具体的“打开”和“下载”逻辑交给 MainViewModel 实现
        public Func<TemplateCardViewModel, Task> DownloadHandler { get; set; }
        public Action<TemplateCardViewModel> OpenHandler { get; set; }

        // --- 命令 ---
        [RelayCommand]
        private async Task CardClick()
        {
            // 如果正在下载，禁止点击
            if (State == TemplateState.Downloading) return;

            if (State == TemplateState.Ready)
            {
                // 状态为 Ready -> 打开模板
                OpenHandler?.Invoke(this);
            }
            else
            {
                // 状态为 未下载/需更新/错误 -> 触发下载
                if (DownloadHandler != null)
                {
                    await DownloadHandler(this);
                }
            }
        }
    }

    // 模板状态枚举
    public enum TemplateState
    {
        Ready,          // 已就绪，可直接打开
        NotDownloaded,  // 本地不存在，需要下载
        UpdateAvailable,// 本地存在但哈希不匹配，需要更新
        Downloading,    // 正在下载/解压中
        Error           // 下载或校验失败
    }
}
