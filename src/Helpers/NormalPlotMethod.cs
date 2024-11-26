using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ScottPlot;
using ScottPlot.WPF;
using System.Data;
using GeoChemistryNexus.ViewModels;
using GeoChemistryNexus.Models;
using System.Reflection.Metadata;

namespace GeoChemistryNexus.Helpers
{
    public static class NormalPlotMethod
    {
        public static object pointObject;

        public static async Task<int> Vermessch_2006_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
        {

            await Task.Run(() =>
            {
                // 创建存储DF1和DF2值的列表
                var df1Values = new List<double>();
                var df2Values = new List<double>();

                int skippedRows = 0; // 计数跳过的行
                foreach (DataRow row in dataTable.Rows)
                {
                    bool skipRow = false;
                    var values = new Dictionary<string, double>();

                    // 检查和转换值
                    foreach (string column in MainWindowViewModel._previousSelectedNode.PlotTemplate.RequiredElements)
                    {
                        // 检查当前列是否为空或无效
                        if (row[column] == DBNull.Value || string.IsNullOrWhiteSpace(row[column].ToString()))
                        {
                            skipRow = true; // 标记为跳过该行
                            break; // 退出当前列的检查
                        }
                        // 尝试将值转换为double类型
                        if (!double.TryParse(row[column].ToString(), out double value) || value == 0)
                        {
                            skipRow = true; // 标记为跳过该行
                            break; // 退出当前列的检查
                        }
                        values[column] = value; // 将值添加到字典中
                    }

                    // 如果标记为跳过，则增加跳过的行计数
                    if (skipRow)
                    {
                        skippedRows++;
                        continue; // 跳过当前行，继续下一行的处理
                    }

                    // 计算DF1的值
                    double df1 = 0.555 * Math.Log(values["Al2O3"] / values["SiO2"]) +
                                 3.822 * Math.Log(values["TiO2"] / values["SiO2"]) +
                                 0.522 * Math.Log(values["CaO"] / values["SiO2"]) +
                                 1.293 * Math.Log(values["MgO"] / values["SiO2"]) -
                                 0.531 * Math.Log(values["MnO"] / values["SiO2"]) -
                                 0.145 * Math.Log(values["K2O"] / values["SiO2"]) -
                                 0.399 * Math.Log(values["Na2O"] / values["SiO2"]);

                    // 计算DF2的值
                    double df2 = 3.796 * Math.Log(values["Al2O3"] / values["SiO2"]) +
                                 0.008 * Math.Log(values["TiO2"] / values["SiO2"]) -
                                 2.868 * Math.Log(values["CaO"] / values["SiO2"]) +
                                 0.313 * Math.Log(values["MgO"] / values["SiO2"]) +
                                 0.650 * Math.Log(values["MnO"] / values["SiO2"]) +
                                 1.421 * Math.Log(values["K2O"] / values["SiO2"]) -
                                 3.017 * Math.Log(values["Na2O"] / values["SiO2"]);

                    // 将计算得到的DF1和DF2添加到各自的列表中
                    df1Values.Add(df1);
                    df2Values.Add(df2);
                }

                // 如果没有有效的数据点可供绘图，则终止方法
                if (df1Values.Count == 0 || df2Values.Count == 0)
                {
                    return -1;      //没有有效的数据点可供绘图。
                }
                // 使用 Dispatcher 在 UI 线程上更新 UI 元素
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    pointObject = plot.Add.ScatterPoints(df1Values.ToArray(), df2Values.ToArray());
                });
                return skippedRows;
            });
            return -999;

        }
    }
}
