using OfficeOpenXml.FormulaParsing.Ranges;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace GeoChemistryNexus.Models
{
    public class BasePlotConfig
    {
        // 图表版本
        public string BaseMapVersion { get; set; }
        // 基础信息
        public BaseInfo baseInfo { get; set; }
        // 图表类型-二维图，三元图，蜘蛛网图等=====   2 二维图； 3 三元图
        public int MapType { get; set; }
        // 图表名称-文件名称
        public string Title { get; set; }
        // 绘图描述
        public string Description { get; set; }
        // 图表绘图设置
        public PlotConfig PlotConfig { get; set; }
        // 图表坐标轴设置
        public PlotAxes PlotAxes { get; set; }
        // 图表刻度轴
        public TickConfig Ticks { get; set; }
        // 图表点位置
        public List<PointConfig> Points { get; set; }
        // 图表多边形位置
        public List<PolygonConfig> Polygons { get; set; }
        // 图表线段位置
        public List<LineConfig> Lines { get; set; }
        // 图表注释位置
        public List<TextConfig> Texts { get; set; }

        public BasePlotConfig() {
            baseInfo = new BaseInfo();
            PlotAxes= new PlotAxes();
            Points = new List<PointConfig>();
            Polygons = new List<PolygonConfig>();
            Lines = new List<LineConfig>();
            Texts = new List<TextConfig>();
        }
    }

    // 基础信息类
    public class BaseInfo
    {
        public string[] rootNode { get; set; }          // 节点路径分类-最后一个节点是名称
        public string description { get; set; }         // 绘图指南内容
        public string[] requiredElements { get; set; }    // 必须的列内容     ["Group","Ti","Zr"，"Y"]
        public string script { get; set; }           // 计算的脚本       return {x:data.Zr／ data.Ti，y:data.Y／data.Ti};
    }

    // 多语言
    public class MultiLanguageText
    {
        public string en_US { get; set; } // 英文
        public string zh_Hans { get; set; } // 中文
    }

    public class PlotConfig
    {
        // 绘图设置-绘图
        public string title { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public float titleFontSize { get; set; }
        public float xFontSize { get; set; }
        public float yFontSize { get; set; }
        public string titleColor { get; set; }
        public string axisTitleColor { get; set; }
        // 绘图设置-背景
        public bool isShowMainGrid { get; set; }                // 是否显示网格
        public string mainGridColor { get; set; }           // 主网格颜色
        public float mainGridSize { get; set; }             // 主网格宽度
        public bool isShowMinorGrid { get; set; }           // 是否显示次网格
        public string minorGridColor { get; set; }           // 次网格颜色
        public float minorGridSize { get; set; }             // 次网格宽度
    }

    public class PlotAxes
    {
        public AxesConfig XAxes { get; set; }
        public AxesConfig YAxes { get; set; }
        public AxesConfig ZAxes { get; set; }
    }

    public class AxesConfig
    {
        public bool isShowMinorGrid { get; set; }           // 是否显示轴
        //public bool isHideMajorTicks { get; set; }           // 是否隐藏主刻度
        //public int axesTickType { get; set; }           // 刻度轴样式
        public float axesTickFontSize { get; set; }           // 刻度轴字体
        public double axesTickSpacing { get; set; }           // 刻度轴间距
        public double[] Limit { get; set; }                 // 数组中分别存放 [min, max]
        public string axesColor { get; set; }           // 刻度轴颜色
    }

    public class PointConfig
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class PolygonConfig
    {
        public List<PointConfig> Points { get; set; }
        public string fillColor { get; set; }
    }

    // 线条类
    public class LineConfig
    {
        public bool isShow { get; set; }      // 是否显示
        public PointConfig start { get; set; }      // 起始点
        public PointConfig end { get; set; }        // 终点
        public string color { get; set; }           // 颜色
        public float linewidth { get; set; }       // 线宽
        public int lineType { get; set; }    // 线条类型
    }

    public class TextConfig
    {
        public bool isShow { get; set; }      // 是否显示
        public string text { get; set; }            // 字体内容
        public double x { get; set; }               // 字体 X 坐标
        public double y { get; set; }               // 字体 Y 坐标
        public float rotation { get; set; }         // 字体旋转
        public float fontSize { get; set; }         // 字体大小
        public string color { get; set; }           // 颜色
    }

    public class TickConfig
    {
        public AxisTickConfig x { get; set; }
        public AxisTickConfig y { get; set; }
    }

    public class AxisTickConfig
    {
        public List<double> positions { get; set; }
        public List<string> labels { get; set; }
    }
}
