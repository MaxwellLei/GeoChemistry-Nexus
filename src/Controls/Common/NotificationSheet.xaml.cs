using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 居中 Sheet 对话框（确认 / 三按钮 / 单选）。
    /// 仅负责视图层的初始焦点：让 Enter / Esc 快捷键一出现就能直接生效，业务逻辑全部在 NotificationViewModel。
    /// </summary>
    public partial class NotificationSheet : UserControl
    {
        public NotificationSheet()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();
        }
    }
}
