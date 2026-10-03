using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels.Home;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class AddWidgetWindow : HandyControl.Controls.Window, INotifyPropertyChanged
    {
        private readonly Action<HomeAppItem>? _onAddWidget;
        private readonly Action<HomeAppItem>? _onRemoveWidget;
        private string _searchText = string.Empty;

        public HomeAppItem? SelectedWidget { get; private set; }

        public ObservableCollection<AddWidgetCardItem> Cards { get; } = new();
        public ICollectionView FilteredCards { get; }

        public ICommand CloseCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    FilteredCards.Refresh();
                    OnPropertyChanged(nameof(SearchText));
                    OnPropertyChanged(nameof(HasMatches));
                }
            }
        }

        public bool HasMatches => !FilteredCards.IsEmpty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddWidgetWindow(List<HomeAppItem> availableWidgets)
            : this(availableWidgets, null, null, null)
        {
        }

        public AddWidgetWindow(
            IEnumerable<HomeAppItem> availableWidgets,
            IEnumerable<HomeAppItem>? currentWidgets = null,
            Action<HomeAppItem>? onAddWidget = null,
            Action<HomeAppItem>? onRemoveWidget = null)
        {
            _onAddWidget = onAddWidget;
            _onRemoveWidget = onRemoveWidget;
            CloseCommand = new RelayCommand(Close);

            InitializeComponent();
            UiScaleHelper.Attach(this);

            var addedKeys = new HashSet<string>(
                currentWidgets?.Select(w => w.WidgetKey).Where(k => !string.IsNullOrEmpty(k)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var widget in availableWidgets)
            {
                bool isAdded = addedKeys.Contains(widget.WidgetKey);
                var card = new AddWidgetCardItem(widget, isAdded, OnCardAddClicked, OnCardRemoveClicked);
                Cards.Add(card);
            }

            FilteredCards = CollectionViewSource.GetDefaultView(Cards);
            FilteredCards.Filter = FilterCard;

            DataContext = this;
        }

        private bool FilterCard(object obj)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            if (obj is AddWidgetCardItem card)
            {
                return (card.Title?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true)
                    || (card.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            return true;
        }

        private void OnCardAddClicked(AddWidgetCardItem card)
        {
            SelectedWidget = card.Item;
            _onAddWidget?.Invoke(card.Item);
        }

        private void OnCardRemoveClicked(AddWidgetCardItem card)
        {
            _onRemoveWidget?.Invoke(card.Item);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
            SearchBox.Text = string.Empty;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
