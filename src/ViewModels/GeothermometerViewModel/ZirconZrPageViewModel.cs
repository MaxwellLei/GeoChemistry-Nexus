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
    public partial class ZirconZrPageViewModel: ObservableObject
    {
        // 数据表格
        [ObservableProperty]
        private DataTable excelData;

        // 步骤条
        [ObservableProperty]
        private int stepIndex;

        //计算模式，目标
        [ObservableProperty]
        private int mode;

        // 初始化 
        public ZirconZrPageViewModel()
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
            DataColumn zrColumn = new DataColumn("Zr(ppm)", typeof(float));
            DataColumn siO2Column = new DataColumn("SiO2(ppm)", typeof(float));
            DataColumn al2O3Column = new DataColumn("Al2O3(ppm)", typeof(float));
            DataColumn caOColumn = new DataColumn("CaO(ppm)", typeof(float));
            DataColumn k2OColumn = new DataColumn("K2O(ppm)", typeof(float));
            DataColumn naO2Column = new DataColumn("Na2O(ppm)", typeof(float));
            DataColumn temperatureKColumn = new DataColumn("T(K)", typeof(float));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(float));

            tempExcelData.Columns.Add(zrColumn);
            tempExcelData.Columns.Add(siO2Column);
            tempExcelData.Columns.Add(al2O3Column);
            tempExcelData.Columns.Add(caOColumn);
            tempExcelData.Columns.Add(k2OColumn);
            tempExcelData.Columns.Add(naO2Column);
            tempExcelData.Columns.Add(temperatureKColumn);
            tempExcelData.Columns.Add(temperatureCColumn);

            ExcelData = tempExcelData;
        }

        // 主量温度 初始化表格
        public void InitDataExcel02()
        {
            // 创建新的 DataTable
            var tempExcelData = new DataTable();

            // 添加列
            DataColumn zrColumn = new DataColumn("Zr(ppm)", typeof(float));
            DataColumn siO2Column = new DataColumn("SiO2(ppm)", typeof(float));
            DataColumn al2O3Column = new DataColumn("Al2O3(ppm)", typeof(float));
            DataColumn fe2O3Column = new DataColumn("Fe2O3(ppm)", typeof(float));
            DataColumn feOColumn = new DataColumn("FeO(ppm)", typeof(float));
            DataColumn mgColumn = new DataColumn("MgO(ppm)", typeof(float));
            DataColumn caOColumn = new DataColumn("CaO(ppm)", typeof(float));
            DataColumn k2OColumn = new DataColumn("K2O(ppm)", typeof(float));
            DataColumn naO2Column = new DataColumn("Na2O(ppm)", typeof(float));
            DataColumn p2O5Column = new DataColumn("P2O5(ppm)", typeof(float));
            DataColumn temperatureKColumn = new DataColumn("T(K)", typeof(float));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(float));

            tempExcelData.Columns.Add(zrColumn);
            tempExcelData.Columns.Add(siO2Column);
            tempExcelData.Columns.Add(al2O3Column);
            tempExcelData.Columns.Add(fe2O3Column);
            tempExcelData.Columns.Add(feOColumn);
            tempExcelData.Columns.Add(mgColumn);
            tempExcelData.Columns.Add(p2O5Column);
            tempExcelData.Columns.Add(caOColumn);
            tempExcelData.Columns.Add(k2OColumn);
            tempExcelData.Columns.Add(naO2Column);
            tempExcelData.Columns.Add(temperatureKColumn);
            tempExcelData.Columns.Add(temperatureCColumn);

            ExcelData = tempExcelData;
        }

        // 初始化数据
        private void InitData()
        {
            Mode = 0;
        }

        // 温度计计算公式
        public static float Calculate(float Mpa, float Ti, float A_TiO2, float A_SiO2)
        {
            return (float)((-4800 + (0.4748 * (Mpa - 1000))) / (Math.Log10(Ti) - 5.711 - Math.Log10(A_TiO2) + Math.Log10(A_SiO2)));
        }

        // 改变计算模型
        partial void OnModeChanged(int value)
        {
            // 如果是计算饱和温度
            if (value == 0)
            {
                InitDataExcel01();
            }
            else
            {
                InitDataExcel02();
            }
        }

        // 计算温度计
        [RelayCommand]
        private void CalTem()
        {
            if (mode == 0)
            {
                // 遍历 DataTable 中的每一行
                foreach (DataRow row in ExcelData.Rows)
                {
                    float tsiO2 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[1]),60.083f);
                    float tal203 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[2]), 101.961f, 2);
                    float tcaO = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[3]), 56.077f);
                    float tk20 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[4]), 94.195f, 2);
                    float tna2O = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[5]), 61.979f, 2);
                    float tempRsM = (2 * tcaO + tk20 + tna2O) / (tsiO2 * tal203);
                    float tK = (float)(12900 / (Math.Log(496000 / Convert.ToSingle(row[0])) + 0.85 * tempRsM + 2.95));

                    // 调用计算函数
                    row[6] = tK;

                    // 换算 摄氏度
                    row[7] = Convert.ToSingle(row[6]) - 273.15;
                }

                StepIndex++;
            }
            else
            {
                // 遍历 DataTable 中的每一行
                foreach (DataRow row in ExcelData.Rows)
                {
                    float tsiO2 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[1]), 60.083f, 10000);
                    float tal203 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[2]), 101.961f, 2*10000);
                    float tfe2O3 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[3]), 159.687f, 10000);
                    float tfeO = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[4]), 71.844f, 10000);
                    float tmgO = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[5]), 40.304f, 10000);
                    float tp2O5 = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[6]), 141.943f, 2*10000);
                    float tcaO = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[7]), 56.077f, 10000);
                    float tk2O = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[8]), 94.195f, 2 * 10000);
                    float tna2O = ChemicalHelper.CalAtomicMass(Convert.ToSingle(row[9]), 61.979f, 2 * 10000);
                    // 求和
                    float tempTotal = tsiO2 + tal203 + tfe2O3 + tfeO + tmgO + tp2O5 + tcaO + tk2O + tna2O;
                    // 归一化
                    float normalizedTsiO2 = tsiO2 / tempTotal;
                    float normalizedTal203 = tal203 / tempTotal;
                    float normalizedTcaO = tcaO / tempTotal;
                    float normalizedTk2O = tk2O / tempTotal;
                    float normalizedTna2O = tna2O / tempTotal;
                    // 计算
                    float m = (2 * normalizedTcaO + normalizedTk2O + normalizedTna2O) / (normalizedTsiO2 * normalizedTal203);
                    // 调用计算温度函数
                    row[10] = (12900 / (Math.Log(496000 / Convert.ToSingle(row[0])) + 0.85 * m + 2.95));

                    // 换算 摄氏度
                    row[11] = Convert.ToSingle(row[10]) - 273.15;
                }

                StepIndex++;
            }
        }

        // 重置数据
        [RelayCommand]
        private void ReSet()
        {
            if(Mode == 0)
            {
                InitDataExcel01();
            }
            else
            {
                InitDataExcel02();
            }
            //ExcelData.Clear();

            // 重置表格数据
            //InitDataExcel();
            // 通知前端
            MessageHelper.Success("重置成功");
        }

        // 导出数据
        [RelayCommand]
        private void Export_Data()
        {
            FileHelper.ExportDataTable(ExcelData);
        }
    }
}
