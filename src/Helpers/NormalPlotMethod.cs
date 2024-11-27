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
        public static Dictionary<string, object> pointObject = new Dictionary<string, object>();

        public static async Task<int> Vermessch_2006_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
        {
            return await Task.Run(() =>
            {
                var groupedData = new Dictionary<string, List<(double, double)>>();
                int skippedRows = 0;

                foreach (DataRow row in dataTable.Rows)
                {
                    bool skipRow = false;
                    var values = new Dictionary<string, double>();
                    string group = row["Group"].ToString();

                    foreach (string column in MainWindowViewModel._previousSelectedNode.PlotTemplate.RequiredElements)
                    {
                        if (row[column] == DBNull.Value || string.IsNullOrWhiteSpace(row[column].ToString()))
                        {
                            skipRow = true;
                            break;
                        }
                        if (!double.TryParse(row[column].ToString(), out double value) || value == 0)
                        {
                            skipRow = true;
                            break;
                        }
                        values[column] = value;
                    }

                    if (skipRow)
                    {
                        skippedRows++;
                        continue;
                    }

                    double df1 = 0.555 * Math.Log(values["Al2O3"] / values["SiO2"]) +
                                 3.822 * Math.Log(values["TiO2"] / values["SiO2"]) +
                                 0.522 * Math.Log(values["CaO"] / values["SiO2"]) +
                                 1.293 * Math.Log(values["MgO"] / values["SiO2"]) -
                                 0.531 * Math.Log(values["MnO"] / values["SiO2"]) -
                                 0.145 * Math.Log(values["K2O"] / values["SiO2"]) -
                                 0.399 * Math.Log(values["Na2O"] / values["SiO2"]);

                    double df2 = 3.796 * Math.Log(values["Al2O3"] / values["SiO2"]) +
                                 0.008 * Math.Log(values["TiO2"] / values["SiO2"]) -
                                 2.868 * Math.Log(values["CaO"] / values["SiO2"]) +
                                 0.313 * Math.Log(values["MgO"] / values["SiO2"]) +
                                 0.650 * Math.Log(values["MnO"] / values["SiO2"]) +
                                 1.421 * Math.Log(values["K2O"] / values["SiO2"]) -
                                 3.017 * Math.Log(values["Na2O"] / values["SiO2"]);

                    if (!groupedData.ContainsKey(group))
                    {
                        groupedData[group] = new List<(double, double)>();
                    }
                    groupedData[group].Add((df1, df2));
                }

                if (groupedData.Count == 0)
                {
                    return -1;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    pointObject.Clear();
                    // 配置图例
                    plot.Legend.IsVisible = true;
                    plot.Legend.Alignment = Alignment.UpperRight; // 设置图例在右上角
                    plot.Legend.Padding = new PixelPadding(10, 10); // 设置内边距
                    plot.Legend.BackgroundColor = Colors.White.WithAlpha(0.9f); // 设置背景色半透明白色
                    foreach (var group in groupedData)
                    {
                        var df1Values = group.Value.Select(p => p.Item1).ToArray();
                        var df2Values = group.Value.Select(p => p.Item2).ToArray();
                        // 添加散点并设置标签
                        var scatter = plot.Add.ScatterPoints(df1Values, df2Values);
                        scatter.Label = group.Key; // 设置图例标签为组名
                        pointObject[group.Key] = scatter;
                    }
                }); 

                return skippedRows;
            });
        }
    }
}
