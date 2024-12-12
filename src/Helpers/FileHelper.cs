using System;
using System.Data;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using HandyControl.Controls;
using Ookii.Dialogs.Wpf;

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

    private static DataTable? ReadExcelFile(string filePath)
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
                dataTable.Columns.Add(worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}");
            }

            // Read data
            for (int row = 2; row <= rowCount; row++)
            {
                DataRow dataRow = dataTable.NewRow();
                for (int col = 1; col <= colCount; col++)
                {
                    dataRow[col - 1] = worksheet.Cells[row, col].Value;
                }
                dataTable.Rows.Add(dataRow);
            }
        }

        return dataTable;
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

    //复制文件到目标路径
    public static bool CopyFile(string sourcePath, string targetPath)
    {
        try
        {
            System.IO.File.Copy(sourcePath, targetPath);
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
}