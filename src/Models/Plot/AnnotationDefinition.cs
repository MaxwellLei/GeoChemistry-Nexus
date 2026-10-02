using CommunityToolkit.Mvvm.ComponentModel;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    /// <summary>
    /// 注释类
    /// </summary>
    public partial class AnnotationDefinition: ObservableObject
    {
        /// <summary>
        /// X 坐标位置
        /// </summary>
        [ObservableProperty]
        private double _x;

        /// <summary>
        /// Y 坐标位置
        /// </summary>
        [ObservableProperty]
        private double _y;

        /// <summary>
        /// 注释内容，多语言
        /// </summary>
        [ObservableProperty]
        private LocalizedString _content = new LocalizedString();

    }
}
