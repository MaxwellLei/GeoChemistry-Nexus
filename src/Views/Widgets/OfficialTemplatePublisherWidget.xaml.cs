using GeoChemistryNexus.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class OfficialTemplatePublisherWidget : UserControl
    {
        private OfficialTemplatePublisherViewModel? _viewModel;

        public OfficialTemplatePublisherWidget()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = e.NewValue as OfficialTemplatePublisherViewModel;
            if (_viewModel != null)
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OfficialTemplatePublisherViewModel.SecretKey)
                && _viewModel != null
                && string.IsNullOrEmpty(_viewModel.SecretKey))
            {
                SecretKeyBox.Password = string.Empty;
            }
        }

        private void SecretKeyBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is OfficialTemplatePublisherViewModel vm && sender is PasswordBox box)
                vm.SecretKey = box.Password;
        }
    }
}
