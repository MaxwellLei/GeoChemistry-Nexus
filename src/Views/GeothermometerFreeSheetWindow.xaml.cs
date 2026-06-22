using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Views
{
    public partial class GeothermometerFreeSheetWindow : Window, IFreeSheetNotificationHost
    {
        private static GeothermometerFreeSheetWindow? _instance;
        private readonly GeothermometerFreeSheetViewModel _viewModel;

        public ObservableCollection<NotificationViewModel> LocalNotifications { get; } = new();

        Window IFreeSheetNotificationHost.OwnerWindow => this;

        public GeothermometerFreeSheetWindow()
        {
            InitializeComponent();
            _viewModel = new GeothermometerFreeSheetViewModel();
            DataContext = _viewModel;

            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        public static void ShowOrActivate()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new GeothermometerFreeSheetWindow
                {
                    Owner = Application.Current.MainWindow
                };
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

        void IFreeSheetNotificationHost.ShowInfo(string message) =>
            ShowLocalNotification(LanguageService.Instance["information"] ?? "Information", message, NotificationType.Info, MessageHelper.waitTime);

        void IFreeSheetNotificationHost.ShowSuccess(string message) =>
            ShowLocalNotification(LanguageService.Instance["notification_success"] ?? "Success", message, NotificationType.Success, MessageHelper.waitTime);

        void IFreeSheetNotificationHost.ShowWarning(string message) =>
            ShowLocalNotification(LanguageService.Instance["notification_warning"] ?? "Warning", message, NotificationType.Warning);

        void IFreeSheetNotificationHost.ShowError(string message) =>
            ShowLocalNotification(LanguageService.Instance["error"] ?? "Error", message, NotificationType.Error);

        Task<bool> IFreeSheetNotificationHost.ShowConfirmAsync(string message, string cancelText, string confirmText)
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(
                    LanguageService.Instance["Confirm"] ?? "Confirm",
                    message,
                    NotificationType.Info,
                    RemoveNotification)
                {
                    IsInteractive = true,
                    ConfirmText = confirmText,
                    CancelText = cancelText,
                    DialogResultAction = result => tcs.TrySetResult(result)
                };
                LocalNotifications.Add(vm);
            });
            return tcs.Task;
        }

        Task<FreeSheetCsvExportMode?> IFreeSheetNotificationHost.ShowExportModeAsync()
        {
            string valuesOption = LanguageService.Instance["geo_free_sheet_export_values"];
            string formulasOption = LanguageService.Instance["geo_free_sheet_export_formulas"];
            var tcs = new TaskCompletionSource<FreeSheetCsvExportMode?>();

            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(
                    LanguageService.Instance["geo_free_sheet_export_title"],
                    LanguageService.Instance["geo_free_sheet_export_mode_prompt"],
                    NotificationType.Info,
                    RemoveNotification)
                {
                    IsInteractive = true,
                    IsExportModeSelection = true,
                    ExportOptions = new ObservableCollection<string> { valuesOption, formulasOption },
                    SelectedExportOption = valuesOption,
                    ExportModeSelectionAction = selected =>
                    {
                        tcs.TrySetResult(string.Equals(selected, formulasOption, StringComparison.Ordinal)
                            ? FreeSheetCsvExportMode.Formulas
                            : FreeSheetCsvExportMode.Values);
                    },
                    DialogResultAction = confirmed =>
                    {
                        if (!confirmed)
                            tcs.TrySetResult(null);
                    }
                };
                LocalNotifications.Add(vm);
            });

            return tcs.Task;
        }

        private void ShowLocalNotification(string title, string message, NotificationType type, int durationSeconds = 0)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, type, RemoveNotification);
                LocalNotifications.Add(vm);

                if (durationSeconds > 0)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                    {
                        Dispatcher.BeginInvoke(new Action(async () => await vm.Close()));
                    });
                }
            });
        }

        private void RemoveNotification(NotificationViewModel vm)
        {
            Dispatcher.Invoke(() =>
            {
                if (LocalNotifications.Contains(vm))
                    LocalNotifications.Remove(vm);
            });
        }
    }
}
