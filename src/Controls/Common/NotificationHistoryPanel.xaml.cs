using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    /// <summary>
    /// 通知中心历史面板（纯视图，DataContext 为 <see cref="Services.NotificationCenter"/>）。
    /// </summary>
    public partial class NotificationHistoryPanel : UserControl
    {
        public NotificationHistoryPanel()
        {
            InitializeComponent();
        }
    }
}
