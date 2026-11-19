using GeoChemistryNexus.Controls.GeothermometerEditorPanels;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class GeothermometerEditorControl : UserControl
    {
        public GeothermometerEditorControl()
        {
            InitializeComponent();
        }

        public GeoTBasicInfoPanel BasicInfoPanel => BasicPanel;

        public GeoTHelpDocPanel HelpDocPanel => HelpPanel;

        // 自绘标题栏的窗口控制按钮（纯窗口行为，与业务逻辑无关）
        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is { } window)
                window.WindowState = WindowState.Minimized;
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is { } window)
                window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}
