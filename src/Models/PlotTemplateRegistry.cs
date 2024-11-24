using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GeoChemistryNexus.Models
{
    public class PlotTemplateRegistry
    {
        private Dictionary<string, PlotTemplate> _templates = new Dictionary<string, PlotTemplate>();

        public void RegisterTemplate(string[] categories, string name, Action<ScottPlot.Plot> drawMethod, string description)
        {
            string key = string.Join("/", categories) + "/" + name;
            _templates[key] = new PlotTemplate
            {
                Name = name,
                DrawMethod = drawMethod,
                Description = description
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
    }
}
