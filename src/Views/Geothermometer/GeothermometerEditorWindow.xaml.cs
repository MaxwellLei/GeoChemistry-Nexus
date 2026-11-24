using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace GeoChemistryNexus.Views
{
    /// <summary>
    /// GeothermometerEditorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GeothermometerEditorWindow : Window, INotificationCenterHost
    {
        /// <summary>
        /// 本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现）。
        /// </summary>
        public NotificationCenter Notifications { get; } = new();

        private PropertyChangedEventHandler? _languageChanged;

        public GeothermometerEditorWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);
            GeoChemistryNexus.Helpers.WindowWorkAreaHelper.Attach(this);
            GeoChemistryNexus.Helpers.ChildWindowPlacementHelper.Attach(this);

            // 无边框窗口：交给 DWM 绘制原生阴影（与 EPMA 模板编辑窗口一致）
            SourceInitialized += (_, _) =>
            {
                int policy = 2; // DWMNCRP_ENABLED
                DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 2, ref policy, sizeof(int));
            };
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public GeothermometerEditorWindow(GeothermometerEditorViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.OnCloseRequested = () => Close();
            viewModel.ShowErrorMessage = ShowErrorMessage;
            viewModel.ShowSuccessMessage = ShowSuccessMessage;
            viewModel.OnTagSuggestionsNeedRefresh = () =>
                Dispatcher.Invoke(() => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel));

            // 构造时显式传入 VM：此时子控件 DataContext 绑定可能尚未生效
            EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel);
            Loaded += (_, _) => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel);
            _languageChanged = (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    Dispatcher.Invoke(() => EditorControl.BasicInfoPanel.RefreshTagSuggestions(viewModel));
            };
            LanguageService.Instance.PropertyChanged += _languageChanged;

            // 帮助文档页使用与 EPMA 共用的 EpmaHelpDocEditor：它会通过 IHelpDocEditorHost 自行挂接 ViewModel，
            // 这里不再需要手动接线 RTF 读写回调。
        }

        private void ShowSuccessMessage(string message) => Notifications.Success(message);

        private void ShowErrorMessage(string message) => Notifications.Error(message);

        protected override void OnClosed(EventArgs e)
        {
            if (_languageChanged != null)
            {
                LanguageService.Instance.PropertyChanged -= _languageChanged;
                _languageChanged = null;
            }

            if (DataContext is GeothermometerEditorViewModel viewModel)
                viewModel.Release();

            Notifications.Dispose();
            base.OnClosed(e);
        }
    }
}
