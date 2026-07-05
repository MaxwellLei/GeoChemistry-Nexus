using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GeoChemistryNexus.ViewModels
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public partial class NotificationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty]
        private NotificationType type;

        [ObservableProperty]
        private Brush backgroundColor = Brushes.Transparent;

        [ObservableProperty]
        private Brush foregroundColor = Brushes.Black;
        
        [ObservableProperty]
        private Brush borderColor = Brushes.Gray;

        // Dialog support
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
        private bool isInteractive;

        [ObservableProperty]
        private bool isCopyVisible;

        [ObservableProperty]
        private string confirmText = string.Empty;

        [ObservableProperty]
        private string cancelText = string.Empty;

        [ObservableProperty]
        private string thirdButtonText = string.Empty;

        [ObservableProperty]
        private bool isThreeButtonDialog;

        [ObservableProperty]
        private bool isThirdButtonSubtle;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CloseCommand))]
        private bool isClosing;

        public Action<bool>? DialogResultAction { get; set; }

        public Action<int>? ThreeButtonDialogResultAction { get; set; }

        [ObservableProperty]
        private bool _isLanguageSelection;

        [ObservableProperty]
        private ObservableCollection<string> _languages = new();

        [ObservableProperty]
        private string _selectedLanguage = string.Empty;

        public Action<string>? LanguageSelectionAction { get; set; }

        [ObservableProperty]
        private bool _isExportModeSelection;

        [ObservableProperty]
        private ObservableCollection<string> _exportOptions = new();

        [ObservableProperty]
        private string _selectedExportOption = string.Empty;

        public Action<string>? ExportModeSelectionAction { get; set; }

        private readonly Action<NotificationViewModel> _closeAction;

        public NotificationViewModel(string title, string message, NotificationType type, Action<NotificationViewModel> closeAction)
        {
            Title = title;
            Message = message;
            Type = type;
            _closeAction = closeAction;

            SetColors(type);
        }

        [RelayCommand]
        private async Task SelectLanguage()
        {
            if (!string.IsNullOrEmpty(SelectedLanguage))
            {
                LanguageSelectionAction?.Invoke(SelectedLanguage);
                await Close();
            }
        }

        [RelayCommand]
        private async Task SelectExportMode()
        {
            if (!string.IsNullOrEmpty(SelectedExportOption))
            {
                ExportModeSelectionAction?.Invoke(SelectedExportOption);
                await Close();
            }
        }

        [RelayCommand]
        private async Task Confirm()
        {
            if (IsThreeButtonDialog)
            {
                ThreeButtonDialogResultAction?.Invoke(0); // 0 = Confirm/Save
            }
            else
            {
                DialogResultAction?.Invoke(true);
            }
            await Close();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            if (IsThreeButtonDialog)
            {
                ThreeButtonDialogResultAction?.Invoke(2); // 2 = Cancel
            }
            else
            {
                DialogResultAction?.Invoke(false);
            }
            await Close();
        }

        [RelayCommand]
        private async Task ThirdButton()
        {
            ThreeButtonDialogResultAction?.Invoke(1); // 1 = Third Button (Don't Save)
            await Close();
        }

        [RelayCommand]
        private void Copy()
        {
            if (!string.IsNullOrEmpty(Message))
            {
                try
                {
                    Clipboard.SetText(Message);
                }
                catch (Exception)
                {
                    // Ignore clipboard errors
                }
            }
        }

        private void SetColors(NotificationType type)
        {
            IsCopyVisible = type == NotificationType.Warning || type == NotificationType.Error;

            // Modern-like colors
            
            switch (type)
            {
                case NotificationType.Success:
                    // Greenish
                    BackgroundColor = new SolidColorBrush(Color.FromRgb(240, 253, 244)) { Opacity = 0.95 }; 
                    BorderColor = new SolidColorBrush(Color.FromRgb(187, 247, 208));
                    ForegroundColor = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                    break;
                case NotificationType.Warning:
                    // Yellowish
                    BackgroundColor = new SolidColorBrush(Color.FromRgb(255, 251, 235)) { Opacity = 0.95 };
                    BorderColor = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                    ForegroundColor = new SolidColorBrush(Color.FromRgb(146, 64, 14));
                    break;
                case NotificationType.Error:
                    // Reddish
                    BackgroundColor = new SolidColorBrush(Color.FromRgb(254, 242, 242)) { Opacity = 0.95 };
                    BorderColor = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                    ForegroundColor = new SolidColorBrush(Color.FromRgb(153, 27, 27));
                    break;
                case NotificationType.Info:
                default:
                    // Blueish/Neutral
                    BackgroundColor = new SolidColorBrush(Color.FromRgb(240, 249, 255)) { Opacity = 0.95 };
                    BorderColor = new SolidColorBrush(Color.FromRgb(186, 230, 253));
                    ForegroundColor = new SolidColorBrush(Color.FromRgb(7, 89, 133));
                    break;
            }
        }

        private bool CanDismiss() => !IsInteractive && !IsClosing;

        [RelayCommand(CanExecute = nameof(CanDismiss))]
        public async Task Close()
        {
            if (IsClosing) return;
            IsClosing = true;
            await Task.Delay(300);
            _closeAction?.Invoke(this);
        }
    }
}

