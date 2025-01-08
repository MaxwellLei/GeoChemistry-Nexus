using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoChemistryNexus.Helpers;
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
    public partial class ChloritePageViewModel : ObservableObject
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
        public ChloritePageViewModel()
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
            DataColumn feSColumn = new DataColumn("SiO2(wt%)", typeof(double));
            DataColumn fSColumn = new DataColumn("TiO2(wt%)", typeof(double));
            DataColumn fSColumn1 = new DataColumn("Al2O3(wt%)", typeof(double));
            DataColumn fSColumn3 = new DataColumn("FeO(wt%)", typeof(double));
            DataColumn fSColumn4 = new DataColumn("MnO(wt%)", typeof(double));
            DataColumn fSColumn5 = new DataColumn("MgO(wt%)", typeof(double));
            DataColumn fSColumn6 = new DataColumn("CaO(wt%)", typeof(double));
            DataColumn fSColumn7 = new DataColumn("Na2O(wt%)", typeof(double));
            DataColumn fSColumn8 = new DataColumn("K2O(wt%)", typeof(double));
            DataColumn fSColumn2 = new DataColumn("BaO(wt%)", typeof(double));
            DataColumn fSColumn21 = new DataColumn("Rb2O(wt%)", typeof(double));
            DataColumn fSColumn22 = new DataColumn("Cs2O(wt%)", typeof(double));
            DataColumn fSColumn23 = new DataColumn("ZnO(wt%)", typeof(double));
            DataColumn fSColumn9 = new DataColumn("F(wt%)", typeof(double));
            DataColumn fSColumn0 = new DataColumn("Cl(wt%)", typeof(double));
            DataColumn fSColumn24 = new DataColumn("Cr2O3(wt%)", typeof(double));
            DataColumn fSColumn25 = new DataColumn("NiO(wt%)", typeof(double));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(string));

            dataTable.Columns.Add(feSColumn);
            dataTable.Columns.Add(fSColumn);
            dataTable.Columns.Add(fSColumn1);
            dataTable.Columns.Add(fSColumn3);
            dataTable.Columns.Add(fSColumn4);
            dataTable.Columns.Add(fSColumn5);
            dataTable.Columns.Add(fSColumn6);
            dataTable.Columns.Add(fSColumn7);
            dataTable.Columns.Add(fSColumn8);
            dataTable.Columns.Add(fSColumn2);
            dataTable.Columns.Add(fSColumn21);
            dataTable.Columns.Add(fSColumn22);
            dataTable.Columns.Add(fSColumn23);
            dataTable.Columns.Add(fSColumn9);
            dataTable.Columns.Add(fSColumn0);
            dataTable.Columns.Add(fSColumn24);
            dataTable.Columns.Add(fSColumn25);
            dataTable.Columns.Add(temperatureCColumn);

            DataRow dataRow = dataTable.NewRow();
            dataRow[0] = 37.663;
            dataRow[1] = 0.045;
            dataRow[2] = 23.254;
            dataRow[3] = 12.791;
            dataRow[4] = 0.058;
            dataRow[5] = 0.032;
            dataRow[6] = 23.047;
            dataRow[7] = 0.024;
            dataRow[8] = 0;
            dataRow[9] = 0;
            dataRow[10] = 0;
            dataRow[11] = 0;
            dataRow[12] = 0;
            dataRow[13] = 0;
            dataRow[14] = 0;
            dataRow[15] = 0;
            dataRow[16] = 0;

            dataTable.Rows.Add(dataRow);

            ExcelData = dataTable;
        }

        // 初始化数据
        private void InitData()
        {
            //A_TiO2 = 0.8f;
            //A_SiO2 = 1.0f;
            //MPa = 300;
        }

        // ========================================计算温度
        private string CalculateTValue(double B219)
        {
            return (319 * B219 - 69).ToString();
        }

        // B199
        // B199 = （28 * （MgO/40.31） / B121） * B189
        // B121 = (sio2 *2/60.09) + (tio2 *2/79.9) + (al2o3 *3/101.96) + (cr2o3 *3/152) + 0 +
        //      (feo/71.85) + (mno/70.94) + (mgo/40.31) + (nio/74.708) + (zno/81.38)
        //          + (cao/56.08) + (na2o/61.98) + (k2o/94.22) + (bao/153.36) +
        //          (rb2o/186.936) + (f*0.5/19) + (cl*0.5/35.45);
        // B189 = 28/B188
        // B188 = ((28*B126/B121)*2) + ((28*b127/b121)*2) + ((28*b128/b121)*1.5) + ((28*b129/b121)*1.5) + (B167*1.5) + B168 + (28*b132/b121) +
        //         (28*b133/b121) + (28*b134/b121) + (28*b135/b121) + (28*b136/b121) + (28*b137/b121) + (28*b138/b121) + (28*b139/b121) + (28*b140/b121)
        //         + (28*b141/b121) + (28*b142/b121)

        // 新B188
        // B188 = ((28*(B30/60.09)/B121)*2) + ((28*(B31/79.9)/b121)*2) + ((28*(B32*2/101.96)/b121)*1.5) + ((28*(B33*2/152)/b121)*1.5) +
        //         (B167*1.5) + B168 + (28*(B36/70.94)/b121) + (28*(B37/40.31)/b121) + (28*(B38/74.708)/b121) + (28*(B39/81.38)/b121) +
        //         (28*(B40/56.08)/b121) + (28*(B41*2/61.98)/b121) + (28*(B42*2/94.22)/b121) + (28*(B43/153.36)/b121) + (28*(B44*2/186.936)/b121)
        //         + (28*(B45/19)/b121) + (28*(B46/35.45)/b121)

        // 新B188
        // B188 = ((28*(B7/60.09)/B121)*2) + ((28*(B8/79.9)/b121)*2) + ((28*(B9*2/101.96)/b121)*1.5) +
        //        ((28*(B22*2/152)/b121)*1.5) + (B167*1.5) + B168 + (28*(B11/70.94)/b121) +
        //        (28*(B12/40.31)/b121) + (28*(b23/74.708)/b121) + (28*(B19/81.38)/b121) +
        //         (28*(B13/56.08)/b121) + (28*(B14*2/61.98)/b121) + (28*(B15*2/94.22)/b121) +
        //         (28*(B16/153.36)/b121) + (28*(B17*2/186.936)/b121)
        //         + (28*(B20/19)/b121) + (28*(B21/35.45)/b121)

        // 新B188
        // B188 = ((28*(sio2/60.09)/B121)*2) + ((28*(tio2/79.9)/b121)*2) + ((28*(al2o3*2/101.96)/b121)*1.5) +
        //        ((28*(cr2o3*2/152)/b121)*1.5) + (B167*1.5) + B168 + (28*(mno/70.94)/b121) +
        //        (28*(mgo/40.31)/b121) + (28*(nio/74.708)/b121) + (28*(zno/81.38)/b121) +
        //         (28*(cao/56.08)/b121) + (28*(na2o*2/61.98)/b121) + (28*(k2o*2/94.22)/b121) +
        //         (28*(bao/153.36)/b121) + (28*(rb2o*2/186.936)/b121)
        //         + (28*(f/19)/b121) + (28*(cl/35.45)/b121)

        // b151 = =28*(feo/71.85)/B121
        // bsum  = ((28*(B7/60.09)/B121)) + ((28*(B8/79.9)/b121)) + ((28*(B9*2/101.96)/b121)) +
        //        ((28*(B22*2/152)/b121)) + 0 + (28*(feo/71.85)/b121) + (28*(B11/70.94)/b121) +
        //        (28*(B12/40.31)/b121) + (28*(b23/74.708)/b121) + (28*(B19/81.38)/b121) +
        //         (28*(B13/56.08)/b121) + (28*(B14*2/61.98)/b121) + (28*(B15*2/94.22)/b121) +
        //         (28*(B16/153.36)/b121) + (28*(B17*2/186.936)/b121)
        // b164 = 20 - bsum

        // b150 = 28*0/b121 = 0
        // b151 = 28*(feo/71.85)/b121

        //b166 = B151-B164

        // 计算 b168
        public double B168(double b151, double b165, double b166)
        {
            return (b165 > b151) ? 0 : b166;
        }

        // 计算 b165
        public double B165(double b164)
        {
            return (b164 < 0) ? 0 : b164;
        }

        // 计算 B167
        public double B167(double b151, double b165)
        {
            return (b165 > b151) ? b151 : b165;
        }

        // B215 = B84/2

        // 计算 B84
        public double B84(double b83)
        {
            return (b83 > 0) ? b83 : 0;
        }

        // 计算 B83
        public double B83(double B192, double al2o3, double b121)
        {
            double b82 = (28 * (al2o3 * 2 / 101.96) / b121);
            return (B192 + b82 > 8) ? (8 - B192) : b82;
        }

        // 计算 B192
        public double B192(double sio2, double b188, double B121)
        {
            return (((28 * (sio2 / 60.09) / B121) * 2) * (28/ b188)) / 2;
        }


        // B217 = B197/2 = (b168*(25/b188))/2
        // 计算 B219
        public double B219(double B84, double B217, double B199)
        {
            // 检查分母是否为零，避免除以零的错误
            if (B217 + B199 == 0)
            {
                throw new DivideByZeroException("B217 + B218 不能为零");
            }

            // 计算并返回结果
            return (B84/2) + 0.1 * (B217 / (B217 + B199 / 2));
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
                    double sio2 = Convert.ToDouble(row[0]);     
                    double tio2 = Convert.ToDouble(row[1]);     
                    double al2o3 = Convert.ToDouble(row[2]);    
                    double feo = Convert.ToDouble(row[3]);      
                    double mno = Convert.ToDouble(row[4]);      
                    double mgo = Convert.ToDouble(row[5]);      
                    double cao = Convert.ToDouble(row[6]);      
                    double na2o = Convert.ToDouble(row[7]);     
                    double k2o = Convert.ToDouble(row[8]);      
                    double bao = Convert.ToDouble(row[9]);      
                    double rb2o = Convert.ToDouble(row[10]);     
                    double cso = Convert.ToDouble(row[11]);     
                    double zno = Convert.ToDouble(row[12]);     
                    double f = Convert.ToDouble(row[13]);       
                    double cl = Convert.ToDouble(row[14]);      
                    double cr2o3 = Convert.ToDouble(row[15]);   
                    double nio = Convert.ToDouble(row[16]);

                    double b121 = (sio2 * 2 / 60.09) + (tio2 * 2 / 79.9) + (al2o3 * 3 / 101.96) + (cr2o3 * 3 / 152) + 0 +
                         (feo / 71.85) + (mno / 70.94) + (mgo / 40.31) + (nio / 74.708) + (zno / 81.38)
                             + (cao / 56.08) + (na2o / 61.98) + (k2o / 94.22) + (bao / 153.36) +
                             (rb2o / 186.936) + (f * 0.5 / 19) + (cl * 0.5 / 35.45);

                    double b151 = 28 * (feo / 71.85) / b121;

                    double bsum = ((28 * (sio2 / 60.09) / b121)) + ((28 * (tio2 / 79.9) / b121)) + ((28 * (al2o3 * 2 / 101.96) / b121)) +
                             ((28 * (cr2o3 * 2 / 152) / b121)) + (28 * (feo / 71.85) / b121) + (28 * (mno / 70.94) / b121) +
                             (28 * (mgo / 40.31) / b121) + (28 * (nio / 74.708) / b121) + (28 * (zno / 81.38) / b121) +
                              (28 * (cao / 56.08) / b121) + (28 * (na2o * 2 / 61.98) / b121) + (28 * (k2o * 2 / 94.22) / b121) +
                              (28 * (bao / 153.36) / b121) + (28 * (rb2o * 2 / 186.936) / b121);
                    double b164 = 20 - bsum;
                    double b166 = b151 - b164;



                    double b188 = ((28 * (sio2 / 60.09) / b121) * 2) + ((28 * (tio2 / 79.9) / b121) * 2) + ((28 * (al2o3 * 2 / 101.96) / b121) * 1.5) +
                           ((28 * (cr2o3 * 2 / 152) / b121) * 1.5) + (B167(b151,B165(b164)) * 1.5) + B168(b151, B165(b164),b166) + (28 * (mno / 70.94) / b121) +
                           (28 * (mgo / 40.31) / b121) + (28 * (nio / 74.708) / b121) + (28 * (zno / 81.38) / b121) +
                            (28 * (cao / 56.08) / b121) + (28 * (na2o * 2 / 61.98) / b121) + (28 * (k2o * 2 / 94.22) / b121) +
                            (28 * (bao / 153.36) / b121) + (28 * (rb2o * 2 / 186.936) / b121)
                            + (28 * (f / 19) / b121) + (28 * (cl / 35.45) / b121);

                    double b217 = (B168(b151, B165(b164),b166) * (28 / b188)) / 2;

                    double b189 = 28 / b188;

                    double b199 = (28 * (mgo / 40.31) / b121) *b189;

                    try
                    {
                        row[17] = CalculateTValue(B219(B84(B83(B192(sio2, b188, b121), al2o3, b121)), b217, b199));
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.Error(ex.Message);
                    }
                }

                StepIndex++;
            }
        }


        [RelayCommand]
        private void ReSet()
        {
            // 重置属性数据
            InitData();
            // 重置表格数据
            InitDataExcel();
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
