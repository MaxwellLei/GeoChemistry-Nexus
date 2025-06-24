using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels
{
    /// <summary>
    /// 代表单个坐标轴的图层项
    /// </summary>
    public partial class AxisLayerItemViewModel : LayerItemViewModel
    {
        public AxisDefinition AxisDefinition { get; }

        public AxisLayerItemViewModel(AxisDefinition axisDefinition)
            : base(GetAxisName(axisDefinition.Type)) // 根据坐标轴类型设置名称
        {
            AxisDefinition = axisDefinition;
        }

        private static string GetAxisName(string type)
        {
            return type switch
            {
                "Left" => LanguageService.Instance["left_y_axis"],
                "Right" => LanguageService.Instance["right_y_axis"],
                "Bottom" => LanguageService.Instance["bottom_x_axis"],
                "Top" => LanguageService.Instance["top_x_axis"],
                _ => LanguageService.Instance["unknown_axis"]
            };
        }
    }
}
