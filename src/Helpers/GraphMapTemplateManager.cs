using GeoChemistryNexus.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoChemistryNexus.Helpers
{
    public static class GraphMapTemplateParser
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
                // 获取分类路径
                string selectedPath = item.NodeList.Get();

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
        /// 【绘制底图】根据单个模板的JSON内容，将底图元素加载到 ScottPlot 控件上。
        /// </summary>
        /// <param name="plot">要绘制图形的 ScottPlot.Plot 对象。</param>
        /// <param name="templateJsonContent">单个 GraphMapTemplate 对象的JSON字符串。</param>
        public static void LoadMap(Plot plot, string templateJsonContent)
        {
            if (plot == null)
            {
                throw new ArgumentNullException(nameof(plot));
            }

            if (string.IsNullOrWhiteSpace(templateJsonContent))
            {
                return; // 如果没有内容，则不执行任何操作
            }

            // 设置反序列化选项
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 反序列化为单个 GraphMapTemplate 对象
            var template = JsonSerializer.Deserialize<GraphMapTemplate>(templateJsonContent, options);

            if (template == null || template.Info == null)
            {
                return; // 无效的模板或信息
            }

            var info = template.Info;

            // 清空现有的 Plottable 对象，开始绘制新底图
            plot.Clear();

            // 绘制线条
            if (info.Lines != null)
            {
                foreach (var lineDef in info.Lines)
                {
                    if (lineDef.Start == null || lineDef.End == null) continue;

                    var linePlot = plot.Add.Line(lineDef.Start.X, lineDef.Start.Y, lineDef.End.X, lineDef.End.Y);

                    // 设置样式
                    linePlot.LineWidth = lineDef.Width;
                    linePlot.Color = Color.FromHex(lineDef.Color);
                    linePlot.LinePattern = GetLinePattern(lineDef.Style.ToString());
                }
            }

            // 可以在这里添加对其他图形元素（点、多边形、注释等）的处理
            // 例如：
            // 2. 绘制多边形 (Polygons)
            // 3. 绘制点 (Points)
            // 4. 绘制注释 (Annotations)

            // 最终调整坐标轴等
            plot.Axes.AutoScale();
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
        private class JsonTemplateItem
        {
            public LocalizedString NodeList { get; set; }
            public string GraphMapPath { get; set; }
            public string FileHash { get; set; }
        }
    }
}
