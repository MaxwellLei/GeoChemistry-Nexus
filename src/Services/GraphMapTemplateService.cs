using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeoChemistryNexus.Services;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public static class GraphMapTemplateService
    {
        /// <summary>
        /// 【模板列表】解析绘图模板的JSON数据，并根据指定语言构建一个树形结构。
        /// </summary>
        /// <param name="jsonContent">包含模板数据的JSON字符串。</param>
        /// <returns>根节点，其 Children 属性包含了所有顶层模板节点。</returns>
        public static GraphMapTemplateNode Parse(string jsonContent)
        {
            var rootNode = new GraphMapTemplateNode { Name = "Root" };

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return rootNode;
            }

            // 反序列化JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var templateList = JsonSerializer.Deserialize<List<JsonTemplateItem>>(jsonContent, options);

            if (templateList == null)
            {
                return rootNode;
            }

            const string separator = ">";

            foreach (var item in templateList)
            {
                // 获取分类路径 (强制使用当前APP语言，忽略 OverrideLanguage)
                string selectedPath = GetPathForAppLanguage(item.NodeList);

                // 分割选择的路径
                var pathParts = selectedPath.Split(separator).Select(p => p.Trim()).ToArray();

                var currentNode = rootNode;

                // 遍历路径的每个部分，构建节点树
                foreach (var partName in pathParts)
                {
                    // 检查当前节点的子节点中是否已存在同名的节点
                    var existingNode = currentNode.Children.FirstOrDefault(c => c.Name == partName);

                    if (existingNode != null)
                    {
                        // 如果存在，则将当前节点切换为现有节点
                        currentNode = existingNode;
                    }
                    else
                    {
                        // 如果节点不存在，则创建一个新节点
                        var newNode = new GraphMapTemplateNode
                        {
                            Name = partName,
                            Parent = currentNode    //设置父节点
                        };
                        currentNode.Children.Add(newNode);
                        currentNode = newNode;
                    }
                }

                // 将模板路径赋值给叶子节点
                currentNode.GraphMapPath = item.GraphMapPath;
                // 获取哈希值
                currentNode.FileHash = item.FileHash;
            }

            return rootNode;
        }

        /// <summary>
        /// 解析并将模板数据合并到现有的根节点中。
        /// </summary>
        /// <param name="rootNode">现有的根节点。</param>
        /// <param name="jsonContent">包含模板数据的JSON字符串。</param>
        /// <param name="isCustom">是否为自定义模板数据。</param>
        public static void ParseAndMerge(GraphMapTemplateNode rootNode, string jsonContent, bool isCustom = false)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));
            if (string.IsNullOrWhiteSpace(jsonContent)) return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var templateList = JsonSerializer.Deserialize<List<JsonTemplateItem>>(jsonContent, options);

            if (templateList == null) return;

            const string separator = ">";

            foreach (var item in templateList)
            {
                string selectedPath = GetPathForAppLanguage(item.NodeList);
                var pathParts = selectedPath.Split(separator).Select(p => p.Trim()).ToArray();
                var currentNode = rootNode;

                foreach (var partName in pathParts)
                {
                    var existingChild = currentNode.Children.FirstOrDefault(c => c.Name == partName);
                    if (existingChild == null)
                    {
                        var newChild = new GraphMapTemplateNode { Name = partName, Parent = currentNode };
                        currentNode.Children.Add(newChild);
                        currentNode = newChild;
                    }
                    else
                    {
                        currentNode = existingChild;
                    }
                }

                currentNode.GraphMapPath = item.GraphMapPath;
                currentNode.FileHash = item.FileHash;
                currentNode.IsCustomTemplate = isCustom;
            }
        }

        /// <summary>
        /// 将表示样式的字符串转换为 ScottPlot 的 LinePattern 枚举。
        /// </summary>
        public static LinePattern GetLinePattern(string style)
        {
            return style.ToString()?.ToLower() switch
            {
                "solid" => LinePattern.Solid,
                "dash" => LinePattern.Dashed,
                "dot" => LinePattern.Dotted,
                "denselydashed" => LinePattern.DenselyDashed,
                _ => LinePattern.Solid, // 默认值为实线
            };
        }

        // 颜色转换辅助方法
        public static string ConvertWpfHexToScottPlotHex(string wpfHex)
        {
            // 移除 '#'
            if (wpfHex.StartsWith("#"))
            {
                wpfHex = wpfHex.Substring(1);
            }

            // 如果是 #RRGGBB (长度为6)
            if (wpfHex.Length == 6)
            {
                return "#" + wpfHex;
            }

            // 如果是 #AARRGGBB (长度为8)
            if (wpfHex.Length == 8)
            {
                string alpha = wpfHex.Substring(0, 2);
                string rgb = wpfHex.Substring(2, 6);
                return $"#{rgb}{alpha}"; // 重新排列为 #RRGGBBAA
            }

            // 对于无效格式，返回一个默认值，例如黑色
            return "#000000";
        }

        /// <summary>
        /// 用于临时匹配 JSON 结构的辅助类
        /// </summary>
        public class JsonTemplateItem
        {
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
        }

        /// <summary>
        /// 向自定义模板列表添加新项
        /// </summary>
        /// <returns>返回更新后的 JSON 字符串</returns>
        public static string AddCustomTemplateEntry(string listPath, LocalizedString nodeList, string graphMapPath, string fileHash)
        {
            List<JsonTemplateItem> list;
            if (System.IO.File.Exists(listPath))
            {
                try 
                {
                    string json = System.IO.File.ReadAllText(listPath);
                    list = JsonSerializer.Deserialize<List<JsonTemplateItem>>(json) ?? new List<JsonTemplateItem>();
                }
                catch 
                {
                    list = new List<JsonTemplateItem>();
                }
            }
            else
            {
                list = new List<JsonTemplateItem>();
            }

            // 检查是否已存在
            var existing = list.FirstOrDefault(x => x.GraphMapPath == graphMapPath);
            if (existing != null)
            {
                existing.NodeList = nodeList;
                existing.FileHash = fileHash;
            }
            else
            {
                list.Add(new JsonTemplateItem
                {
                    NodeList = nodeList,
                    GraphMapPath = graphMapPath,
                    FileHash = fileHash
                });
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string output = JsonSerializer.Serialize(list, options);
            
            // 确保目录存在
            string dir = System.IO.Path.GetDirectoryName(listPath);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            
            System.IO.File.WriteAllText(listPath, output);
            
            return output;
        }

        /// <summary>
        /// 从自定义模板列表中移除项
        /// </summary>
        public static void RemoveCustomTemplateEntry(string listPath, string graphMapPath)
        {
            if (!System.IO.File.Exists(listPath)) return;

            try
            {
                string json = System.IO.File.ReadAllText(listPath);
                var list = JsonSerializer.Deserialize<List<JsonTemplateItem>>(json) ?? new List<JsonTemplateItem>();

                // 查找并移除
                var itemToRemove = list.FirstOrDefault(x => x.GraphMapPath == graphMapPath);
                if (itemToRemove != null)
                {
                    list.Remove(itemToRemove);
                    
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string output = JsonSerializer.Serialize(list, options);
                    
                    System.IO.File.WriteAllText(listPath, output);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing custom template entry: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前APP语言对应的路径，忽略 LocalizedString.OverrideLanguage 设置。
        /// </summary>
        private static string GetPathForAppLanguage(LocalizedString localizedString)
        {
            if (localizedString == null) return string.Empty;

            // 使用 LanguageService 获取当前界面语言
            string appLang = LanguageService.CurrentLanguage;

            if (localizedString.Translations != null && localizedString.Translations.ContainsKey(appLang))
            {
                return localizedString.Translations[appLang];
            }

            // 回退到默认语言
            if (!string.IsNullOrEmpty(localizedString.Default) &&
                localizedString.Translations != null &&
                localizedString.Translations.ContainsKey(localizedString.Default))
            {
                return localizedString.Translations[localizedString.Default];
            }

            return string.Empty;
        }

        /// <summary>
        /// 验证模板版本是否兼容
        /// </summary>
        /// <param name="template">模板对象</param>
        /// <returns>如果是 true 则兼容，否则不兼容</returns>
        public static bool IsVersionCompatible(GraphMapTemplate template)
        {
            if (template == null) return false;
            float currentVersion = UpdateHelper.GetCurrentVersionFloat();
            return template.Version <= currentVersion;
        }
    }
}
