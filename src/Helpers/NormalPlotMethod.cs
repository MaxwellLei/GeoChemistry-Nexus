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
        Func<Dictionary<string, double>, (double df1, double df2)> calculatePoints)
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

            string group = row["Group"].ToString();
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
    private static (bool success, Dictionary<string, double> values) ExtractValues(DataRow row)
    {
        var values = new Dictionary<string, double>();
        foreach (string column in MainWindowViewModel._previousSelectedNode.PlotTemplate.RequiredElements)
        {
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
    private static (double df1, double df2) CalculateVermessch2006Points(Dictionary<string, double> values)
    {
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

        return (df1, df2);
    }

    // Vermessch_2006_b 计算方法
    private static (double df1, double df2) CalculateVermessch2006BPoints(Dictionary<string, double> values)
    {
        double tiValue = ChemicalHelper.ConvertOxideToElementPpm(values["TiO2"], 47.867, 79.866, 1);

        double df1 = 0.016 * Math.Log(values["Zr"] / tiValue) -
                     2.961 * Math.Log(values["Y"] / tiValue) +
                     1.500 * Math.Log(values["Sr"] / tiValue);

        double df2 = 1.474 * Math.Log(values["Zr"] / tiValue) +
                     2.143 * Math.Log(values["Y"] / tiValue) +
                     1.840 * Math.Log(values["Sr"] / tiValue);

        return (df1, df2);
    }

    // Saccani_2015 计算方法
    private static (double df1, double df2) CalculateSaccani2015Points(Dictionary<string, double> values)
    {
        double thNormalized = ChemicalHelper.NormalizeValue(values["Th"], 0.12);
        double nbNormalized = ChemicalHelper.NormalizeValue(values["Nb"], 2.33);
        return (thNormalized, nbNormalized);
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
}