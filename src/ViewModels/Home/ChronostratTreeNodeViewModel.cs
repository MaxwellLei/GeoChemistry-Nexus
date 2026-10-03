using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Models.Chronostrat;
using System.Collections.ObjectModel;

namespace GeoChemistryNexus.ViewModels.Home
{
    /// <summary>
    /// 年代地层树节点 ViewModel，支持多级折叠展开与选择状态绑定。
    /// </summary>
    public partial class ChronostratTreeNodeViewModel : ObservableObject
    {
        public ChronostratUnitRecord Unit { get; }

        public ChronostratTreeNodeViewModel? ParentNode { get; }

        public ObservableCollection<ChronostratTreeNodeViewModel> Children { get; } = new();

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        public bool HasChildren => Children.Count > 0;

        public string DisplayName => Unit.GetDisplayName();

        public string SubDisplayName => Unit.GetSubDisplayName();

        public void RefreshLanguage()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(SubDisplayName));
            foreach (var child in Children)
            {
                child.RefreshLanguage();
            }
        }

        public ChronostratTreeNodeViewModel(ChronostratUnitRecord unit, ChronostratTreeNodeViewModel? parent = null)
        {
            Unit = unit;
            ParentNode = parent;
        }

        /// <summary>
        /// 递归设置整棵子树的展开状态。
        /// </summary>
        public void SetExpandedRecursive(bool expand)
        {
            IsExpanded = expand;
            foreach (var child in Children)
            {
                child.SetExpandedRecursive(expand);
            }
        }

        /// <summary>
        /// 展开至指定的层级（例如 Period 纪级）：
        /// 所有 Rank 小于 targetRank 的节点全部展开；
        /// 等于或大于 targetRank 的节点自身折叠，但若有子代则保留随时展开能力。
        /// </summary>
        public void ExpandToRank(ChronostratRank targetRank)
        {
            // Rank 越小层级越高：SuperEon(1) < Eon(2) < Era(3) < Period(4) < SubPeriod(5) < Epoch(6) < Age(7)
            if (Unit.Rank < targetRank)
            {
                IsExpanded = true;
            }
            else
            {
                IsExpanded = false;
            }

            foreach (var child in Children)
            {
                child.ExpandToRank(targetRank);
            }
        }

        /// <summary>
        /// 确保从根节点到自身的所有祖先节点全部处于展开状态，以便在界面上可见。
        /// </summary>
        public void EnsurePathExpanded()
        {
            var p = ParentNode;
            while (p != null)
            {
                p.IsExpanded = true;
                p = p.ParentNode;
            }
        }
    }
}
