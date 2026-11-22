using GeoChemistryNexus.Models;
using GeoChemistryNexus.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 右侧检查器「计算详情」的导出 CSV / 复制到剪贴板。
    /// 与温压计检查器的同款按钮共用同一套提示文字：导出为 UTF-8（带 BOM）CSV，复制为制表符分隔文本，可直接粘贴到 Excel。
    /// 各模块只需把当前显示的（名称，值）行交给这里。
    /// </summary>
    public static class InspectorDetailsExporter
    {
        private const string DefaultFileName = "calculation_details";

        /// <summary>弹出保存对话框并把详情写成 CSV。</summary>
        public static async Task ExportCsvAsync(IReadOnlyCollection<(string Name, string Value)> rows, string? suggestedName)
        {
            if (rows.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("Name,Value");
            foreach (var (name, value) in rows)
                sb.Append(EscapeCsvField(name)).Append(',').Append(EscapeCsvField(value)).AppendLine();

            await SaveCsvAsync(sb.ToString(), suggestedName);
        }

        /// <summary>把详情复制到剪贴板（制表符分隔）。</summary>
        public static void CopyToClipboard(IReadOnlyCollection<(string Name, string Value)> rows)
        {
            if (rows.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("Name\tValue");
            foreach (var (name, value) in rows)
                sb.Append(FlattenForClipboard(name)).Append('\t').Append(FlattenForClipboard(value)).AppendLine();

            CopyText(sb.ToString());
        }

        /// <summary>弹出保存对话框并把计算步骤（名称 / 值 / 说明）写成 CSV；分组标题行同样写入。</summary>
        public static async Task ExportStepsCsvAsync(IReadOnlyCollection<CalculationStep> steps, string? suggestedName)
        {
            if (steps.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("Name,Value,Description");
            foreach (var step in steps)
                sb.Append(EscapeCsvField(step.Name)).Append(',')
                  .Append(EscapeCsvField(step.Value)).Append(',')
                  .Append(EscapeCsvField(step.Description))
                  .AppendLine();

            await SaveCsvAsync(sb.ToString(), suggestedName);
        }

        /// <summary>把计算步骤（名称 / 值 / 说明）复制到剪贴板（制表符分隔）。</summary>
        public static void CopyStepsToClipboard(IReadOnlyCollection<CalculationStep> steps)
        {
            if (steps.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("Name\tValue\tDescription");
            foreach (var step in steps)
                sb.Append(FlattenForClipboard(step.Name)).Append('\t')
                  .Append(FlattenForClipboard(step.Value)).Append('\t')
                  .Append(FlattenForClipboard(step.Description))
                  .AppendLine();

            CopyText(sb.ToString());
        }

        private static async Task SaveCsvAsync(string content, string? suggestedName)
        {
            string? filePath = await FileHelper.GetSaveFilePath2Async(
                title: LanguageService.Instance["geo_msg_csv_save_title"],
                filter: LanguageService.Instance["csv_file_filter"],
                defaultExt: ".csv",
                defaultFileName: SanitizeFileName(suggestedName));
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                await Task.Run(() => File.WriteAllText(filePath, content, new UTF8Encoding(true)));
                MessageHelper.ExportSuccess(LanguageService.Instance["geo_msg_export_success"], filePath);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(string.Format(LanguageService.Instance["geo_msg_export_failed"], ex.Message));
            }
        }

        private static void CopyText(string text)
        {
            try
            {
                Clipboard.SetText(text.TrimEnd('\r', '\n'));
                MessageHelper.Success(LanguageService.Instance["geo_msg_calc_details_copied"]);
            }
            catch (Exception ex)
            {
                MessageHelper.Error(LanguageService.Instance["copy_failed"] + ex.Message);
            }
        }

        // 单元格内的换行和制表符会打乱粘贴后的行列，统一替换成空格。
        private static string FlattenForClipboard(string? value) =>
            (value ?? string.Empty).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');

        private static string EscapeCsvField(string? value)
        {
            value ??= string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static string SanitizeFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return DefaultFileName;
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? DefaultFileName : name.Trim();
        }
    }
}
