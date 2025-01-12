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
using System.Windows;

namespace GeoChemistryNexus.ViewModels.GeothermometerViewModel
{
    public partial class SphaleriteGGIMFisPageViewModel : ObservableObject
    {
        // 数据表格
        [ObservableProperty]
        private DataTable excelData;

        // 步骤条
        [ObservableProperty]
        private int stepIndex;

        // 显示误差
        [ObservableProperty]
        private bool showDeviation;

        // 初始化 
        public SphaleriteGGIMFisPageViewModel()
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
            DataTable tempdata = new DataTable();

            // 添加列
            DataColumn gaColumn = new DataColumn("Ga(ppm)", typeof(float));
            DataColumn geColumn = new DataColumn("Ge(ppm)", typeof(float));
            DataColumn feColumn = new DataColumn("Fe(ppm)", typeof(float));
            DataColumn mnColumn = new DataColumn("Mn(ppm)", typeof(float));
            DataColumn inColumn = new DataColumn("In(ppm)", typeof(float));
            //DataColumn temperatureCColumn = new DataColumn("T(K)", typeof(double));
            DataColumn temperatureKColumn = new DataColumn("T(℃)", typeof(string));

            tempdata.Columns.Add(gaColumn);
            tempdata.Columns.Add(geColumn);
            tempdata.Columns.Add(feColumn);
            tempdata.Columns.Add(mnColumn);
            tempdata.Columns.Add(inColumn);
            //ExcelData.Columns.Add(temperatureCColumn);
            tempdata.Columns.Add(temperatureKColumn);

            ExcelData = tempdata;
        }

        // 初始化数据
        private void InitData()
        {
            ShowDeviation = false;
        }


        // 温度计计算公式
        public string Calculate(float ga, float ge, float fe, float mn, float inConcentration)
        {

            // 计算 PC1*
            float pc1Star = (float)((Math.Log(ga) *0.22 + Math.Log(ge) *0.22) -
                                       Math.Log(fe) * 0.37 - Math.Log(mn) *0.20 - Math.Log(inConcentration) * 0.11);
            // 计算温度 T
            float temperature;
            // 计算误差
            float tempDeviation;
            string resT;

            if (ShowDeviation)
            {
                // 计算温度 T
                temperature = (float)(-54.4 * pc1Star + 208);
                // 计算误差
                tempDeviation = (float)(7.3 *  pc1Star + 10);
                resT = temperature + "±" + tempDeviation;
            }
            else
            {
                // 计算温度 T
                temperature = (float)(-54.4 * pc1Star + 208);
                resT = temperature.ToString();
            }
            
            return resT;
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
                    // 假设 Ti 是表格的第一列
                    float ga = Convert.ToSingle(row[0]);
                    float ge = Convert.ToSingle(row[1]);
                    float fe = Convert.ToSingle(row[2]);
                    float mn = Convert.ToSingle(row[3]);
                    float in_ = Convert.ToSingle(row[4]);

                    // 调用计算函数
                    row[5] = Calculate(ga, ge, fe, mn, in_);
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
