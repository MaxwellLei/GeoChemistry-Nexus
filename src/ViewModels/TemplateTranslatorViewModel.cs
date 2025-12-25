using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace GeoChemistryNexus.ViewModels
{
    public partial class TemplateTranslatorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _currentFilePath;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _isFileLoaded;

        [ObservableProperty]
        private DataTable _translationTable;

        [ObservableProperty]
        private GraphMapTemplate _currentTemplate;

        [ObservableProperty]
        private List<LocalizedString> _localizedStrings;

        private readonly JsonSerializerOptions _jsonOptions;

        public TemplateTranslatorViewModel()
        {
            TranslationTable = new DataTable();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        [RelayCommand]
        private void CloseFile()
        {
            CurrentTemplate = null;
            TranslationTable = new DataTable();
            IsFileLoaded = false;
            CurrentFilePath = string.Empty;
            StatusMessage = "文件已关闭";
        }

        [RelayCommand]
        private void AddLanguage(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode) || TranslationTable == null) return;
            if (TranslationTable.Columns.Contains(langCode))
            {
                StatusMessage = $"语言 {langCode} 已存在";
                return;
            }

            // DataTable 添加列
            TranslationTable.Columns.Add(langCode, typeof(string));
            
            // 触发 DataGrid 刷新
            var temp = TranslationTable;
            TranslationTable = null;
            TranslationTable = temp;

            StatusMessage = $"已添加语言: {langCode}";
        }

        [RelayCommand]
        private void RemoveLanguage(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode) || TranslationTable == null) return;
            if (TranslationTable.Columns.Contains(langCode))
            {
                TranslationTable.Columns.Remove(langCode);
                
                // 触发 DataGrid 刷新
                var temp = TranslationTable;
                TranslationTable = null;
                TranslationTable = temp;

                StatusMessage = $"已删除语言: {langCode}";
            }
        }

        [RelayCommand]
        private void SwapLanguages(Tuple<string, string> args) // Tuple<lang1, lang2>
        {
            if (args == null || TranslationTable == null) return;
            string lang1 = args.Item1;
            string lang2 = args.Item2;

            if (TranslationTable.Columns.Contains(lang1) && TranslationTable.Columns.Contains(lang2))
            {
                // 交换两列的所有数据
                foreach (DataRow row in TranslationTable.Rows)
                {
                    var temp = row[lang1];
                    row[lang1] = row[lang2];
                    row[lang2] = temp;
                }

                StatusMessage = $"已交换内容: {lang1} <-> {lang2}";
            }
        }

        [RelayCommand]
        private void ImportTemplate(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                StatusMessage = "文件不存在或路径无效";
                return;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                
                _currentTemplate = JsonSerializer.Deserialize<GraphMapTemplate>(jsonContent, _jsonOptions);

                if (_currentTemplate == null)
                {
                    StatusMessage = "无法解析模板文件";
                    return;
                }

                // 版本校验
                if (!GraphMapTemplateService.IsVersionCompatible(_currentTemplate))
                {
                    StatusMessage = LanguageService.Instance["template_version_too_high"];
                    return;
                }

                CurrentFilePath = filePath;
                LoadDataToTable();
                IsFileLoaded = true;
                StatusMessage = $"已加载: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
            }
        }

        private void LoadDataToTable()
        {
            _localizedStrings = new List<LocalizedString>();
            var dt = new DataTable();

            // 定义列
            dt.Columns.Add("Context", typeof(string)); // 上下文描述，例如 "NodeList", "Text #1"
            dt.Columns.Add("ObjectRef", typeof(object)); // 隐藏列，存储 LocalizedString 对象引用

            // 收集所有的语言 Key
            var allLanguages = new HashSet<string>();
            
            // 1. NodeList
            if (_currentTemplate.NodeList != null)
            {
                _localizedStrings.Add(_currentTemplate.NodeList);
                if (_currentTemplate.NodeList.Translations != null)
                {
                    foreach (var key in _currentTemplate.NodeList.Translations.Keys)
                    {
                        allLanguages.Add(key);
                    }
                }
            }

            // 2. Info.Texts
            if (_currentTemplate.Info?.Texts != null)
            {
                for (int i = 0; i < _currentTemplate.Info.Texts.Count; i++)
                {
                    var textDef = _currentTemplate.Info.Texts[i];
                    if (textDef.Content != null)
                    {
                        _localizedStrings.Add(textDef.Content);
                        if (textDef.Content.Translations != null)
                        {
                            foreach (var key in textDef.Content.Translations.Keys)
                            {
                                allLanguages.Add(key);
                            }
                        }
                    }
                }
            }

            // 3. Info.Title
             if (_currentTemplate.Info?.Title?.Label != null)
            {
                _localizedStrings.Add(_currentTemplate.Info.Title.Label);
                if (_currentTemplate.Info.Title.Label.Translations != null)
                {
                    foreach (var key in _currentTemplate.Info.Title.Label.Translations.Keys)
                    {
                        allLanguages.Add(key);
                    }
                }
            }

             // 4. Info.Axes
             if (_currentTemplate.Info?.Axes != null)
            {
                for (int i = 0; i < _currentTemplate.Info.Axes.Count; i++)
                {
                    var axis = _currentTemplate.Info.Axes[i];
                    if (axis.Label != null) // BaseAxisDefinition uses 'Label' not 'Title.Label'
                    {
                        _localizedStrings.Add(axis.Label);
                        if (axis.Label.Translations != null)
                        {
                            foreach (var key in axis.Label.Translations.Keys)
                            {
                                allLanguages.Add(key);
                            }
                        }
                    }
                }
            }


            // 确保默认语言也在列中
            if (!string.IsNullOrEmpty(_currentTemplate.DefaultLanguage))
            {
                allLanguages.Add(_currentTemplate.DefaultLanguage);
            }

            // 添加语言列
            foreach (var lang in allLanguages.OrderBy(x => x))
            {
                dt.Columns.Add(lang, typeof(string));
            }

            // 填充行
            // NodeList
             if (_currentTemplate.NodeList != null)
            {
                AddRow(dt, "NodeList (Category)", _currentTemplate.NodeList, allLanguages);
            }

             // Title
             if (_currentTemplate.Info?.Title?.Label != null)
             {
                 AddRow(dt, "Main Title", _currentTemplate.Info.Title.Label, allLanguages);
             }

            // Texts
            if (_currentTemplate.Info?.Texts != null)
            {
                for (int i = 0; i < _currentTemplate.Info.Texts.Count; i++)
                {
                    var textDef = _currentTemplate.Info.Texts[i];
                    if (textDef.Content != null)
                    {
                        // 尝试用默认语言的内容作为标识，如果没有则用索引
                        string defaultText = textDef.Content.Default != null && textDef.Content.Translations.ContainsKey(textDef.Content.Default) 
                            ? textDef.Content.Translations[textDef.Content.Default] 
                            : $"Text #{i + 1}";
                        
                        // 截断过长的文本
                        if (defaultText.Length > 20) defaultText = defaultText.Substring(0, 20) + "...";

                        AddRow(dt, $"Text #{i + 1} ({defaultText})", textDef.Content, allLanguages);
                    }
                }
            }
            
            // Axes
             if (_currentTemplate.Info?.Axes != null)
            {
                for (int i = 0; i < _currentTemplate.Info.Axes.Count; i++)
                {
                    var axis = _currentTemplate.Info.Axes[i];
                    if (axis.Label != null)
                    {
                         AddRow(dt, $"Axis #{i + 1} Title", axis.Label, allLanguages);
                    }
                }
            }

            TranslationTable = dt;
        }

        private void AddRow(DataTable dt, string context, LocalizedString locString, IEnumerable<string> languages)
        {
            var row = dt.NewRow();
            row["Context"] = context;
            row["ObjectRef"] = locString;

            foreach (var lang in languages)
            {
                if (locString.Translations != null && locString.Translations.ContainsKey(lang))
                {
                    row[lang] = locString.Translations[lang];
                }
                else
                {
                    row[lang] = string.Empty; // 或者 null
                }
            }
            dt.Rows.Add(row);
        }

        [RelayCommand]
        private void SaveTemplate()
        {
            if (_currentTemplate == null || TranslationTable == null)
            {
                StatusMessage = "没有可保存的数据";
                return;
            }

            try
            {
                // 从 DataTable 回写数据到 LocalizedString 对象
                // 获取当前表格中的所有语言列
                var currentLanguages = new HashSet<string>();
                foreach (DataColumn col in TranslationTable.Columns)
                {
                    if (col.ColumnName != "Context" && col.ColumnName != "ObjectRef")
                    {
                        currentLanguages.Add(col.ColumnName);
                    }
                }

                foreach (DataRow row in TranslationTable.Rows)
                {
                    var locString = row["ObjectRef"] as LocalizedString;
                    if (locString == null) continue;

                    if (locString.Translations == null)
                    {
                        locString.Translations = new Dictionary<string, string>();
                    }

                    // 1. 移除已经不在表格中的语言
                    var keysToRemove = locString.Translations.Keys.Where(k => !currentLanguages.Contains(k)).ToList();
                    foreach (var key in keysToRemove)
                    {
                        locString.Translations.Remove(key);
                    }

                    // 2. 更新或添加表格中的语言
                    foreach (string langKey in currentLanguages)
                    {
                        string value = row[langKey] as string;

                        if (!string.IsNullOrEmpty(value))
                        {
                            locString.Translations[langKey] = value;
                        }
                        else
                        {

                             locString.Translations[langKey] = string.Empty;
                        }
                    }
                }

                // 序列化并保存
                string jsonOutput = JsonSerializer.Serialize(_currentTemplate, _jsonOptions);
                
                File.WriteAllText(CurrentFilePath, jsonOutput);
                StatusMessage = $"保存成功: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
            }
        }
    }
}
