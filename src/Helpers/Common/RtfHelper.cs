using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GeoChemistryNexus.Helpers
{
    public static class RtfHelper
    {
        /// <summary>
        /// 将RTF文件内容加载到RichTextBox控件中
        /// </summary>
        /// <param name="rtfFilePath">RTF文件路径</param>
        /// <param name="richTextBox">目标RichTextBox控件</param>
        /// <returns>操作是否成功</returns>
        public static bool LoadRtfToRichTextBox(string rtfFilePath, RichTextBox richTextBox)
        {
            try
            {
                // 检查参数
                if (string.IsNullOrEmpty(rtfFilePath))
                {
                    // 文件路径不能为空
                    throw new ArgumentException(LanguageService.Instance["file_path_cannot_be_empty"], nameof(rtfFilePath));
                }

                if (richTextBox == null)
                {
                    // RichTextBox控件不能为null
                    throw new ArgumentNullException(nameof(richTextBox), LanguageService.Instance["richtextbox_control_cannot_be_null"]);
                }

                // 检查文件是否存在
                if (!File.Exists(rtfFilePath))
                {
                    throw new FileNotFoundException(LanguageService.Instance["file_does_not_exist"] + $"{rtfFilePath}");
                }

                // 读取RTF文件内容
                string rtfContent = File.ReadAllText(rtfFilePath);

                // 使用TextRange来加载RTF内容
                TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

                using (MemoryStream rtfMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
                {
                    textRange.Load(rtfMemoryStream, DataFormats.Rtf);
                    ApplyScriptBaselines(richTextBox.Document);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["error_loading_rtf_file"] + $"{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步版本：将RTF文件内容加载到RichTextBox控件中
        /// </summary>
        /// <param name="rtfFilePath">RTF文件路径</param>
        /// <param name="richTextBox">目标RichTextBox控件</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> LoadRtfToRichTextBoxAsync(string rtfFilePath, RichTextBox richTextBox)
        {
            try
            {
                // 检查参数
                if (string.IsNullOrEmpty(rtfFilePath))
                {
                    throw new ArgumentException(LanguageService.Instance["file_path_cannot_be_empty"], nameof(rtfFilePath));
                }

                if (richTextBox == null)
                {
                    throw new ArgumentNullException(nameof(richTextBox), LanguageService.Instance["richtextbox_control_cannot_be_null"]);
                }

                // 检查文件是否存在
                if (!File.Exists(rtfFilePath))
                {
                    throw new FileNotFoundException(LanguageService.Instance["file_does_not_exist"] + $"{rtfFilePath}");
                }

                // 异步读取文件内容
                string rtfContent = await File.ReadAllTextAsync(rtfFilePath);

                // 在UI线程上更新RichTextBox
                if (!richTextBox.Dispatcher.CheckAccess())
                {
                    await richTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                        using (MemoryStream rtfMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
                        {
                            textRange.Load(rtfMemoryStream, DataFormats.Rtf);
                    ApplyScriptBaselines(richTextBox.Document);
                        }
                    });
                }
                else
                {
                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    using (MemoryStream rtfMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
                    {
                        textRange.Load(rtfMemoryStream, DataFormats.Rtf);
                    ApplyScriptBaselines(richTextBox.Document);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["error_loading_rtf_file"] + $"{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取RichTextBox的RTF字符串内容
        /// </summary>
        public static string GetRtfString(RichTextBox richTextBox)
        {
            if (richTextBox == null) return string.Empty;
            TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            using (MemoryStream ms = new MemoryStream())
            {
                textRange.Save(ms, DataFormats.Rtf);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 将RTF字符串加载到RichTextBox
        /// </summary>
        public static void LoadRtfString(RichTextBox richTextBox, string rtfContent)
        {
             if (richTextBox == null || string.IsNullOrEmpty(rtfContent)) return;
             TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
             using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
             {
                 textRange.Load(ms, DataFormats.Rtf);
                 ApplyScriptBaselines(richTextBox.Document);
             }
        }

        /// <summary>
        /// 将RichTextBox内容保存为RTF文件
        /// </summary>
        /// <param name="richTextBox">源RichTextBox控件</param>
        /// <param name="rtfFilePath">保存的RTF文件路径</param>
        /// <returns>操作是否成功</returns>
        public static bool SaveRichTextBoxToRtf(RichTextBox richTextBox, string rtfFilePath)
        {
            try
            {
                // 检查参数
                if (richTextBox == null)
                {
                    throw new ArgumentNullException(nameof(richTextBox), LanguageService.Instance["richtextbox_control_cannot_be_null"]);
                }

                if (string.IsNullOrEmpty(rtfFilePath))
                {
                    throw new ArgumentException(LanguageService.Instance["file_path_cannot_be_empty"], nameof(rtfFilePath));
                }

                // 获取RichTextBox的RTF内容
                TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);

                using (MemoryStream rtfMemoryStream = new MemoryStream())
                {
                    textRange.Save(rtfMemoryStream, DataFormats.Rtf);
                    string rtfContent = Encoding.UTF8.GetString(rtfMemoryStream.ToArray());
                    File.WriteAllText(rtfFilePath, rtfContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["error_saving_rtf_file"] + $"{ex.Message}");
                return false;
            }
        }

        // ===== 上标 / 下标 =====
        // RTF 只会保存 Typography.Variants（\super / \sub），不保存 Inline.BaselineAlignment；
        // 而多数字体不支持 OpenType 上下标，Variants 本身没有可见效果。
        // 因此：编辑时同时写入 Variants（用于持久化）、BaselineAlignment（用于显示）与缩小的字号；
        // 加载 RTF 后再根据 Variants 恢复 BaselineAlignment（字号已随 RTF 保存）。

        /// <summary>上下标文字相对正常字号的缩放比例（2/3 可使字号在 RTF 半磅取整后保持精确）。</summary>
        private const double ScriptFontScale = 2d / 3d;

        /// <summary>
        /// 加载 RTF 后，根据 Typography.Variants 为上下标文字恢复基线对齐（不改动字号）。
        /// </summary>
        public static void ApplyScriptBaselines(FlowDocument? document)
        {
            if (document == null) return;
            var pointer = document.ContentStart;
            while (pointer != null && pointer.CompareTo(document.ContentEnd) < 0)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart
                    && pointer.GetAdjacentElement(LogicalDirection.Forward) is Run run)
                {
                    var alignment = run.Typography.Variants switch
                    {
                        FontVariants.Superscript => BaselineAlignment.Superscript,
                        FontVariants.Subscript => BaselineAlignment.Subscript,
                        _ => BaselineAlignment.Baseline
                    };
                    if (run.BaselineAlignment != alignment) run.BaselineAlignment = alignment;
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        /// <summary>
        /// 切换选区的上标 / 下标。已是目标格式则还原，否则应用（上下标之间切换不重复缩放字号）。
        /// </summary>
        public static void ToggleScript(RichTextBox richTextBox, FontVariants target)
        {
            if (richTextBox == null || richTextBox.IsReadOnly) return;
            if (target != FontVariants.Superscript && target != FontVariants.Subscript) return;

            var selection = richTextBox.Selection;
            bool turnOff = selection.GetPropertyValue(Typography.VariantsProperty) is FontVariants current && current == target;

            // 先按文本段记录选区范围（TextRange 会随编辑自动跟踪位置），再逐段应用，保证混合字号时各段独立缩放
            var segments = new List<TextRange>();
            if (selection.IsEmpty)
            {
                segments.Add(selection);
            }
            else
            {
                var pointer = selection.Start;
                while (pointer != null && pointer.CompareTo(selection.End) < 0)
                {
                    var next = pointer.GetNextContextPosition(LogicalDirection.Forward);
                    if (next == null) break;
                    if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text && pointer.Parent is Run)
                    {
                        segments.Add(new TextRange(pointer, next.CompareTo(selection.End) < 0 ? next : selection.End));
                    }
                    pointer = next;
                }
            }

            richTextBox.BeginChange();
            try
            {
                foreach (var segment in segments)
                {
                    bool isNormal = segment.GetPropertyValue(Typography.VariantsProperty) is not FontVariants variant
                                    || (variant != FontVariants.Superscript && variant != FontVariants.Subscript);
                    double size = segment.GetPropertyValue(TextElement.FontSizeProperty) is double s ? s : richTextBox.FontSize;

                    if (turnOff)
                    {
                        segment.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Round(size / ScriptFontScale, 1));
                        segment.ApplyPropertyValue(Typography.VariantsProperty, FontVariants.Normal);
                        segment.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                    }
                    else
                    {
                        if (isNormal) segment.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Round(size * ScriptFontScale, 2));
                        segment.ApplyPropertyValue(Typography.VariantsProperty, target);
                        segment.ApplyPropertyValue(Inline.BaselineAlignmentProperty,
                            target == FontVariants.Superscript ? BaselineAlignment.Superscript : BaselineAlignment.Subscript);
                    }
                }
            }
            finally { richTextBox.EndChange(); }
        }
    }
}