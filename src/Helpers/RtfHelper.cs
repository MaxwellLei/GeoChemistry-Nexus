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
                        }
                    });
                }
                else
                {
                    TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    using (MemoryStream rtfMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(rtfContent)))
                    {
                        textRange.Load(rtfMemoryStream, DataFormats.Rtf);
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
    }
}