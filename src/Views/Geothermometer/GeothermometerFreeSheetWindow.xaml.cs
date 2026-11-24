using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace GeoChemistryNexus.Views
{
    public partial class GeothermometerFreeSheetWindow : Window, IFreeSheetNotificationHost, INotificationCenterHost
    {
        private static GeothermometerFreeSheetWindow? _instance;
        private readonly GeothermometerFreeSheetViewModel _viewModel;

        /// <summary>
        /// 本窗口独立的通知中心（由 XAML 中的 NotificationHost 呈现）。
        /// </summary>
        public NotificationCenter Notifications { get; } = new();

        Window IFreeSheetNotificationHost.OwnerWindow => this;

        public GeothermometerFreeSheetWindow()
        {
            InitializeComponent();
            UiScaleHelper.Attach(this);
            GeoChemistryNexus.Helpers.WindowWorkAreaHelper.Attach(this);
            GeoChemistryNexus.Helpers.ChildWindowPlacementHelper.Attach(this);
            _viewModel = new GeothermometerFreeSheetViewModel();
            DataContext = _viewModel;

            SourceInitialized += (_, _) =>
            {
                int policy = 2; // Match the native DWM shadow of the template editor windows.
                DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 2, ref policy, sizeof(int));
            };
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();

        public static void ShowOrActivate()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new GeothermometerFreeSheetWindow
                {
                    Owner = Application.Current.MainWindow
                };
                WindowActivationHelper.AttachOwnerFocusPreservation(_instance, Application.Current.MainWindow);
                _instance.Closed += (_, _) => _instance = null;
                _instance.Show();
                return;
            }

            if (_instance.WindowState == WindowState.Minimized)
                _instance.WindowState = WindowState.Normal;

            _instance.Activate();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.AttachNotificationHost(this);
            _viewModel.AttachGrid(FreeReoGrid);

            var worksheet = FreeReoGrid.CurrentWorksheet;
            if (worksheet != null)
                worksheet.SelectionRangeChanged += OnSelectionRangeChanged;

            FreeReoGrid.CurrentWorksheetChanged += OnCurrentWorksheetChanged;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Notifications.Dispose();
            _viewModel.AttachNotificationHost(null);

            var worksheet = FreeReoGrid.CurrentWorksheet;
            if (worksheet != null)
                worksheet.SelectionRangeChanged -= OnSelectionRangeChanged;

            FreeReoGrid.CurrentWorksheetChanged -= OnCurrentWorksheetChanged;
        }

        private void OnCurrentWorksheetChanged(object? sender, EventArgs e)
        {
            var worksheet = FreeReoGrid.CurrentWorksheet;
            if (worksheet == null)
                return;

            worksheet.SelectionRangeChanged += OnSelectionRangeChanged;
            _viewModel.AttachWorksheetRowExpansionEvents(worksheet);
            _viewModel.OnSelectionChanged(worksheet);
        }

        private void OnSelectionRangeChanged(object? sender, unvell.ReoGrid.Events.RangeEventArgs e)
        {
            if (sender is unvell.ReoGrid.Worksheet worksheet)
                _viewModel.OnSelectionChanged(worksheet);
        }

        void IFreeSheetNotificationHost.ShowInfo(string message) => Notifications.Info(message);

        void IFreeSheetNotificationHost.ShowSuccess(string message) => Notifications.Success(message);

        void IFreeSheetNotificationHost.ShowWarning(string message) => Notifications.Warning(message);

        void IFreeSheetNotificationHost.ShowError(string message) => Notifications.Error(message);

        Task<bool> IFreeSheetNotificationHost.ShowConfirmAsync(string message, string cancelText, string confirmText) =>
            Notifications.ShowDialogAsync(
                LanguageService.Instance["Confirm"] ?? "Confirm",
                message,
                confirmText,
                cancelText);

        async Task<FreeSheetCsvExportMode?> IFreeSheetNotificationHost.ShowExportModeAsync()
        {
            string valuesOption = LanguageService.Instance["geo_free_sheet_export_values"];
            string formulasOption = LanguageService.Instance["geo_free_sheet_export_formulas"];

            string? selected = await Notifications.ShowChoiceAsync(
                LanguageService.Instance["geo_free_sheet_export_title"],
                LanguageService.Instance["geo_free_sheet_export_mode_prompt"],
                new[] { valuesOption, formulasOption },
                LanguageService.Instance["Confirm"] ?? "Confirm",
                LanguageService.Instance["Cancel"] ?? "Cancel",
                valuesOption);

            if (selected == null)
                return null;

            return string.Equals(selected, formulasOption, StringComparison.Ordinal)
                ? FreeSheetCsvExportMode.Formulas
                : FreeSheetCsvExportMode.Values;
        }
    }
}
