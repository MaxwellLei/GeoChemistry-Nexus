using GeoChemistryNexus.Controls;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GeoChemistryNexus.Views
{
    public partial class DiagramPlotEditorWindow : Window
    {
        public ObservableCollection<NotificationViewModel> LocalNotifications { get; } = new();

        public DiagramPlotEditorControl Editor => EditorControl;

        public DiagramPlotEditorWindow()
        {
            InitializeComponent();
            EditorControl.ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DiagramPlotEditorViewModel.WindowTitle))
                    Title = EditorControl.ViewModel.WindowTitle;
            };
            Title = EditorControl.ViewModel.WindowTitle;
        }

        public void InitializeForCreate()
        {
            EditorControl.InitializeForCreate();
        }

        public void InitializeForEdit(GraphMapTemplateEntity entity)
        {
            EditorControl.InitializeForEdit(entity);
        }

        public void ShowSuccessMessage(string message) =>
            ShowLocalNotification("Success", message, NotificationType.Success, MessageHelper.WaitTime);

        public void ShowWarningMessage(string message) =>
            ShowLocalNotification("Warning", message, NotificationType.Warning);

        public void ShowErrorMessage(string message) =>
            ShowLocalNotification("Error", message, NotificationType.Error);

        public Task<bool> ShowConfirmDialogAsync(string title, string message, string confirmText, string cancelText)
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, NotificationType.Info, RemoveNotification)
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

        private void ShowLocalNotification(string title, string message, NotificationType type, int durationSeconds = 0)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = new NotificationViewModel(title, message, type, RemoveNotification);
                LocalNotifications.Add(vm);
                if (durationSeconds > 0)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(durationSeconds)).ContinueWith(_ =>
                        Dispatcher.BeginInvoke(new Action(async () => await vm.Close())));
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
