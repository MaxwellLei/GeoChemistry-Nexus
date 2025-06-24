using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public partial class FontDefinition:ObservableObject
    {
        /// <summary>
        /// 字体
        /// </summary>
        [ObservableProperty]
        private string _family = "Arial";

        /// <summary>
        /// 字体大小
        /// </summary>
        [ObservableProperty]
        private float _size = 12;

        /// <summary>
        /// 字体颜色
        /// </summary>
        [ObservableProperty]
        private string _color = "#000000";

        /// <summary>
        /// 粗体样式
        /// </summary>
        [ObservableProperty] 
        private bool _isBold = false;

        /// <summary>
        /// 斜体样式
        /// </summary>
        [ObservableProperty] 
        private bool _isItalic = false;
    }
}
