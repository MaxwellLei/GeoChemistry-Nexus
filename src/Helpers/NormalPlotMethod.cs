using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using ScottPlot;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System;
using System.Linq;

public static class NormalPlotMethod
{
    public static Dictionary<string, object> pointObject = new Dictionary<string, object>();

    // 通用的数据处理方法
    private static (Dictionary<string, List<(double, double)>> groupedData, int skippedRows) ProcessData(
        DataTable dataTable,
        Func<Dictionary<string, object>, (double df1, double df2)> calculatePoints)
    {
        var groupedData = new Dictionary<string, List<(double, double)>>();
        int skippedRows = 0;
        foreach (DataRow row in dataTable.Rows)
        {
            var (success, values) = ExtractValues(row);
            if (!success)
            {
                skippedRows++;
                continue;
            }

            // 确保 "Group" 键存在且是字符串
            if (!values.TryGetValue("Group", out var groupObj) || !(groupObj is string group))
            {
                skippedRows++;
                continue;
            }

            var (df1, df2) = calculatePoints(values);
            if (!groupedData.ContainsKey(group))
            {
                groupedData[group] = new List<(double, double)>();
            }
            groupedData[group].Add((df1, df2));
        }
        return (groupedData, skippedRows);
    }

    // 提取行数据的辅助方法
    private static (bool success, Dictionary<string, object> values) ExtractValues(DataRow row)
    {
        var values = new Dictionary<string, object>();
        foreach (string column in MainWindowViewModel._previousSelectedNode.PlotTemplate.RequiredElements)
        {
            // 如果列名是 "Group"，允许任何字符串值
            if (column == "Group")
            {
                values[column] = row[column]?.ToString();
                continue;
            }

            if (row[column] == DBNull.Value ||
                string.IsNullOrWhiteSpace(row[column].ToString()) ||
                !double.TryParse(row[column].ToString(), out double value) ||
                value == 0)
            {
                return (false, null);
            }
            values[column] = value;
        }
        return (true, values);
    }

    // 通用的绘图方法
    private static void PlotData(ScottPlot.Plot plot, Dictionary<string, List<(double, double)>> groupedData)
    {
        pointObject.Clear();
        plot.Legend.IsVisible = true;
        plot.Legend.Alignment = Alignment.UpperRight;
        plot.Legend.Padding = new PixelPadding(10, 10);
        plot.Legend.BackgroundColor = Colors.White.WithAlpha(0.9f);

        foreach (var group in groupedData)
        {
            var df1Values = group.Value.Select(p => p.Item1).ToArray();
            var df2Values = group.Value.Select(p => p.Item2).ToArray();
            var scatter = plot.Add.ScatterPoints(df1Values, df2Values);
            scatter.Label = group.Key;
            pointObject[group.Key] = scatter;
        }
    }

    // Vermessch_2006 计算方法
    private static (double df1, double df2) CalculateVermessch2006Points(Dictionary<string, object> values)
    {
        // Assure value to convert is double
        double df1 = 0.555 * Math.Log(Convert.ToDouble(values["Al2O3"]) / Convert.ToDouble(values["SiO2"])) +
                     3.822 * Math.Log(Convert.ToDouble(values["TiO2"]) / Convert.ToDouble(values["SiO2"])) +
                     0.522 * Math.Log(Convert.ToDouble(values["CaO"]) / Convert.ToDouble(values["SiO2"])) +
                     1.293 * Math.Log(Convert.ToDouble(values["MgO"]) / Convert.ToDouble(values["SiO2"])) -
                     0.531 * Math.Log(Convert.ToDouble(values["MnO"]) / Convert.ToDouble(values["SiO2"])) -
                     0.145 * Math.Log(Convert.ToDouble(values["K2O"]) / Convert.ToDouble(values["SiO2"])) -
                     0.399 * Math.Log(Convert.ToDouble(values["Na2O"]) / Convert.ToDouble(values["SiO2"]));

        double df2 = 3.796 * Math.Log(Convert.ToDouble(values["Al2O3"]) / Convert.ToDouble(values["SiO2"])) +
                     0.008 * Math.Log(Convert.ToDouble(values["TiO2"]) / Convert.ToDouble(values["SiO2"])) -
                     2.868 * Math.Log(Convert.ToDouble(values["CaO"]) / Convert.ToDouble(values["SiO2"])) +
                     0.313 * Math.Log(Convert.ToDouble(values["MgO"]) / Convert.ToDouble(values["SiO2"])) +
                     0.650 * Math.Log(Convert.ToDouble(values["MnO"]) / Convert.ToDouble(values["SiO2"])) +
                     1.421 * Math.Log(Convert.ToDouble(values["K2O"]) / Convert.ToDouble(values["SiO2"])) -
                     3.017 * Math.Log(Convert.ToDouble(values["Na2O"]) / Convert.ToDouble(values["SiO2"]));

        return (df1, df2);
    }

