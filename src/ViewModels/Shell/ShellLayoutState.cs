using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 主窗口外壳布局。页面会在启动动画播放期间实例化，此时主窗口尚未创建，
    /// 不能通过 MainWindow.DataContext 读取侧栏状态。
    /// </summary>
    public partial class ShellLayoutState : ObservableObject
    {
        public static ShellLayoutState Instance { get; } = new();

        [ObservableProperty]
        private bool _isSideBarVisible = true;

        [ObservableProperty]
        private bool _useIconOnlySidebarCollapse;
    }
}
