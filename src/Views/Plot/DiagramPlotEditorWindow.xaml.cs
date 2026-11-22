using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace GeoChemistryNexus.Views
{
    public partial class DiagramPlotEditorWindow : Window, INotificationCenterHost
    {
        /// <summary>
        /// 本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现）。
        /// </summary>
        public NotificationCenter Notifications { get; } = new();

        public DiagramPlotEditorControl Editor => EditorControl;

        public DiagramPlotEditorWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);
            GeoChemistryNexus.Helpers.WindowWorkAreaHelper.Attach(this);
            EditorControl.ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DiagramPlotEditorViewModel.WindowTitle))
                    Title = EditorControl.ViewModel.WindowTitle;
            };
            Title = EditorControl.ViewModel.WindowTitle;
            EditorControl.ViewModel.ExportNotificationCenter = Notifications;

            // 无边框窗口：交给 DWM 绘制原生阴影（与 EPMA 模板编辑窗口一致）
            SourceInitialized += (_, _) =>
            {
                int policy = 2; // DWMNCRP_ENABLED
                DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 2, ref policy, sizeof(int));
                FitToOwner();
            };
            Loaded += (_, _) => FitToOwner();
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>高度跟随主窗口可用区域，上限 700，与 EPMA 新建计算模板窗口一致。</summary>
        private void FitToOwner()
        {
            if (WindowState != WindowState.Normal) return;
            var owner = Owner ?? Application.Current?.MainWindow;
            if (ReferenceEquals(owner, this)) owner = null;
            double ownerHeight = owner is { ActualHeight: > 0 } ? owner.ActualHeight : owner?.Height ?? SystemParameters.WorkArea.Height;
            if (double.IsNaN(ownerHeight) || double.IsInfinity(ownerHeight)) ownerHeight = SystemParameters.WorkArea.Height;
            double available = Math.Min(ownerHeight - 72, SystemParameters.WorkArea.Height - 48);
            double initialHeight = Math.Max(360, Math.Min(700, available));
            MinHeight = Math.Min(560, initialHeight);
            Height = initialHeight;
        }

        private void ConfirmClick(object sender, RoutedEventArgs e) => EditorControl.Confirm();

        private void CancelClick(object sender, RoutedEventArgs e) => EditorControl.Cancel();

        private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeClick(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        public void InitializeForCreate()
        {
            EditorControl.InitializeForCreate();
        }

        public void InitializeForEdit(GraphMapTemplateEntity entity)
        {
            EditorControl.InitializeForEdit(entity);
        }

        private DispatcherTimer? _viewOnlyTimer;

        /// <summary>
        /// 只读查看（非开发者查看官方图解）：加载内容，隐藏确认按钮，并持续把编辑控件置为只读，
        /// 切换分区后才生成的控件也会被覆盖到。
        /// </summary>
        public void InitializeForView(GraphMapTemplateEntity entity)
        {
            EditorControl.InitializeForEdit(entity);
            EditorControl.ViewModel.EnterViewOnlyMode();
            EditorControl.ViewModel.WindowTitle = LanguageService.GetString("view_diagram_template", "View Diagram Template (Read-only)");
            ConfirmButton.Visibility = Visibility.Collapsed;

            _viewOnlyTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(250) };
            _viewOnlyTimer.Tick += (_, _) => LockEditorControls(EditorControl);
            _viewOnlyTimer.Start();
            Loaded += (_, _) => LockEditorControls(EditorControl);
        }

        private void LockEditorControls(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                switch (child)
                {
                    case TextBox textBox:
                        if (!textBox.IsReadOnly) textBox.IsReadOnly = true;
                        break;
                    case ComboBox comboBox:
                        if (comboBox.IsEnabled) comboBox.IsEnabled = false;
                        break;
                    case RadioButton radioButton:
                        // 左侧分区导航保持可用
                        if (radioButton.IsEnabled && !(radioButton.Name?.StartsWith("Nav", StringComparison.Ordinal) ?? false))
                            radioButton.IsEnabled = false;
                        break;
                    case Button button:
                        // 测试脚本只读取脚本并输出结果，不修改模板，保留；其余按钮（增删语言/分类、导入翻译等）全部禁用
                        if (button.IsEnabled && !ReferenceEquals(button.Command, EditorControl.ViewModel.TestScriptCommand))
                            button.IsEnabled = false;
                        break;
                }

                LockEditorControls(child);
            }
        }

        public void ShowSuccessMessage(string message) => Notifications.Success(message);

        public void ShowWarningMessage(string message) => Notifications.Warning(message);

        public void ShowErrorMessage(string message) => Notifications.Error(message);

        public Task<bool> ShowConfirmDialogAsync(string title, string message, string confirmText, string cancelText) =>
            Notifications.ShowDialogAsync(title, message, confirmText, cancelText);

        public ICommand? ConfirmCommand
        {
            get => (ICommand?)GetValue(ConfirmCommandProperty);
            set => SetValue(ConfirmCommandProperty, value);
        }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register(nameof(ConfirmCommand), typeof(ICommand), typeof(DiagramPlotEditorWindow));

        public ICommand? CancelCommand
        {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(DiagramPlotEditorWindow));

        protected override void OnClosed(EventArgs e)
        {
            _viewOnlyTimer?.Stop();
            Notifications.Dispose();
            EditorControl.ReleaseResources();
            base.OnClosed(e);
        }
    }
}
