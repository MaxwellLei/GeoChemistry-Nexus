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

namespace GeoChemistryNexus.ViewModels.GeothermometerViewModel
{
    public partial class ZirconTiPageViewModel: ObservableObject
    {
        // 数据表格
        [ObservableProperty]
        private DataTable excelData;

        // 步骤条
        [ObservableProperty]
        private int stepIndex;

        //压力
        [ObservableProperty]
        private float mPa;

        //TiO2 活度
        [ObservableProperty]
        private float a_TiO2;

        //SiO2 活度
        [ObservableProperty]
        private float a_SiO2;

        // 初始化
        public ZirconTiPageViewModel()
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
            DataColumn tiO2Column = new DataColumn("Ti(ppm)", typeof(double));
            DataColumn temperatureCColumn = new DataColumn("T(K)", typeof(double));
            DataColumn temperatureKColumn = new DataColumn("T(℃)", typeof(double));

            dataTable.Columns.Add(tiO2Column);
            dataTable.Columns.Add(temperatureCColumn);
            dataTable.Columns.Add(temperatureKColumn);

            ExcelData = dataTable;
        }

        // 初始化数据
        private void InitData()
        {
            A_TiO2 = 0.8f;
            A_SiO2 = 1.0f;
            MPa = 300;
        }

        // 过滤检测任务
        private bool CheckDataStatus()
        {
            // 检查设定值
            if(!(MPa > 0 && A_SiO2 > 0 && A_TiO2 > 0))
            {
                MessageHelper.Warning("请检查参数设定是否合法");
                return false;
            }
            // 检查输入值
            return true;
        }

        // 温度计计算公式
        public static float Calculate(float Mpa, float Ti, float A_TiO2, float A_SiO2)
        {
            return (float)((-4800 + (0.4748 * (Mpa - 1000))) / (Math.Log10(Ti) - 5.711 - Math.Log10(A_TiO2) + Math.Log10(A_SiO2)));
        }

        // 计算温度计
        [RelayCommand]
        private void CalTem()
        {

            if (CheckDataStatus())
            {
                // 遍历 DataTable 中的每一行
                foreach (DataRow row in ExcelData.Rows)
                {
                    // 假设 Ti 是表格的第一列
                    float Ti = Convert.ToSingle(row[0]);

                    // 调用计算函数
                    row[1] = Calculate(MPa, Ti, A_TiO2, A_SiO2);

                    // 换算 摄氏度
                    row[2] = Convert.ToSingle(row[1]) - 273.15;
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