    // Vermessch_2006_b 计算方法
    private static (double df1, double df2) CalculateVermessch2006BPoints(Dictionary<string, object> values)
    {

        double tiValue = ChemicalHelper.ConvertOxideToElementPpm(
                                        Convert.ToDouble(values["TiO2"]), 47.867, 79.866, 1);

        double df1 = 0.016 * Math.Log(Convert.ToDouble(values["Zr"]) / tiValue) -
                     2.961 * Math.Log(Convert.ToDouble(values["Y"]) / tiValue) +
                     1.500 * Math.Log(Convert.ToDouble(values["Sr"]) / tiValue);

        double df2 = 1.474 * Math.Log(Convert.ToDouble(values["Zr"]) / tiValue) +
                     2.143 * Math.Log(Convert.ToDouble(values["Y"]) / tiValue) +
                     1.840 * Math.Log(Convert.ToDouble(values["Sr"]) / tiValue);

        return (df1, df2);
    }

    // Saccani_2015 计算方法
    private static (double df1, double df2) CalculateSaccani2015Points(Dictionary<string, object> values)
    {
        // Assure value to convert is double
        double thNormalized = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Th"]), 0.12);
        double nbNormalized = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Nb"]), 2.33);
        return (Math.Log10(thNormalized), Math.Log10(nbNormalized));
    }

    // Saccani_2015 b 计算方法
    private static (double df1, double df2) CalculateSaccani2015bPoints(Dictionary<string, object> values)
    {
        // Assure value to convert is double
        double y = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Yb"]), 3.05);
        double x = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Dy"]), 4.550);
        return (x, y);
    }

    // Saccani_2015 c 计算方法
    private static (double df1, double df2) CalculateSaccani2015cPoints(Dictionary<string, object> values)
    {
        // Assure value to convert is double
        double ce_n = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Ce"]), 7.50);
        double dy_n = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Dy"]), 4.550);
        double yb_n = ChemicalHelper.NormalizeValue(Convert.ToDouble(values["Yb"]), 3.05);
        double y = ce_n / yb_n;
        double x = dy_n / yb_n;
        return (x, y);
    }

    // TAS 计算方法
    private static (double df1, double df2) CalculateTASPoints(Dictionary<string, object> values)
    {
        // 读取 SiO2 的值作为 x
        double x = Convert.ToDouble(values["SiO2"]);

        // 读取 Na2O 和 K2O 的值并相加作为 y
        double na2o = Convert.ToDouble(values["Na2O"]);
        double k2o = Convert.ToDouble(values["K2O"]);
        double y = na2o + k2o;

        return (x, y);
    }

    // Vermessch_2006 (Majior elements -Fe)
    public static async Task<int> Vermessch_2006_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateVermessch2006Points);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }

    // Vermessch_2006_b (TiO2-Zr-Y-Sr)
    public static async Task<int> Vermessch_2006_b_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateVermessch2006BPoints);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }

    // Saccani_2015 (Th_n-Nb_n)
    public static async Task<int> Saccani_2015_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateSaccani2015Points);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }

    // Saccani_2015 (Yb_n-Dy_n)
    public static async Task<int> Saccani_2015_b_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateSaccani2015bPoints);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }

    // Saccani_2015 (Ce_n/Yb_n-Dy_n/Yb_n)
    public static async Task<int> Saccani_2015_c_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateSaccani2015cPoints);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }

    // (TAS)
    public static async Task<int> TAS_PlotAsync(ScottPlot.Plot plot, DataTable dataTable)
    {
        return await Task.Run(() =>
        {
            var (groupedData, skippedRows) = ProcessData(dataTable, CalculateTASPoints);
            if (!groupedData.Any()) return -1;
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlotData(plot, groupedData));
            return skippedRows;
        });
    }
}