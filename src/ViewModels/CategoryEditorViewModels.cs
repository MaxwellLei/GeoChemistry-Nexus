using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.ComponentModel;

namespace GeoChemistryNexus.ViewModels
{
    public partial class LocalizedCategoryEntryViewModel : ObservableObject
    {
        private readonly ContentLanguageContext _languageContext;

        [ObservableProperty]
        private LocalizedString title = new();

        public string DisplayTitle => HomeLinksLocalization.ResolveForContext(Title, _languageContext);

        public LocalizedCategoryEntryViewModel(ContentLanguageContext languageContext)
        {
            _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
            _languageContext.PropertyChanged += OnLanguageContextChanged;
        }

        partial void OnTitleChanged(LocalizedString value) => OnPropertyChanged(nameof(DisplayTitle));

        private void OnLanguageContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContentLanguageContext.ContentLanguage))
                OnPropertyChanged(nameof(DisplayTitle));
        }
    }
}
