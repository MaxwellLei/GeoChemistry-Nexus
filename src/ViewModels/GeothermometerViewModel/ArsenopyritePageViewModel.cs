using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using MathNet.Numerics.Distributions;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels.GeothermometerViewModel
{
    public partial class ArsenopyritePageViewModel : ObservableObject
    {
        // 数据表格
        [ObservableProperty]
        private DataTable excelData;

        // 步骤条
        [ObservableProperty]
        private int stepIndex;

        //压力
        [ObservableProperty]
        private float fS;

        // 标题
        [ObservableProperty]
        private string titleX;

        // 选项标题
        [ObservableProperty]
        private string titleP;

        // 选项
        [ObservableProperty]
        private string titleC;


        // 初始化 
        public ArsenopyritePageViewModel()
        {
            // 初始化表格
            InitDataExcel();
        }

        // 标题改变
        partial void OnTitleXChanged(string oldValue)
        {
            // 初始化表格
            InitDataExcel();
        }

        // 初始化表格
        private void InitDataExcel()
        {
            // 创建新的 DataTable
            var dataTable = new DataTable();

            // 添加列
            DataColumn feSColumn;
            DataColumn fSColumn25;
            DataColumn fSColumn251;
            DataColumn temperatureCColumn;

            if (TitleX == I18n.GetString("BiotiteGTM"))
            {
                TitleP = I18n.GetString("AtomicMassStd");
                TitleC = I18n.GetString("DefaultImport");
                feSColumn = new DataColumn("Ti", typeof(double));
                fSColumn25 = new DataColumn("Mg", typeof(double));
                fSColumn251 = new DataColumn("Fe", typeof(double));
                temperatureCColumn = new DataColumn("T(℃)", typeof(string));
                dataTable.Columns.Add(feSColumn);
                dataTable.Columns.Add(fSColumn25);
                dataTable.Columns.Add(fSColumn251);
                dataTable.Columns.Add(temperatureCColumn);
            }
            else
            {
                TitleP = I18n.GetString("MineralAssemblage");
                TitleC = I18n.GetString("Arsenopyrite");
                feSColumn = new DataColumn("Arsenopyrite", typeof(double));
                fSColumn25 = new DataColumn("Logf(S2)", typeof(double));
                temperatureCColumn = new DataColumn("T(℃)", typeof(string));
                dataTable.Columns.Add(feSColumn);
                dataTable.Columns.Add(fSColumn25);
                dataTable.Columns.Add(temperatureCColumn);
            }

            //DataRow dataRow = dataTable.NewRow();
            //dataRow[0] = 37.663;

            //dataTable.Rows.Add(dataRow);

            ExcelData = dataTable;
        }

        // 第一层映射的数据点
        private (double Percentage, double X)[] percentageToXMapping = new[]
        {
            (29.0, 32.506257569900846),
            (30.0, 41.003953073697566),
            (31.0, 50.67231833822731),
            (32.0, 61.503823),
            (33.0, 73.45785730835725)
        };

        // 第二层映射的范围定义
        private (double MinX, double MaxX, double MinZ, double MaxZ)[] xToZRanges = new[]
        {
            (MinX: 0, MaxX: 35.17156757579819, MinZ: (double)200, MaxZ: (double)300),
            (MinX: 35.17156757579819, MaxX: 59.08098467076307, MinZ: (double)300, MaxZ: (double)400),
            (MinX: 59.08098467076307, MaxX: double.MaxValue, MinZ: (double)400, MaxZ: (double)500)
        };

        public double Calculate(double percentage)
        {
            // 第一步：将百分比映射到X值
            double xValue = MapPercentageToX(percentage);

            // 第二步：将X值映射到Z值
            double zValue = MapXToZ(xValue);

            return zValue;
        }

        private double MapPercentageToX(double percentage)
        {
            for (int i = 0; i < percentageToXMapping.Length - 1; i++)
            {
                double currentPercentage = percentageToXMapping[i].Percentage;
                double nextPercentage = percentageToXMapping[i + 1].Percentage;

                // 检查百分比是否在当前区间内
                if (percentage >= currentPercentage && percentage <= nextPercentage)
                {
                    double currentX = percentageToXMapping[i].X;
                    double nextX = percentageToXMapping[i + 1].X;

                    // 计算在该百分比区间内的比例
                    double ratio = (percentage - currentPercentage) / (nextPercentage - currentPercentage);
                    // 使用该比例计算X值
                    return currentX + (nextX - currentX) * ratio;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(percentage),
                $"超出计算范围");
        }

        private double MapXToZ(double x)
        {
            foreach (var range in xToZRanges)
            {
                if (x >= range.MinX && x < range.MaxX)
                {
                    // 计算在该范围内的插值比例
                    double ratio = (x - range.MinX) / (range.MaxX - range.MinX);
                    // 线性插值计算Z值
                    return range.MinZ + (range.MaxZ - range.MinZ) * ratio;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(x), "超出计算范围");
        }

        // ========================================计算温度
        public double CalculateTemperature(double Ti, double X_Mg)
        {
            double a = -2.3594;
            double b = 4.6482e-9;
            double c = -1.7283;

            // 检查输入值是否有效
            if (Ti <= 0)
            {
                throw new ArgumentException("Ti must be greater than 0");
            }

            // 计算公式
            double lnTi = Math.Log(Ti);
            double numerator = lnTi - a - c * Math.Pow(X_Mg, 3);
            double denominator = b;

            // 检查分母是否为0
            if (denominator == 0)
            {
                throw new DivideByZeroException("分母不合法，计算为 0");
            }

            double result = Math.Pow(numerator / denominator, 0.333);

            return result;
        }



        // 计算温度计
        [RelayCommand]
        private void CalTem()
        {
            if (ExcelData.Rows.Count >= 1)
            {
                // 遍历 DataTable 中的每一行
                foreach (DataRow row in ExcelData.Rows)
                {
                    if (TitleX == "黑云母温度计")
                    {
                        double biotite = Convert.ToDouble(row[0]);
                        double mg = Convert.ToDouble(row[1]);
                        double fe = Convert.ToDouble(row[2]);

                        row[3] = CalculateTemperature(biotite,mg/(mg + fe));

                    }
                    else
                    {
                        double arsenopyrite = Convert.ToDouble(row[0]);

                        try
                        {
                            row[1] = 0.1738800011455738 * MapPercentageToX(arsenopyrite) + -17.294155571073922;
                            row[2] = Calculate(arsenopyrite);
                        }
                        catch (Exception ex)
                        {
                            MessageHelper.Error(ex.Message);
                        }
                    }
                }

                StepIndex++;
            }
        }


        [RelayCommand]
        private void ReSet()
        {
            // 重置表格数据
            InitDataExcel();
            // 通知前端
            MessageHelper.Success(I18n.GetString("ResetSuccess"));
        }

        // 导出数据
        [RelayCommand]
        private void Export_Data()
        {
            FileHelper.ExportDataTable(ExcelData);
        }
    }
}
