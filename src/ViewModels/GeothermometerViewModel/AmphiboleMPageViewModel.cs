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
    public partial class AmphiboleMPageViewModel: ObservableObject
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
        public AmphiboleMPageViewModel()
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
            DataColumn fSColumn2 = new DataColumn("Cr2O3(wt%)", typeof(double));
            DataColumn fSColumn3 = new DataColumn("FeO(wt%)", typeof(double));
            DataColumn fSColumn4 = new DataColumn("MnO(wt%)", typeof(double));
            DataColumn fSColumn5 = new DataColumn("MgO(wt%)", typeof(double));
            DataColumn fSColumn6 = new DataColumn("CaO(wt%)", typeof(double));
            DataColumn fSColumn7 = new DataColumn("Na2O(wt%)", typeof(double));
            DataColumn fSColumn8 = new DataColumn("K2O(wt%)", typeof(double));
            DataColumn fSColumn9 = new DataColumn("F(wt%)", typeof(double));
            DataColumn fSColumn0 = new DataColumn("Cl(wt%)", typeof(double));
            DataColumn temperatureCColumn = new DataColumn("T(℃)", typeof(string));

            dataTable.Columns.Add(feSColumn);
            dataTable.Columns.Add(fSColumn);
            dataTable.Columns.Add(fSColumn1);
            dataTable.Columns.Add(fSColumn2);
            dataTable.Columns.Add(fSColumn3);
            dataTable.Columns.Add(fSColumn4);
            dataTable.Columns.Add(fSColumn5);
            dataTable.Columns.Add(fSColumn6);
            dataTable.Columns.Add(fSColumn7);
            dataTable.Columns.Add(fSColumn8);
            dataTable.Columns.Add(fSColumn9);
            dataTable.Columns.Add(fSColumn0);
            dataTable.Columns.Add(temperatureCColumn);

            //DataRow dataRow = dataTable.NewRow();
            //dataRow[0] = 45.4737f;
            //dataRow[1] = 2.52873f;
            //dataRow[2] = 9.07045f;
            //dataRow[3] = 0.02634f;
            //dataRow[4] = 12.68237f;
            //dataRow[5] = 0.23527f;
            //dataRow[6] = 14.21359f;
            //dataRow[7] = 11.31522f;
            //dataRow[8] = 2.03819f;
            //dataRow[9] = 0.62045f;
            //dataRow[10] = 0.16709f;
            //dataRow[11] = 0.06827f;
            //dataTable.Rows.Add(dataRow);

            ExcelData = dataTable;
        }

        // 初始化数据
        private void InitData()
        {
            //A_TiO2 = 0.8f;
            //A_SiO2 = 1.0f;
            //MPa = 300;
        }

        private double B44(double sio2, double tio2, double al2o3, double cr2o3, double feo, double mno, double mgo, double cao, double na2o, double k2o)
        {
            var temp2 = Math.Round(sio2 * 0.532554423806671, 2) +
                        Math.Round(tio2 * 0.400485619164435, 2) +
                        Math.Round(al2o3 * 0.470738811898667, 2) +
                        Math.Round(cr2o3 * 0.315790096993096, 2) +
                        Math.Round(feo * 0.222684631016341, 2) +
                        Math.Round(mno * 0.225538153572889, 2) +
                        Math.Round(mgo * 0.396958118300913, 2) +
                        Math.Round(cao * 0.285293960305997, 2) +
                        Math.Round(na2o * 0.258135820197164, 2) +
                        Math.Round(k2o * 0.169849779712299, 2);
            var temp3 = 23 * 15.999;
            return (23 * 15.999 / temp2);
        }

        // ========================================计算温度
        private string CalculateTValue(double B6, double B164, string B112)
        {
            if (B6 == 0)
            {
                return " ";
            }
            else if (B164 == 0)
            {
                return "invalid";
            }
            else if (B112 == "invalid")
            {
                return "invalid";
            }
            else
            {
                double result = -151.487 * B164 + 2041;
                return result.ToString();
            }
        }

        // ==================================计算 B164
        private double B164(double B104, double B131, double B132, double B133, double B136, double B137, double B139, double B141,
                                       double B140, double B146, double B147, double B150, double B152)
        {
            if (B104 < 1.5)
            {
                return 0;
            }
            else
            {
                double result = B131 + B132 / 15 - B133 * 2 - B136 / 2 - B137 / 1.8 + B139 / 9 + B141 / 3.3 +
                                B140 / 26 + B146 / 5 + B147 / 1.3 - B150 / 15 + (1 - B152) / 2.3;
                return result;
            }
        }

        // 计算 B131
        private double B131(double B96)
        {
            return B96;
        }

        // 计算 B132
        private double B132(double B131, double B98)
        {
            double value = 8 - B131; // 计算 (8 - B131)
            return value < B98 ? value : B98; // 返回较小的值
        }

        // 计算 B133
        private double B133(double B131, double B132, double B97)
        {
            double result = 8 - B131 - B132;
            return result < B97 ? result : B97;
        }

        // 计算 B136
        private double B136(double B98, double B132)
        {
            return B98 - B132;
        }

        // 计算 B137
        private double B137(double B97, double B133)
        {
            return B97 - B133;
        }

        // 计算 B139
        private double B139(double B100)
        {
            return B100;
        }

        // 计算 B140
        private double B140(double B103)
        {
            return B103;
        }

        // 计算 B141
        private double B141(double B136, double B137, double B138, double B139, double B140, double B142, double B101)
        {
            // 计算 (5 - B136 - B137 - B138 - B139 - B140 - B142)
            double result = 5 - B136 - B137 - B138 - B139 - B140 - B142;

            // 如果结果小于 B101，返回结果；否则返回 B101
            return result < B101 ? result : B101;
        }

        // 计算 B146
        private double B146(double B104)
        {
            return B104;
        }

        // 计算 B147
        private double B147(double B145, double B146, double B105)
        {
            double result = 2 - B145 - B146;
            return result < B105 ? result : B105;
        }

        // 计算 B150
        private double B150(double B105, double B147)
        {
            return B105 - B147;
        }

        // 计算 B152
        private double B152(double B150, double B151)
        {
            return B150 + B151;
        }

        // 计算 B104
        private double B104(double B63, double B64, double B65, double B86, double B70)
        {
            double sum = B63 + B64 + B65;
            return sum < 8 ? B86 : B70;
        }

        // 计算 B63
        private double B63(double B45, double B62)
        {
            // 计算公式: (B45 * 13) / B62
            // 注意: 如果 B62 为 0，会抛出 DivideByZeroException
            if (B62 == 0)
            {
                throw new DivideByZeroException("B62 cannot be zero.");
            }
            return B45 * 13 / B62;
        }

        // 计算 B45
        private double B45(double B6, double B44)
        {
            // 计算公式: (B6 * B44) / T4
            var test = (B6 * B44) / 60.084;
            return (B6 * B44) / 60.084;
        }

        // 计算 B62
        private double B62(double B45, double B46, double B47, double B48, double B49, double B50, double B51)
        {
            // 计算公式: B45 + B46 + B47 + B48 + B49 + B50 + B51
            var test = Math.Round(B45, 2) +
               Math.Round(B46, 2) +
               Math.Round(B47, 2) +
               Math.Round(B48, 2) +
               Math.Round(B49, 2) +
               Math.Round(B50, 2) +
               Math.Round(B51, 2);
            return B45 + B46 + B47 + B48 + B49 + B50 + B51;
        }

        // 计算 B46
        private double B46(double B7, double B44)
        {
            return (B7 * B44) / 79.898;
        }

        // 计算 B47
        private double B47(double B8, double B44)
        {
            // 计算公式: (B8 * B44) / (T6 * 2)

            return B8 * B44 / 101.961 * 2;
        }

        // 计算 B48
        private double B48(double B9, double B44)
        {
            // 计算公式: (B9 * B44) / (T7 * 2)
            return B9 * B44 / 151.9902 * 2;
        }

        // 计算 B49
        private double B49(double B10, double B44)
        {
            // 计算公式: (B10 * B44) / T8
            return (B10 * B44) / 71.846;
        }

        // 计算 B50
        private double B50(double B11, double B44)
        {
            // 计算公式: (B11 * B44) / T9
            return (B11 * B44) / 70.937;
        }

        // 计算 B51
        private double B51(double B12, double B44)
        {
            // 计算公式: (B12 * B44) / T10
            return (B12 * B44) / 40.304;
        }

        // 计算 B64
        private double B64(double B46, double B62)
        {
            // 计算公式: (B46 * 13) / B62
            return (B46 * 13) / B62;
        }

        // 计算 B65
        private double B65(double B47, double B62)
        {
            // 计算公式: (B47 * 13) / B62
            return (B47 * 13) / B62;
        }

        // 计算 B86
        private double B86(double B52, double B78)
        {
            // 计算公式: (B52 * 15) / B78
            return (B52 * 15) / B78;
        }

        // 计算 B52
        private double B52(double B13, double B44)
        {
            // 计算公式: (B13 * B44) / T11
            return (B13 * B44) / 56.079;
        }

        // 计算 B53
        private double B53(double B14, double B44)
        {
            // 计算公式: (B14 * B44) / (T12 * 2)
            return B14 * B44 / 61.979 * 2;
        }

        // 计算 B54
        private double B54(double B15, double B44)
        {
            // 计算公式: (B15 * B44) / (T13 * 2)
            return B15 * B44 / 94.195 * 2;
        }

        // 计算 B78
        private double B78(
            double B45, double B46, double B47, double B48,
            double B49, double B50, double B51, double B52)
        {
            // 计算公式: B45 + B46 + B47 + B48 + B49 + B50 + B51 + B52
            return B45 + B46 + B47 + B48 + B49 + B50 + B51 + B52;
        }

        // 计算 B70
        private double B70(double B52, double B62)
        {
            // 计算公式: (B52 * 13) / B62
            return (B52 * 13) / B62;
        }

        // 计算 B96
        private double B96(double B63, double B64, double B65, double B79)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B79，否则返回 B63
            return (B63 + B64 + B65) < 8 ? B79 : B63;
        }

        // 计算 B79
        private double B79(double B45, double B78)
        {
            // 计算公式: (B45 * 15) / B78
            return (B45 * 15) / B78;
        }

        // 计算 B98
        private double B98(double B63, double B64, double B65, double B81)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B81，否则返回 B65
            return (B63 + B64 + B65) < 8 ? B81 : B65;
        }

        // 计算 B97
        private double B97(double B63, double B64, double B65, double B80)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B80，否则返回 B64
            return (B63 + B64 + B65) < 8 ? B80 : B64;
        }

        // 计算 B80
        private double B80(double B46, double B78)
        {
            // 计算公式: (B46 * 15) / B78
            return (B46 * 15) / B78;
        }

        // 计算 B81
        private double B81(double B47, double B78)
        {
            // 计算公式: (B47 * 15) / B78
            return (B47 * 15) / B78;
        }

        // 计算 B82
        private double B82(double B48, double B78)
        {
            // 计算公式: (B48 * 15) / B78
            // 注意: 如果 B78 为 0，会抛出 DivideByZeroException
            if (B78 == 0)
            {
                throw new DivideByZeroException("B78 cannot be zero.");
            }
            return (B48 * 15) / B78;
        }

        // 计算 B100
        private double B100(double B94)
        {
            // 计算公式: 如果 B94 > 46，返回 0，否则返回 46 - B94
            return B94 > 46 ? 0 : 46 - B94;
        }

        // 计算 B94
        private double B94(
            double B63, double B64, double B65, double B66, double B67, double B68, double B69, double B70, double B71, double B72,
            double B79, double B80, double B81, double B82, double B83, double B84, double B85, double B86, double B87, double B88)
        {
            // 计算公式: 如果 B70 < 1.5，返回加权和 1，否则返回加权和 2
            if (B70 < 1.5)
            {
                // 加权和 1: B79*4 + B80*4 + B81*3 + B82*3 + B83*2 + B84*2 + B85*2 + B86*2 + B87 + B88
                return B79 * 4 + B80 * 4 + B81 * 3 + B82 * 3 + B83 * 2 + B84 * 2 + B85 * 2 + B86 * 2 + B87 + B88;
            }
            else
            {
                // 加权和 2: B63*4 + B64*4 + B65*3 + B66*3 + B67*2 + B68*2 + B69*2 + B70*2 + B71 + B72
                return B63 * 4 + B64 * 4 + B65 * 3 + B66 * 3 + B67 * 2 + B68 * 2 + B69 * 2 + B70 * 2 + B71 + B72;
            }
        }

        // 计算 B66
        private double B66(double B48, double B62)
        {
            // 计算公式: (B48 * 13) / B62
            // 注意: 如果 B62 为 0，会抛出 DivideByZeroException
            if (B62 == 0)
            {
                throw new DivideByZeroException("B62 cannot be zero.");
            }
            return (B48 * 13) / B62;
        }

        // 计算 B67
        private double B67(double B49, double B62)
        {
            // 计算公式: (B49 * 13) / B62
            return (B49 * 13) / B62;
        }

        // 计算 B68
        private double B68(double B50, double B62)
        {
            // 计算公式: (B50 * 13) / B62
            return (B50 * 13) / B62;
        }

        // 计算 B69
        private double B69(double B51, double B62)
        {
            // 计算公式: (B51 * 13) / B62
            return (B51 * 13) / B62;
        }

        // 计算 B71
        private double B71(double B53, double B62)
        {
            // 计算公式: (B53 * 13) / B62
            // 注意: 如果 B62 为 0，会抛出 DivideByZeroException
            if (B62 == 0)
            {
                throw new DivideByZeroException("B62 cannot be zero.");
            }
            return (B53 * 13) / B62;
        }

        // 计算 B72
        private double B72(double B54, double B62)
        {
            // 计算公式: (B54 * 13) / B62
            return (B54 * 13) / B62;
        }

        // 计算 B83
        private double B83(double B49, double B78)
        {
            // 计算公式: (B49 * 15) / B78
            return (B49 * 15) / B78;
        }

        // 计算 B84
        private double B84(double B50, double B78)
        {
            // 计算公式: (B50 * 15) / B78
            return (B50 * 15) / B78;
        }

        // 计算 B85
        private double B85(double B51, double B78)
        {
            // 计算公式: (B51 * 15) / B78
            return (B51 * 15) / B78;
        }

        // 计算 B87
        private double B87(double B53, double B78)
        {
            // 计算公式: (B53 * 15) / B78
            return (B53 * 15) / B78;
        }

        // 计算 B88
        private double B88(double B54, double B78)
        {
            // 计算公式: (B54 * 15) / B78
            return (B54 * 15) / B78;
        }

        // 计算 B138
        private double B138(double B99)
        {
            // 直接返回 B99 的值
            return B99;
        }

        // 计算 B142
        private double B142(double B102)
        {
            // 直接返回 B102 的值
            return B102;
        }

        // 计算 B102
        private double B102(double B63, double B64, double B65, double B84, double B68)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B84，否则返回 B68
            return (B63 + B64 + B65) < 8 ? B84 : B68;
        }

        // 计算 B99
        private double B99(double B63, double B64, double B65, double B82, double B66)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B82，否则返回 B66
            return (B63 + B64 + B65) < 8 ? B82 : B66;
        }

        // 计算 B103
        private double B103(double B63, double B64, double B65, double B85, double B69)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B85，否则返回 B69
            return (B63 + B64 + B65) < 8 ? B85 : B69;
        }

        // 计算 B101
        private double B101(double B63, double B64, double B65, double B83, double B67, double B100)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B83 - B100，否则返回 B67 - B100
            return (B63 + B64 + B65) < 8 ? B83 - B100 : B67 - B100;
        }

        // 计算 B145
        private double B145(double B100, double B101, double B139, double B141)
        {
            // 计算公式: B100 + B101 - B139 - B141
            return B100 + B101 - B139 - B141;
        }

        // 计算 B105
        private double B105(double B63, double B64, double B65, double B87, double B71)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B87，否则返回 B71
            return (B63 + B64 + B65) < 8 ? B87 : B71;
        }

        // 计算 B151
        private double B151(double B106)
        {
            // 直接返回 B106 的值
            return B106;
        }

        // 计算 B106
        private double B106(double B63, double B64, double B65, double B88, double B72)
        {
            // 计算公式: 如果 (B63 + B64 + B65) < 8，返回 B88，否则返回 B72
            return (B63 + B64 + B65) < 8 ? B88 : B72;
        }

        // 计算 B112
        private string B112(double B6, double B164, string B110, double B111, double B131, double B152)
        {
            // 计算公式: 嵌套条件判断
            if (B6 == 0)
            {
                return " ";
            }
            else if (B164 == 0)
            {
                return "low-Ca";
            }
            else if (B110 == "wrong")
            {
                return "invalid";
            }
            else if (B110 == "low total")
            {
                return "invalid";
            }
            else if (B111 > 0.21)
            {
                return "Xenocryst";
            }
            else if (B131 >= 6.5)
            {
                return "Mg-Hbl";
            }
            else if (B152 > 0.5)
            {
                return "Mg-Hst";
            }
            else
            {
                return "Tsch-Prg";
            }
        }

        // 计算 B110
        private string B110(double B6, double B39, double B139, double B141, double B150, double B152, double B155)
        {
            // 计算公式: 嵌套条件判断
            if (B6 == 0)
            {
                return " ";
            }
            else if (B39 < 98)
            {
                return "wrong";
            }
            else if (B139 < 0)
            {
                return "wrong";
            }
            else if (B141 < 0)
            {
                return "wrong";
            }
            else if (B150 < 0)
            {
                return "wrong";
            }
            else if (B152 > 1)
            {
                return "wrong";
            }
            else if (B155 < 0.5)
            {
                return "wrong";
            }
            else
            {
                return "ok";
            }
        }

        // 计算 B39
        private double? B39(double B6, double B37, double B38)
        {
            // 计算公式: 如果 B6 为 0，返回 null，否则返回 B37 + B38
            if (B6 == 0)
            {
                return null; // 表示空值
            }
            else
            {
                return B37 + B38;
            }
        }

        // 计算 B37
        private double B37(double[] values)
        {
            // 计算公式: 对数组 values 中的所有元素求和
            double sum = 0;
            foreach (double value in values)
            {
                sum += value;
            }
            return sum;
        }

        // 计算 B27
        private double B27(double B6, double B100, double B62, double B44)
        {
            // 计算公式: 如果 B6 为 0，返回空格字符串，否则返回计算结果
            if (B6 == 0)
            {
                return double.NaN;
            }
            else
            {
                // 计算公式: B100 * B62 / 13 / B44 * T17 / 2
                // 注意: 如果 B44 为 0，会抛出 DivideByZeroException
                if (B44 == 0)
                {
                    throw new DivideByZeroException("B44 cannot be zero.");
                }
                double result = (B100 * B62) / 13 / B44 * 159.691 / 2;
                return result; // 将数值结果转换为字符串
            }
        }

        // 计算 B28
        private double B28(double B6, double B101, double B62, double B44)
        {
            // 计算公式: 如果 B6 为 0，返回空格字符串，否则返回计算结果
            if (B6 == 0)
            {
                return double.NaN;
            }
            else
            {
                // 计算公式: B101 * B62 / 13 / B44 * T8
                // 注意: 如果 B44 为 0，会抛出 DivideByZeroException
                if (B44 == 0)
                {
                    throw new DivideByZeroException("B44 cannot be zero.");
                }
                double result = (B101 * B62) / 13 / B44 * 71.846;
                return result; // 将数值结果转换为字符串
            }
        }

        // 计算 B21
        private double? B21(double B6, double B59, double B44)
        {
            // 计算公式: 如果 B6 为 0，返回 null，否则返回计算结果
            if (B6 == 0)
            {
                return null; // 表示空值
            }
            else
            {
                // 计算公式: B59 * 17 / B44 / 2
                // 注意: 如果 B44 为 0，会抛出 DivideByZeroException
                if (B44 == 0)
                {
                    throw new DivideByZeroException("B44 cannot be zero.");
                }
                return (B59 * 17) / B44 / 2;
            }
        }

        // 计算 B59
        private double B59(double B57, double B58)
        {
            // 计算公式: 2 - B57 - B58
            return 2 - B57 - B58;
        }

        // 计算 B57
        private double B57(double B16, double B44)
        {
            // 计算公式: (B16 * B44) / T14
            return (B16 * B44) / 18.998;
        }

        // 计算 B58
        private double B58(double B17, double B44)
        {
            // 计算公式: (B17 * B44) / T15
            return (B17 * B44) / 35.453;
        }

        // 计算 B41
        private double B41(double B6, double B16, double B17)
        {
            // 计算公式: 如果 B6 为 0，返回空格字符串，否则返回计算结果
            if (B6 == 0)
            {
                return double.NaN;
            }
            else
            {
                // 计算公式: -(B16 * 0.421070639014633 + B17 * 0.225636758525372)
                double result = -(B16 * 0.421070639014633 + B17 * 0.225636758525372);
                return result; // 将数值结果转换为字符串
            }
        }

        // 计算 B111
        private double B111(double B6, double B136, double B132)
        {
            // 计算公式: 如果 B6 为 0，返回空格字符串，否则返回计算结果
            if (B6 == 0)
            {
                return double.NaN;
            }
            else
            {
                // 计算公式: B136 / (B136 + B132)
                // 注意: 如果 B136 + B132 为 0，会抛出 DivideByZeroException
                if (B136 + B132 == 0)
                {
                    throw new DivideByZeroException("B136 + B132 cannot be zero.");
                }
                double result = B136 / (B136 + B132);
                return result; // 将数值结果转换为字符串
            }
        }

        // 计算 B155
        private double B155(double B103, double B141, double B145)
        {
            // 计算公式: B103 / (B103 + B141 + B145)
            return B103 / (B103 + B141 + B145);
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
                    double cr2o3 = Convert.ToDouble(row[3]);
                    double feo = Convert.ToDouble(row[4]);
                    double mno = Convert.ToDouble(row[5]);
                    double mgo = Convert.ToDouble(row[6]);
                    double cao = Convert.ToDouble(row[7]);
                    double na2o = Convert.ToDouble(row[8]);
                    double k2o = Convert.ToDouble(row[9]);
                    double f = Convert.ToDouble(row[10]);
                    double cl = Convert.ToDouble(row[11]);
                    double b44 = B44(sio2, tio2, al2o3, cr2o3, feo, mno, mgo, cao, na2o, k2o);
                    double b62 = B62(B45(sio2, b44), B46(tio2, b44), B47(al2o3, b44), B48(cr2o3, b44), B49(feo, b44), B50(mno, b44), B51(mgo, b44));
                    double b78 = B78(B45(sio2, b44), B46(tio2, b44), B47(al2o3, b44), B48(cr2o3, b44), B49(feo, b44), B50(mno, b44), B51(mgo, b44), B52(cao, b44));
                    double b131 = B131(B96(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B79(B45(sio2, b44), b78)));
                    double b94 = B94(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B66(B48(cr2o3, b44), b62),
                                               B67(B49(feo, b44), b62), B68(B50(mno, b44), b62), B69(B51(mgo, b44), b62), B70(B52(cao, b44), b62),
                                               B71(B53(na2o, b44), b62), B72(B54(k2o, b44), b62), B79(B45(sio2, b44), b78), B80(B46(tio2, b44), b78),
                                               B81(B47(al2o3, b44), b78), B82(B48(cr2o3, b44), b78), B83(B49(feo, b44), b78), B84(B50(mno, b44), b78),
                                               B85(B51(mgo, b44), b78), B86(B52(cao, b44), b78), B87(B53(na2o, b44), b78), B88(B54(k2o, b44), b78));
                    double b139 = B139(B100(b94));
                    double b98 = B98(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B81(B47(al2o3, b44), b78));
                    double b97 = B97(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B80(B46(tio2, b44), b78));
                    double b132 = B132(b131, b98);
                    double b103 = B103(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B85(B51(mgo, b44), b78), B69(B51(mgo, b44), b62));
                    double b140 = B140(b103);
                    double b104 = B104(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B86(B52(cao, b44), b78), B70(B52(cao, b44), b62));

                    double b136 = B136(b98, b132);
                    double b137 = B137(b97, B133(b131, b132, b97));
                    double b138 = B138(B99(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62),
                                          B65(B47(al2o3, b44), b62), B82(B48(cr2o3, b44), b78), B66(B48(cr2o3, b44), b62)));
                    double b142 = B142(B102(B63(B45(sio2, b44), b62),
                                          B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B84(B50(mno, b44), b78), B68(B50(mno, b44), b62)));
                    double b101 = B101(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B83(B49(feo, b44), b78), B67(B49(feo, b44), b62), B100(b94));

                    double b141 = B141(b136, b137, b138, b139, b140, b142, b101);
                    double b105 = B105(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B87(B53(na2o, b44), b78), B71(B53(na2o, b44), b62));
                    double b150 = B150(b105, B147(B145(B100(b94), b101, b139, b141), B146(b104), b105));
                    double b151 = B151(B106(B63(B45(sio2, b44), b62), B64(B46(tio2, b44), b62), B65(B47(al2o3, b44), b62), B88(B54(k2o, b44), b78), B72(B54(k2o, b44), b62)));
                    double b164 = B164(b104, b131, b132, B133(b131, b132, b97), b136, b137, b139, b141, b140, B146(b104),
                                      B147(B145(B100(b94), b101, b139, b141), B146(b104), b105), b150, B152(b150, b151));

                    var temp = B27(sio2, B100(b94), b62, b44);
                    double[] values = new double[] { sio2, tio2, al2o3, cr2o3, temp, B28(sio2, b101, b62, b44), mno, mgo, cao, na2o, k2o, f, cl, (double)B21(sio2, B59(B57(f, b44), B58(cl, b44)), b44) };

                    try
                    {
                        row[12] = CalculateTValue(sio2, b164, B112(sio2, b164, B110(sio2, (double)B39(sio2, B37(values), B41(sio2, f, cl)), b139, b141, b150, B152(b150, b151), B155(b103, b141, B145(B100(b94), b101, b139, b141))),
                            B111(sio2, b136, b132), b131, B152(b150, B151(B106(B63(B45(sio2, b44), b62),
                                      B64(B46(al2o3, b44), b62), B65(B47(al2o3, b44), b62), B88(B54(k2o, b44), b78), B72(B54(k2o, b44), b62))))));
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
