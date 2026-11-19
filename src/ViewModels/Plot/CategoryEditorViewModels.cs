using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace GeoChemistryNexus.ViewModels
{
    public partial class LocalizedCategoryEntryViewModel : ObservableObject
    {
        private readonly ContentLanguageContext _languageContext;

        [ObservableProperty]
        private LocalizedString title = new();

        public string DisplayTitle => HomeLinksLocalization.ResolveForContext(Title, _languageContext);

        public ObservableCollection<CategoryLanguageField> LanguageFields { get; }

        public LocalizedCategoryEntryViewModel(ContentLanguageContext languageContext)
        {
            _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
            _languageContext.PropertyChanged += OnLanguageContextChanged;
            LanguageFields = new ObservableCollection<CategoryLanguageField>(
                AppCultureRegistry.SupportedCodes.Select(code =>
                    new CategoryLanguageField(this, code, AppCultureRegistry.GetDisplayName(code, false))));
        }

        partial void OnTitleChanged(LocalizedString value)
        {
            OnPropertyChanged(nameof(DisplayTitle));
            RefreshLanguageFields();
        }

        public void ReplaceTitle(LocalizedString source)
        {
            Title = HomeLinksLocalization.Clone(source);
        }

        public string GetTranslation(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || Title?.Translations == null)
                return string.Empty;

            foreach (var pair in Title.Translations)
            {
                if (string.Equals(pair.Key, code, StringComparison.OrdinalIgnoreCase))
                    return pair.Value ?? string.Empty;
            }

            return string.Empty;
        }

        public void SetTranslation(string code, string text)
        {
            Title ??= new LocalizedString();
            Title.Translations ??= new System.Collections.Generic.Dictionary<string, string>();
            var stale = Title.Translations.Keys
                .Where(key => string.Equals(key, code, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(key, code, StringComparison.Ordinal))
                .ToList();
            foreach (var key in stale)
                Title.Translations.Remove(key);

            Title.Set(text ?? string.Empty, languageCode: code);
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(DisplayTitle));
        }

        public void RefreshLanguageFields()
        {
            foreach (var field in LanguageFields)
                field.Refresh();
        }

        private void OnLanguageContextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContentLanguageContext.ContentLanguage))
                OnPropertyChanged(nameof(DisplayTitle));
        }
    }

    public partial class CategoryLanguageField : ObservableObject
    {
        private readonly LocalizedCategoryEntryViewModel _owner;

        public CategoryLanguageField(LocalizedCategoryEntryViewModel owner, string code, string displayName)
        {
            _owner = owner;
            Code = code;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? code : displayName;
        }

        public string Code { get; }

        public string DisplayName { get; }

        public string Text
        {
            get => _owner.GetTranslation(Code);
            set
            {
                string next = value ?? string.Empty;
                if (next == _owner.GetTranslation(Code))
                    return;

                _owner.SetTranslation(Code, next);
            }
        }

        public void Refresh() => OnPropertyChanged(nameof(Text));
    }
}
