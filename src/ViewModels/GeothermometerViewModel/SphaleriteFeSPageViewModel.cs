using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using MathNet.Numerics.RootFinding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.ViewModels.GeothermometerViewModel
{
    public partial class SphaleriteFeSPageViewModel: ObservableObject
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

        // 初始化 
        public SphaleriteFeSPageViewModel()
        {
            // 初始化表格
            InitDataExcel();
            // 初始化数据
            InitData();
        }

        // 初始化表格
        private void InitDataExcel()
        {
            // 创建新的 DataTable
            var dataTable = new DataTable();

            // 添加列
            DataColumn feSColumn = new DataColumn("FeS(Mol%)", typeof(double));
            DataColumn fSColumn = new DataColumn("fS(log10)", typeof(double));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(double));

            dataTable.Columns.Add(feSColumn);
            dataTable.Columns.Add(fSColumn);
            dataTable.Columns.Add(temperatureCColumn);

            ExcelData = dataTable;
        }

        // 初始化数据
        private void InitData()
        {
            //A_TiO2 = 0.8f;
            //A_SiO2 = 1.0f;
            //MPa = 300;
        }

        // 定义方程
        private float SolveTemperature(float FeS_value, float log_fs_value)
        {
            // 定义方程
            Func<float, float> equation = T =>
                72.26695f - 15900.5f / T
                + 0.01448f * log_fs_value
                - 0.38918f * ((float)Math.Pow(10, 8) / (T * T))
                - (7205.5f / T) * log_fs_value
                - 0.34486f * log_fs_value * log_fs_value
                - FeS_value;

            // 使用 Brent 方法求解 T
            float initialGuess = 800f; // 设定初始猜测值，比如 800K
            float lowerBound = 1f; // 设定下界
            float upperBound = 2000f; // 设定上界
            float accuracy = 1e-6f; // 设定精度

            return (float)Brent.FindRoot(
                x => (double)equation((float)x),
                (double)lowerBound,
                (double)upperBound,
                (double)accuracy,
                100);
        }

        // 计算温度计
        [RelayCommand]
        private void CalTem()
        {
            if (ExcelData.Rows.Count>=1)
            {
                // 遍历 DataTable 中的每一行
                foreach (DataRow row in ExcelData.Rows)
                {
                    float feS = Convert.ToSingle(row[0]);
                    float fs = Convert.ToSingle(row[1]);

                    try
                    {
                        float T = SolveTemperature(feS, fs);
                        row[2] = T;
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Error(ex.Message);
                    }
                }

                StepIndex++;
            }
        }

        // 重置数据
        [RelayCommand]
        private void ReSet()
        {
            // 重置属性数据
            InitData();
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
