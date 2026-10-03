using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Views.Widgets
{
    public partial class OxideElementConverterWidget : UserControl
    {
        public ICommand SelectEntryCommand { get; }

        public OxideElementConverterWidget()
        {
            InitializeComponent();

            SelectEntryCommand = new RelayCommand<OxideElementEntry>(entry =>
            {
                if (DataContext is OxideElementConverterViewModel vm && entry != null)
                    vm.SelectedEntry = entry;
            });
        }
    }
}
