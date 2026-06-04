using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeoChemistryNexus.Controls
{
    public partial class DiagramPlotEditorControl : UserControl
    {
        public DiagramPlotEditorViewModel ViewModel { get; }

        public static readonly DependencyProperty ConfirmCommandProperty =
            DependencyProperty.Register(nameof(ConfirmCommand), typeof(ICommand), typeof(DiagramPlotEditorControl));

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(DiagramPlotEditorControl));

        public ICommand? ConfirmCommand
        {
            get => (ICommand?)GetValue(ConfirmCommandProperty);
            set => SetValue(ConfirmCommandProperty, value);
        }

        public ICommand? CancelCommand
        {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public DiagramPlotEditorControl()
        {
            InitializeComponent();
            ViewModel = new DiagramPlotEditorViewModel();
            DataContext = ViewModel;

            InitializeLanguageCombo();
            RefreshCategorySuggestions();

            WeakReferenceMessenger.Default.Register<CategoryConfigUpdatedMessage>(this, (_, _) =>
            {
                Application.Current.Dispatcher.Invoke(ViewModel.ReloadCategories);
            });

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DiagramPlotEditorViewModel.SelectedSectionIndex))
                    UpdateSectionVisibility();
            };
        }

        public void InitializeForCreate() => ViewModel.InitializeForCreate();

        public void InitializeForEdit(Models.GraphMapTemplateEntity entity) => ViewModel.InitializeForEdit(entity);

        private void InitializeLanguageCombo()
        {
            LanguageCombo.ItemsSource = new[]
            {
                new LanguageOption { Name = "简体中文 (zh-CN)", Code = "zh-CN" },
                new LanguageOption { Name = "繁体中文 (zh-TW)", Code = "zh-TW" },
                new LanguageOption { Name = "English (en-US)", Code = "en-US" },
                new LanguageOption { Name = "日本語 (ja-JP)", Code = "ja-JP" },
                new LanguageOption { Name = "Русский (ru-RU)", Code = "ru-RU" },
                new LanguageOption { Name = "한국어 (ko-KR)", Code = "ko-KR" },
                new LanguageOption { Name = "Deutsch (de-DE)", Code = "de-DE" },
                new LanguageOption { Name = "Español (es-ES)", Code = "es-ES" }
            };
        }

        private void NavSection_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int index))
                ViewModel.SelectedSectionIndex = index;
        }

        private void UpdateSectionVisibility()
        {
            bool showTranslation = ViewModel.SelectedSectionIndex == 1;
            BasicPanel.Visibility = showTranslation ? Visibility.Collapsed : Visibility.Visible;
            TranslationPanel.Visibility = showTranslation ? Visibility.Visible : Visibility.Collapsed;

            if (showTranslation)
                NavTranslation.IsChecked = true;
            else
                NavBasic.IsChecked = true;
        }

        private void LanguageCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (ViewModel.TryAddLanguage(LanguageCombo.Text?.Trim() ?? string.Empty))
                LanguageCombo.Text = string.Empty;
            e.Handled = true;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedValue is string code && ViewModel.TryAddLanguage(code))
            {
                LanguageCombo.SelectedIndex = -1;
                LanguageCombo.Text = string.Empty;
            }
        }

        private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is LanguageTagModel item)
                ViewModel.RemoveLanguage(item);
        }

        private void CategoryCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (ViewModel.TryAddCategory(CategoryCombo.Text?.Trim() ?? string.Empty))
                CategoryCombo.Text = string.Empty;
            e.Handled = true;
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!CategoryCombo.IsDropDownOpen)
            {
                CategoryCombo.SelectedIndex = -1;
                return;
            }

            if (CategoryCombo.SelectedItem is CategoryDisplayItem item &&
                ViewModel.TryAddCategory(item.DisplayName, item.OriginalObject as Dictionary<string, string>))
            {
                CategoryCombo.SelectedIndex = -1;
                CategoryCombo.Text = string.Empty;
            }

            RefreshCategorySuggestions();
        }

        private void RemoveCategory_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CategoryPartModel item)
            {
                ViewModel.RemoveCategory(item);
                RefreshCategorySuggestions();
            }
        }

        private void RefreshCategorySuggestions()
        {
            var list = ViewModel.GetCategorySuggestions();
            if (list == null)
            {
                CategoryCombo.ItemsSource = null;
                return;
            }

            CategoryCombo.ItemsSource = list.Select(c => new CategoryDisplayItem
            {
                DisplayName = PlotCategoryHelper.GetName(c),
                OriginalObject = c
            }).ToList();
        }

        private void TranslationGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Context")
            {
                e.Column.IsReadOnly = true;
                e.Column.Header = LanguageService.Instance["translation_item_context"] ?? "Item";
                e.Column.Width = new DataGridLength(200);
                e.Column.MinWidth = 160;
            }
            else if (e.PropertyName == "ObjectRef")
            {
                e.Cancel = true;
            }
            else
            {
                string langCode = e.PropertyName;
                bool isDefault = string.Equals(langCode, ViewModel.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase);
                e.Column.Header = isDefault ? $"{langCode} *" : langCode;
                e.Column.Width = new DataGridLength(140);
                e.Column.MinWidth = 120;
            }
        }

        private void TranslationGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ViewModel.OnTranslationCellChanged()));
        }

        private void AddTranslationLanguage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SimpleInputDialog(
                LanguageService.Instance["enter_language_code_prompt"] ?? "Enter language code (e.g. fr-FR):",
                LanguageService.Instance["add_language_column"] ?? "Add Language",
                OwnerWindow());
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
                ViewModel.AddTranslationLanguageCommand.Execute(dialog.InputText.Trim());
        }

        private void SwapTranslationLanguage_Click(object sender, RoutedEventArgs e)
        {
            var langs = GetTranslationLanguages();
            if (langs.Count < 2)
            {
                HandyControl.Controls.MessageBox.Show(
                    LanguageService.Instance["at_least_two_languages_swap"] ?? "At least two languages required.",
                    LanguageService.Instance["tips"] ?? "Tips",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new LanguageSwapDialog(langs, OwnerWindow());
            if (dialog.ShowDialog() == true &&
                !string.IsNullOrEmpty(dialog.Lang1) &&
                !string.IsNullOrEmpty(dialog.Lang2) &&
                dialog.Lang1 != dialog.Lang2)
            {
                ViewModel.SwapTranslationLanguagesCommand.Execute(Tuple.Create(dialog.Lang1, dialog.Lang2));
            }
        }

        private void RemoveTranslationLanguage_Click(object sender, RoutedEventArgs e)
        {
            var langs = GetTranslationLanguages();
            if (langs.Count == 0) return;

            var selectDialog = new LanguageSelectDialog(
                langs,
                LanguageService.Instance["select_language_to_delete"] ?? "Select language to delete:",
                OwnerWindow());
            if (selectDialog.ShowDialog() != true || string.IsNullOrEmpty(selectDialog.SelectedLanguage))
                return;

            var confirm = HandyControl.Controls.MessageBox.Show(
                string.Format(
                    LanguageService.Instance["confirm_delete_language"] ?? "Delete language '{0}'?",
                    selectDialog.SelectedLanguage),
                LanguageService.Instance["tips"] ?? "Tips",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
                ViewModel.RemoveTranslationLanguageCommand.Execute(selectDialog.SelectedLanguage);
        }

        private List<string> GetTranslationLanguages()
        {
            if (ViewModel.TranslationTable == null) return new List<string>();
            return ViewModel.TranslationTable.Columns.Cast<System.Data.DataColumn>()
                .Where(c => c.ColumnName is not "Context" and not "ObjectRef")
                .Select(c => c.ColumnName)
                .ToList();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmCommand?.CanExecute(ViewModel) ?? ConfirmCommand?.CanExecute(null) ?? false)
                ConfirmCommand.Execute(ViewModel);
            else if (ConfirmCommand != null)
                ConfirmCommand.Execute(ViewModel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (CancelCommand?.CanExecute(ViewModel) ?? true)
                CancelCommand?.Execute(ViewModel);
        }

        private Window? OwnerWindow() => Window.GetWindow(this);

        private class LanguageOption
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }

        private class CategoryDisplayItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public object? OriginalObject { get; set; }
        }
    }
}
