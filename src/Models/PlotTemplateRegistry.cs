using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GeoChemistryNexus.Models
{
    public class PlotTemplateRegistry
    {
        private Dictionary<string, PlotTemplate> _templates = new Dictionary<string, PlotTemplate>();

        public void RegisterTemplate(string[] categories, string name, Action<ScottPlot.Plot> drawMethod,
            Func<ScottPlot.Plot, DataTable, Task<int>> plotMethod, string description, string[] requiredElements)
        {
            string key = string.Join("/", categories) + "/" + name;
            _templates[key] = new PlotTemplate
            {
                Name = name,
                DrawMethod = drawMethod,
                PlotMethod = plotMethod,
                Description = description,
                RequiredElements = requiredElements
            };
        }


        public TreeNode GenerateTreeStructure()
        {
            var root = new TreeNode { Name = "绘图类型" };

            foreach (var key in _templates.Keys)
            {
                var parts = key.Split('/');
                var templateName = parts.Last();
                var categories = parts.Take(parts.Length - 1).ToArray();

                AddToTree(root, categories, templateName, _templates[key]);
            }

            return root;
        }

        private void AddToTree(TreeNode parent, string[] categories, string templateName, PlotTemplate template)
        {
            if (categories.Length == 0)
            {
                parent.Children.Add(new TreeNode
                {
                    Name = templateName,
                    PlotTemplate = template
                });
                return;
            }

            var categoryName = categories[0];
            var categoryNode = parent.Children.FirstOrDefault(c => c.Name == categoryName);
            if (categoryNode == null)
            {
                categoryNode = new TreeNode { Name = categoryName };
                parent.Children.Add(categoryNode);
            }

            AddToTree(categoryNode, categories.Skip(1).ToArray(), templateName, template);
        }

        public TreeNode GenerateListFromConfig(PlotListConfig config)
        {
            // 创建根节点
            var root = new TreeNode { Name = "绘图类型" };

            // 遍历所有 ListNodeConfig 对象
            foreach (var nodeConfig in config.listNodeConfigs)
            {
                // 当前节点从根节点开始
                var currentNode = root;
                var path = nodeConfig.rootNode;

                // 遍历路径中的所有元素（除了最后一个）
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var category = path[i];
                    // 查找是否已存在该分类节点
                    var childNode = currentNode.Children.FirstOrDefault(c => c.Name == category);
                    if (childNode == null)
                    {
                        // 不存在则创建新节点
                        childNode = new TreeNode { Name = category };
                        currentNode.Children.Add(childNode);
                    }
                    // 更新当前节点为子节点
                    currentNode = childNode;
                }

                // 创建叶子节点
                var leafName = path.Last();
                var leafNode = new TreeNode
                {
                    Name = leafName,
                    BaseMapPath = nodeConfig.baseMapPath,
                    rootNode = nodeConfig.rootNode

                    //PlotTemplate = new PlotTemplate
                    //{
                    //    RequiredElements = nodeConfig.
                    //}
                };
                currentNode.Children.Add(leafNode);
            }

            return root;
        }
    }
}
