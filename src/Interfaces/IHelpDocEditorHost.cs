using System;

namespace GeoChemistryNexus.Interfaces;

/// <summary>
/// 多语言帮助文档富文本编辑器（<c>EpmaHelpDocEditor</c>）需要的宿主能力。
/// EPMA 模板编辑器与温压计模板编辑器都实现它，从而共用同一个编辑器控件。
/// 控件 XAML 里绑定的 LanguageParts / SelectedHelpLanguage / CanShowSave / CanEdit / IsReadOnly 由宿主以同名属性提供。
/// </summary>
public interface IHelpDocEditorHost
{
    /// <summary>当前正在编辑帮助文档的语言代码。</summary>
    string? SelectedHelpLanguage { get; }

    /// <summary>编辑器通过这两个回调读写当前语言的 RTF。</summary>
    void AttachHelpEditor(Func<string?> read, Action<string?> write);

    /// <summary>编辑器内容变化时通知宿主（宿主可据此刷新状态或暂存草稿）。</summary>
    void UpdateHelpDocumentDraft(string? rtf);

    /// <summary>“管理语言”链接：回到语言设置所在的页面。</summary>
    void ShowLanguageSettings();
}
