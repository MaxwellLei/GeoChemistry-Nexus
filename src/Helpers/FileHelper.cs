using System;
using System.Data;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using HandyControl.Controls;
using Ookii.Dialogs.Wpf;
using System.Linq;
using System.Windows;
using GeoChemistryNexus.Helpers;

public class FileHelper
{
    // 读取 数据 文件
    public static DataTable? ReadFile()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls|CSV Files|*.csv",
            Title = "Select a file"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            string fileExtension = Path.GetExtension(filePath).ToLower();

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return null; // 空文件
            }

            if (fileExtension == ".xlsx" || fileExtension == ".xls")
            {
                return ReadExcelFile(filePath);
            }
            else if (fileExtension == ".csv")
            {
                return ReadCsvFile(filePath);
            }
            else
            {
                throw new NotSupportedException("Unsupported file format.");
            }
        }

        return null;
    }

    // 读取 数据 文件
    public static DataTable? ReadFile(string filePath)
    {
        string fileExtension = Path.GetExtension(filePath).ToLower();

        FileInfo fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            return null; // 空文件
        }

        if (fileExtension == ".xlsx" || fileExtension == ".xls")
        {
            return ReadExcelFile(filePath);
        }
        else if (fileExtension == ".csv")
        {
            return ReadCsvFile(filePath);
        }
        else
        {
            throw new NotSupportedException("Unsupported file format.");
        }
    }

    private static DataTable? ReadExcelFile(string filePath)
    {
        try
        {
            DataTable dataTable = new DataTable();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                {
                    return null;
                }
                int rowCount = worksheet.Dimension.Rows;
                int colCount = worksheet.Dimension.Columns;

                // Read header
                for (int col = 1; col <= colCount; col++)
                {
                    //dataTable.Columns.Add(worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}");
                    var columnName = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                    if (!dataTable.Columns.Contains(columnName))
                    {
                        dataTable.Columns.Add(columnName);
                    }
                    else
                    {
                        // 处理列名重复，添加序号后缀
                        int suffix = 1;
                        string newColumnName = columnName + "_" + suffix;
                        while (dataTable.Columns.Contains(newColumnName))
                        {
                            suffix++;
                            newColumnName = columnName + "_" + suffix;
                        }
                        dataTable.Columns.Add(newColumnName);
                    }
                }

                // Read data
                for (int row = 2; row <= rowCount; row++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int col = 1; col <= colCount; col++)
                    {
                        dataRow[col - 1] = worksheet.Cells[row, col].Value;
                        var test = worksheet.Cells[row, col].Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }
        catch
        {
            MessageHelper.Error("文件不合法，请检查文件");
            return null;
        }

    }

    private static DataTable ReadCsvFile(string filePath)
    {
        DataTable dataTable = new DataTable();

        using (var reader = new StreamReader(filePath))
        {
            string[] headers = reader.ReadLine().Split(',');
            foreach (string header in headers)
            {
                dataTable.Columns.Add(header);
            }

            while (!reader.EndOfStream)
            {
                string[] rows = reader.ReadLine().Split(',');
                DataRow dataRow = dataTable.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dataRow[i] = rows[i];
                }
                dataTable.Rows.Add(dataRow);
            }
        }

        return dataTable;
    }

    // 导出 DataTable 到指定格式的文件
    public static bool ExportDataTable(DataTable dataTable)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx;*.xls|CSV Files|*.csv",
            Title = "Save DataTable as File",
            FileName = "ExportedData" // 默认文件名
        };

        // 显示保存文件对话框
        if (saveFileDialog.ShowDialog() == true)
        {
            string filePath = saveFileDialog.FileName;
            string fileExtension = Path.GetExtension(filePath).ToLower();

            try
            {
                if (fileExtension == ".xlsx" || fileExtension == ".xls")
                {
                    ExportToExcel(dataTable, filePath);
                }
                else if (fileExtension == ".csv")
                {
                    ExportToCsv(dataTable, filePath);
                }
                else
                {
                    throw new NotSupportedException("Unsupported file format.");
                }
                MessageHelper.Success("导出成功");
                return true;
            }
            catch (Exception ex)
            {
                MessageHelper.Error($"Error exporting DataTable: {ex.Message}");
                return false;
            }
        }

        return false; // 用户取消了保存对话框
    }

    private static void ExportToExcel(DataTable dataTable, string filePath)
    {
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // Load the DataTable into the worksheet
            worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);

            // Save the workbook to the specified file
            package.SaveAs(new FileInfo(filePath));
        }
    }

    private static void ExportToCsv(DataTable dataTable, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            // Write the header line
            writer.WriteLine(string.Join(",", dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName)));

            // Write each data row
            foreach (DataRow row in dataTable.Rows)
            {
                writer.WriteLine(string.Join(",", row.ItemArray));
            }
        }
    }

    // 获取保存文件路径  —— 不带文件过滤器
    public static string GetSaveFilePath(string defaultFileName, string initialDirectory = null)
    {
        // 创建文件保存对话框的实例
        var dialog = new VistaSaveFileDialog
        {
            FileName = defaultFileName, // 设置默认文件名
            Filter = "All Files (*.*)|*.*", // 文件类型过滤器
            Title = "保存文件" // 对话框标题
        };

        // 如果提供了初始目录，则设置它
        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        // 显示对话框并检查结果
        bool? result = dialog.ShowDialog();

        // 如果用户点击了“保存”按钮，返回完整的文件路径
        if (result == true)
        {
            return dialog.FileName;
        }

        // 用户取消操作，返回一个空字符串或其他指示性值
        return string.Empty;
    }

    //获取文件路径——不带格式限制
    public static string GetFilePath()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            return openFileDialog.FileName;     //用户正确选择了路径
        }
        else
        {
            return null;    //用户直接关闭了窗口
        }
    }

    //获取文件路径——带有格式限制
    public static string GetFilePath(string filter)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = filter;
        if (openFileDialog.ShowDialog() == true)
        {
            return openFileDialog.FileName;     //用户正确选择了路径
        }
        else
        {
            return null;    //用户直接关闭了窗口
        }
    }

    /// <summary>
    /// 获取文件保存路径 —— 带文件过滤器
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="filter">文件类型过滤器</param>
    /// <param name="defaultExt">默认文件扩展名</param>
    /// <returns>选择的文件保存路径，如果用户取消则返回null</returns>
    public static string GetSaveFilePath2(string title = "保存文件", string filter = "所有文件|*.*", string defaultExt = "", string defaultFileName = "")
    {
        try
        {
            var dialog = new VistaSaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                AddExtension = true,
                FileName = defaultFileName
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                return dialog.FileName;
            }

            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    //打开文件所在路径
    public static bool Openxplorer(string path)
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //删除指定路径文件
    public static bool DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            Growl.Success("删除成功");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //检查文件是否被占用
    public static bool IsFileInUse(string path)
    {
        try
        {
            System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            fs.Close();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    //解除文件占用
    public static bool ReleaseFile(string path)
    {
        try
        {
            System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            fs.Close();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //检查文件是否存在
    public static bool IsFileExist(string path)
    {
        return System.IO.File.Exists(path);
    }

    //检查文件是否存在，如果不存在则创建
    public static bool CreateFile(string path)
    {
        try
        {
            if (!IsFileExist(path))
            {
                System.IO.File.Create(path);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //检查文件夹是否存在
    public static bool IsFolderExist(string path)
    {
        return System.IO.Directory.Exists(path);
    }

    //如果文件夹不存在则创建文件夹
    public static bool CreateFolder(string path)
    {
        try
        {
            if (!IsFolderExist(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    //获取当前运行 程序所在路径
    public static string GetAppPath()
    {
        return System.AppDomain.CurrentDomain.BaseDirectory;
    }

    //判断文件夹是否为空
    public static bool IsFolderEmpty(string path)
    {
        return Directory.GetFiles(path).Length == 0;
    }

    //获取文件夹路径
    public static string GetFolderPath()
    {
        // 创建一个新的 VistaFolderBrowserDialog 对象
        var dialog = new VistaFolderBrowserDialog
        {
            Description = "请选择一个文件夹", // 对话框的描述
            UseDescriptionForTitle = true    // 使用描述作为窗口标题
        };

        // 显示对话框，并判断用户是否点击了“确定”按钮
        if (dialog.ShowDialog() == true)
        {
            // 返回用户选择的文件夹路径
            return dialog.SelectedPath;
        }

        // 如果用户取消了选择，返回空字符串或其他默认值
        return null;
    }

    // 复制文件到目标路径
    public static bool CopyFile(string sourcePath, string targetDir)
    {
        try
        {
            // 检查源文件是否存在
            if (!File.Exists(sourcePath))
            {
                Console.WriteLine($"错误：源文件 {sourcePath} 不存在");
                return false;
            }

            // 确保目标目录存在
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 获取源文件的文件名，并构造目标文件的完整路径
            string fileName = Path.GetFileName(sourcePath);
            string targetPath = Path.Combine(targetDir, fileName);

            // 执行文件复制
            File.Copy(sourcePath, targetPath, true); // true表示如果目标文件存在则覆盖
            Console.WriteLine($"文件已成功从 {sourcePath} 复制到 {targetPath}");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("错误：权限不足，无法复制文件");
            return false;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"复制文件时发生IO错误: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"复制文件时发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取指定文件路径的文件名称（包括扩展名）。
    /// </summary>
    /// <param name="filePath">文件的完整路径。</param>
    /// <returns>文件名称（包含扩展名）。</returns>
    /// <exception cref="ArgumentException">当文件路径为空或为 null 时抛出。</exception>
    public static string GetFileName(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        return Path.GetFileName(filePath);
    }
}