using System.Windows;
using System.Windows.Controls;
using GeoChemistryNexus.Interfaces;
using GeoChemistryNexus.ViewModels;

namespace GeoChemistryNexus.Views;

/// <summary>
/// 多语言帮助文档编辑器：语言页签 + 通用富文本编辑器（<see cref="GeoChemistryNexus.Controls.RichTextEditor"/>）。
/// 富文本相关逻辑全部在 RichTextEditor 中，这里只负责与宿主（IHelpDocEditorHost）对接。
/// </summary>
public partial class EpmaHelpDocEditor : UserControl
{
    public EpmaHelpDocEditor()
    {
        InitializeComponent();
        // EPMA、温压计模板编辑器与图解编辑器共用此控件，宿主通过 IHelpDocEditorHost 提供能力。
        DataContextChanged += (_, e) => (e.NewValue as IHelpDocEditorHost)?.AttachHelpEditor(DocumentEditor.ReadRtf, WriteRtf);
    }

    private void WriteRtf(string? rtf)
        => DocumentEditor.WriteRtf(rtf, (DataContext as IHelpDocEditorHost)?.SelectedHelpLanguage);

    private void DocumentContentEdited(object? sender, string rtf)
        => (DataContext as IHelpDocEditorHost)?.UpdateHelpDocumentDraft(rtf);

    private void ManageLanguagesClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IHelpDocEditorHost editor) return;
        editor.ShowLanguageSettings();
        (Window.GetWindow(this)?.FindName("AddLanguageButton") as GeoChemistryNexus.Controls.LanguagePicker)?.FocusTrigger();
    }
}
