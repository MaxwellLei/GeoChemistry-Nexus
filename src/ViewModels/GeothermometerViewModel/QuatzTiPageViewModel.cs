using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GeoChemistryNexus.ViewModels.GeothermometerViewModel
{
    public partial class QuatzTiPageViewModel: ObservableObject
    {
        // 数据表格
        [ObservableProperty]
        private DataTable excelData;

        // 步骤条
        [ObservableProperty]
        private int stepIndex;

        // 显示误差范围
        [ObservableProperty]
        private bool mode;

        // 初始化 
        public QuatzTiPageViewModel()
        {
            InitDataExcel01();
            // 初始化数据
            InitData();
        }

        // 饱和温度 初始化表格
        private void InitDataExcel01()
        {
            // 创建新的 DataTable
            var tempExcelData = new DataTable();

            // 添加列
            DataColumn tiColumn = new DataColumn("Ti(ppm)", typeof(float));
            DataColumn temperatureKColumn = new DataColumn("T(K)", typeof(string));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(string));

            tempExcelData.Columns.Add(tiColumn);
            tempExcelData.Columns.Add(temperatureKColumn);
            tempExcelData.Columns.Add(temperatureCColumn);

            ExcelData = tempExcelData;
        }

        // 初始化数据
        private void InitData()
        {
            Mode = false;
        }

        // 温度计计算公式
        public (float T, float error) CalculateTemperature(float xQtz)
        {
            // 常数定义
            float logValue = 5.69f; // log 的均值
            float logError = 0.02f; // log 的误差
            float numerator = 3765f; // 分子的均值
            float numeratorError = 24; // 分子的误差

            // 计算 log(X^qtz)
            float logXQtZ = (float)Math.Log10(xQtz);

            // 计算分母 (5.69 - log(X^qtz))
            float denominatorMean = logValue - logXQtZ;

            // 计算 T
            float T = numerator / denominatorMean;

            // 误差传播公式
            float denominatorError = Math.Abs(logError); // log 的误差
            float TError = (float)Math.Sqrt(
                Math.Pow(numeratorError / denominatorMean, 2) +
                Math.Pow(numerator * denominatorError / Math.Pow(denominatorMean, 2), 2)
            );

            return (T, TError); // 返回 T 和误差
        }

        // 计算温度计
        [RelayCommand]
        private void CalTem()
        {
            // 遍历 DataTable 中的每一行
            foreach (DataRow row in ExcelData.Rows)
            {
                float tti = Convert.ToSingle(row[0]);
                (float tK, float errorV) = CalculateTemperature(tti);

                if (mode)
                {
                    // 调用计算函数
                    row[1] = tK + "±" + errorV;

                    // 换算 摄氏度
                    row[2] = (tK - 273.15) + "±" + errorV;
                }
                else
                {
                    // 调用计算函数
                    row[1] = tK;

                    // 换算 摄氏度
                    row[2] = tK - 273.15;
                }
            }

            StepIndex++;
        }

        // 重置数据
        [RelayCommand]
        private void ReSet()
        {
            // 重置表格
            InitDataExcel01();

            // 重置步骤条
            StepIndex = 0;

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
