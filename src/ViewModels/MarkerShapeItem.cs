using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using System.Windows.Media;
using GeoChemistryNexus.Services;

namespace GeoChemistryNexus.ViewModels
{
    public class MarkerShapeItem : ObservableObject
    {
        public MarkerShape Shape { get; }
        public Geometry Icon { get; }
        public bool IsFilled { get; }

        public string DisplayName => LanguageService.Instance[$"MarkerShape_{Shape}"];

        public MarkerShapeItem(MarkerShape shape, Geometry icon, bool isFilled)
        {
            Shape = shape;
            Icon = icon;
            IsFilled = isFilled;
            // 当语言改变时通知界面更新
            LanguageService.Instance.PropertyChanged += (s, e) => OnPropertyChanged(nameof(DisplayName));
        }
    }
}
