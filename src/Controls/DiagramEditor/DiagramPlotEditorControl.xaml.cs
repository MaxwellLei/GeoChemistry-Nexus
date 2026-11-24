using CommunityToolkit.Mvvm.Messaging;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using GeoChemistryNexus.Messages;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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

            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DiagramPlotEditorViewModel.SelectedSectionIndex))
                    UpdateSectionVisibility();
            };

            // 帮助文档由 EpmaHelpDocEditor 通过 IHelpDocEditorHost.AttachHelpEditor 自行挂接。
        }

        private bool _released;

        public void ReleaseResources()
        {
            if (_released)
                return;

            _released = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
            ViewModel.Release();
        }

        public void InitializeForCreate(IReadOnlyList<CategoryPartModel>? initialFolder = null) =>
            ViewModel.InitializeForCreate(initialFolder);

        public void InitializeForEdit(Models.GraphMapTemplateEntity entity) => ViewModel.InitializeForEdit(entity);

        private void NavSection_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int index))
                ViewModel.SelectedSectionIndex = index;
        }

        private void UpdateSectionVisibility()
        {
            // 0 基本信息 / 1 多语言 / 2 数据与脚本 / 3 帮助文档
            int index = ViewModel.SelectedSectionIndex;
            BasicPanel.Visibility = index == DiagramPlotEditorViewModel.SectionBasic ? Visibility.Visible : Visibility.Collapsed;
            ScriptPanel.Visibility = index == DiagramPlotEditorViewModel.SectionScript ? Visibility.Visible : Visibility.Collapsed;
            HelpPanel.Visibility = index == DiagramPlotEditorViewModel.SectionHelp ? Visibility.Visible : Visibility.Collapsed;
            TranslationPanel.Visibility = index == DiagramPlotEditorViewModel.SectionTranslation ? Visibility.Visible : Visibility.Collapsed;

            switch (index)
            {
                case DiagramPlotEditorViewModel.SectionBasic:
                    NavBasic.IsChecked = true;
                    break;
                case DiagramPlotEditorViewModel.SectionScript:
                    NavScript.IsChecked = true;
                    break;
                case DiagramPlotEditorViewModel.SectionHelp:
                    NavHelp.IsChecked = true;
                    break;
                case DiagramPlotEditorViewModel.SectionTranslation:
                    NavTranslation.IsChecked = true;
                    break;
            }
        }

        private void RemoveLanguage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: LanguageTagModel item })
                ViewModel.RemoveLanguage(item);
        }

        private void SelectSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            var pickerViewModel = new FolderPickerDialogViewModel(new FolderPickerOptions
            {
                IsCustom = ViewModel.IsCustomTemplateScope,
                Title = LanguageService.GetString("template_save_location_choose_title", "Choose save location"),
                InitialPath = ViewModel.SaveLocationPath
            });
            var dialog = new FolderPickerWindow(pickerViewModel, Window.GetWindow(this));
            if (dialog.ShowDialog() == true && dialog.SelectedPath != null)
                ViewModel.SelectSaveLocation(dialog.SelectedPath);
        }

        public void Confirm()
        {
            ViewModel.GetHelpDocumentsForSubmit();
            ConfirmCommand?.Execute(ViewModel);
        }

        public void Cancel()
        {
            if (CancelCommand?.CanExecute(ViewModel) ?? true)
                CancelCommand?.Execute(ViewModel);
        }
    }
}
