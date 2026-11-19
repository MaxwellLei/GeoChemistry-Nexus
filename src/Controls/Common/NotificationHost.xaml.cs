using GeoChemistryNexus.Services;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 通知中心的呈现控件：把 <see cref="NotificationCenter"/> 中的通知渲染为右侧堆叠的 Toast 卡片。
    /// 主窗口绑定全局 <see cref="NotificationManager"/>，其他窗口绑定各自的 <see cref="NotificationCenter"/>。
    /// </summary>
    public partial class NotificationHost : UserControl
    {
        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register(
                nameof(Center),
                typeof(NotificationCenter),
                typeof(NotificationHost),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ShowDialogsProperty =
            DependencyProperty.Register(
                nameof(ShowDialogs),
                typeof(bool),
                typeof(NotificationHost),
                new PropertyMetadata(true));

        public NotificationHost()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 要呈现的通知中心。
        /// </summary>
        public NotificationCenter? Center
        {
            get => (NotificationCenter?)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        /// <summary>
        /// 是否在本控件内呈现交互对话框（确认 / 三按钮 / 单选）。
        /// 主窗口使用独立的居中 Sheet 层时设为 false。
        /// </summary>
        public bool ShowDialogs
        {
            get => (bool)GetValue(ShowDialogsProperty);
            set => SetValue(ShowDialogsProperty, value);
        }
    }
}
