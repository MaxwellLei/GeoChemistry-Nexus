using GeoChemistryNexus.Helpers;
using GeoChemistryNexus.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GeoChemistryNexus.ViewModels.GeothermometerViewModel;
using System.Data;

namespace GeoChemistryNexus.Views.Geothermometer
{
    /// <summary>
    ///  .xaml 的交互逻辑
    /// </summary>
    public partial class ArsenopyritePageView : Page
    {
        private static ArsenopyritePageView commonPage;

        private static ArsenopyritePageViewModel viewModel;

        public ArsenopyritePageView(string tempType)
        {
            InitializeComponent();
            viewModel = new ArsenopyritePageViewModel();
            this.DataContext = viewModel;
        }

        public static Page GetPage(string tempType)
        {
            if (commonPage == null)
            {
                commonPage = new ArsenopyritePageView(tempType);
            }

            if (tempType == "biotite")
            {
                ((ArsenopyritePageViewModel)commonPage.DataContext).TitleX = I18n.GetString("BiotiteGTM");
            }
            else
            {
                ((ArsenopyritePageViewModel)commonPage.DataContext).TitleX = I18n.GetString("ArsenopyriteGTM");
            }
            return commonPage;
        }

        // 拖拽文件
        private void Border_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            var test = System.Windows.DataFormats.FileDrop;
            // 检查拖入的对象是否是文件
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy; // 设置拖拽效果
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None; // 禁止拖拽
            }
        }

        // 拖拽文件
        private void Border_Drop(object sender, System.Windows.DragEventArgs e)
        {
            viewModel.StepIndex = 0;
            // 获取拖拽的文件路径
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    // 读取第一个文件路径
                    string filePath = files[0];
                    // 检查文件扩展名
                    string extension = System.IO.Path.GetExtension(filePath).ToLower();
                    if (extension == ".xlsx" || extension == ".xls" || extension == ".csv")
                    {
                        MessageHelper.Success($"读取文件成功");
                        if (CopyTiO2Values(FileHelper.ReadFile(filePath)))
                        {
                            viewModel.StepIndex += 1;
                        }
                    }
                    else
                    {
                        MessageHelper.Warning($"文件 {filePath} 不是有效的 Excel 或 CSV 文件");
                    }
                }
            }
        }

        // 点击导入文件
        private void Click_Import_File(object sender, RoutedEventArgs e)
        {
            viewModel.StepIndex = 0;
            // 获取拖拽的文件路径
            var tempData = FileHelper.ReadFile();
            if (tempData != null)
            {
                if (CopyTiO2Values(tempData))
                {
                    MessageHelper.Success($"读取文件成功");
                    viewModel.StepIndex += 1;
                }
            }
        }

        // 读取 TiO2 的值 
        private bool CopyTiO2Values(DataTable sourceDataTable)
        {
            if(sourceDataTable == null)
            {
                MessageHelper.Error("读取的数据为空，请检查数据");
                return false;
            }

            // 定义需要检查和复制的列标题
            Dictionary<string, string> columnNames;
            
            if(((ArsenopyritePageViewModel)commonPage.DataContext).TitleX == I18n.GetString("BiotiteGTM"))
            {
                columnNames = new Dictionary<string, string>
                {
                    { "Arsenopyrit", "Arsenopyrit" },
                };
            }
            else
            {
                columnNames = new Dictionary<string, string>
                {
                    { "Ti", "Ti" },
                    { "Mg", "Mg" },
                    { "Fe", "Fe" },
                };
            }

            // 检查所有列标题是否存在
            foreach (var column in columnNames)
            {
                if (!sourceDataTable.Columns.Contains(column.Value))
                {
                    MessageHelper.Error($"源表格不包含标题名称为 {column.Value} 的列");
                    return false;
                }
            }

            // 检查ExcelData的行数，如果不够，就扩展行
            while (sourceDataTable.Rows.Count > viewModel.ExcelData.Rows.Count)
            {
                var newRow = viewModel.ExcelData.NewRow();
                viewModel.ExcelData.Rows.Add(newRow);
            }

            // 复制每个元素列的值
            foreach (var column in columnNames)
            {
                for (int i = 0; i < sourceDataTable.Rows.Count; i++)
                {
                    float value = Convert.ToSingle(sourceDataTable.Rows[i][column.Value]);
                    viewModel.ExcelData.Rows[i][column.Key] = value;
                }
            }

            return true;
        }

        // 弃用的方法
        // 更改表格数据
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            viewModel.StepIndex = 1;
        }
    }
}
