using CommunityToolkit.Mvvm.ComponentModel;

namespace GeoChemistryNexus.Models.SpiderDiagram
{
    /// <summary>
    /// 蛛网图元素项视图模型，用于元素选择和排序
    /// </summary>
    public partial class ElementItemViewModel : ObservableObject
    {
        /// <summary>
        /// 元素符号
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// 是否选中（参与绘图）
        /// </summary>
        [ObservableProperty]
        private bool _isSelected = true;

        public ElementItemViewModel() { }

        public ElementItemViewModel(string name, bool isSelected = true)
        {
            _name = name;
            _isSelected = isSelected;
        }
    }
}
