using GeoChemistryNexus.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel; // 引入此命名空间
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GeoChemistryNexus.Controls
{
    public partial class VerticesEditorControl : UserControl
    {
        public static readonly DependencyProperty VerticesValueProperty =
            DependencyProperty.Register("VerticesValue", typeof(ObservableCollection<PointDefinition>), typeof(VerticesEditorControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ObservableCollection<PointDefinition> VerticesValue
        {
            get { return (ObservableCollection<PointDefinition>)GetValue(VerticesValueProperty); }
            set { SetValue(VerticesValueProperty, value); }
        }

        public VerticesEditorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// “添加新顶点”按钮
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果列表为null，则先创建一个新的
            if (VerticesValue == null)
            {
                VerticesValue = new ObservableCollection<PointDefinition>();
            }
            // 添加顶点
            VerticesValue.Add(new PointDefinition { X = 0, Y = 0 });
        }

        /// <summary>
        /// 单个顶点后的“删除”按钮
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PointDefinition pointToRemove)
            {
                // 删除顶点
                VerticesValue?.Remove(pointToRemove);
            }
        }
    }
}