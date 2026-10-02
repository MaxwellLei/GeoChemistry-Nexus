using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GeoChemistryNexus.ViewModels
{
    public partial class HomeLinkEntryEditorViewModel : ObservableObject
    {
        private readonly ContentLanguageContext _languageContext;

        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private LocalizedString title = new();

        [ObservableProperty]
        private LocalizedString description = new();

        [ObservableProperty]
        private string url = string.Empty;

        [ObservableProperty]
        private string icon = "\uE774";

        public string DisplayTitle => HomeLinksLocalization.ResolveForContext(Title, _languageContext);

        public string DisplayDescription => HomeLinksLocalization.ResolveForContext(Description, _languageContext);

        public HomeLinkEntryEditorViewModel(ContentLanguageContext languageContext)
        {
            _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
            _languageContext.PropertyChanged += OnLanguageContextChanged;
        }

        partial void OnTitleChanged(LocalizedString value) => NotifyDisplayPropertiesChanged();

        partial void OnDescriptionChanged(LocalizedString value) => NotifyDisplayPropertiesChanged();

        private void OnLanguageContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContentLanguageContext.ContentLanguage))
                NotifyDisplayPropertiesChanged();
        }

        private void NotifyDisplayPropertiesChanged()
        {
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(DisplayDescription));
        }
    }

    public partial class HomeLinkGroupEditorViewModel : ObservableObject
    {
        private readonly ContentLanguageContext _languageContext;

        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private LocalizedString title = new();

        [ObservableProperty]
        private int sortOrder;

        public string DisplayTitle => HomeLinksLocalization.ResolveForContext(Title, _languageContext);

        public ObservableCollection<HomeLinkEntryEditorViewModel> Links { get; }

        public HomeLinkGroupEditorViewModel(ContentLanguageContext languageContext)
        {
            _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
            _languageContext.PropertyChanged += OnLanguageContextChanged;
            Links = new ObservableCollection<HomeLinkEntryEditorViewModel>();
        }

        partial void OnTitleChanged(LocalizedString value) => OnPropertyChanged(nameof(DisplayTitle));

        private void OnLanguageContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContentLanguageContext.ContentLanguage))
                OnPropertyChanged(nameof(DisplayTitle));
        }
    }
}
