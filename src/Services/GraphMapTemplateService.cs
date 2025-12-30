using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using GeoChemistryNexus.Helpers;

namespace GeoChemistryNexus.Services
{
    public static class GraphMapTemplateService
    {
        /// <summary>
        /// 根据数据库实体列表构建模板树
        /// </summary>
        public static GraphMapTemplateNode BuildTreeFromEntities(List<GraphMapTemplateEntity> entities)
        {
            var rootNode = new GraphMapTemplateNode { Name = "Root" };

            if (entities == null || !entities.Any())
            {
                return rootNode;
            }

            const string separator = ">";

            foreach (var item in entities)
            {
                // 获取分类路径 (强制使用当前APP语言，忽略 OverrideLanguage)
                string selectedPath = GetPathForAppLanguage(item.NodeList);

                if (item.IsCustom)
                {
                    string customRoot = LanguageService.Instance["custom_templates"];
                    if (string.IsNullOrEmpty(customRoot)) customRoot = "Custom Templates";
                    selectedPath = $"{customRoot}{separator}{selectedPath}";
                }

                // 分割选择的路径
                var pathParts = selectedPath.Split(separator).Select(p => p.Trim()).ToArray();

                var currentNode = rootNode;

                // 遍历路径的每个部分，构建节点树
                for (int i = 0; i < pathParts.Length; i++)
                {
                    string partName = pathParts[i];
                    bool isLeaf = (i == pathParts.Length - 1);
                    
                    GraphMapTemplateNode existingNode = null;
                    
                    if (!isLeaf)
                    {
                        // 找到一个名字相同，且 GraphMapPath 为空的节点（纯文件夹）
                        existingNode = currentNode.Children.FirstOrDefault(c => c.Name == partName && string.IsNullOrEmpty(c.GraphMapPath));
                    }
                    else
                    {
                        existingNode = null; 
                    }

                    if (existingNode != null)
                    {
                        // 如果存在可复用的容器节点，则进入该节点
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

                // 将模板信息赋值给叶子节点
                currentNode.GraphMapPath = item.GraphMapPath;
                currentNode.FileHash = item.FileHash;
                currentNode.IsCustomTemplate = item.IsCustom;
                currentNode.TemplateId = item.Id;
                currentNode.Status = item.Status;
            }

            return rootNode;
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
            public string ID { get; set; }
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
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
