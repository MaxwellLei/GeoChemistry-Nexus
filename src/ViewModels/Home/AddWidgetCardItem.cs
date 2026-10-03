using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Models;

namespace GeoChemistryNexus.ViewModels.Home
{
    public partial class AddWidgetCardItem : ObservableObject
    {
        public HomeAppItem Item { get; }

        public string Title => Item.Title;
        public string Description => Item.Description;
        public string Icon => Item.Icon;
        public string WidgetKey => Item.WidgetKey;

        [ObservableProperty]
        private bool isAdded;

        public IRelayCommand ToggleAddCommand { get; }

        public AddWidgetCardItem(
            HomeAppItem item,
            bool isAlreadyAdded,
            Action<AddWidgetCardItem> onAdd,
            Action<AddWidgetCardItem> onRemove)
        {
            Item = item;
            isAdded = isAlreadyAdded;

            ToggleAddCommand = new RelayCommand(() =>
            {
                if (IsAdded)
                {
                    onRemove(this);
                    IsAdded = false;
                }
                else
                {
                    onAdd(this);
                    IsAdded = true;
                }
            });
        }
    }
}
