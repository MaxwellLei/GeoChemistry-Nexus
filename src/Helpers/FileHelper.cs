using System;
using System.Data;
using System.IO;
using Microsoft.Win32;
using HandyControl.Controls;
using Ookii.Dialogs.Wpf;
using System.Linq;
using System.Windows;
using GeoChemistryNexus.Helpers;
using System.Windows.Media.Imaging;
using System.Windows.Media;

public class FileHelper
{
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

    /// <summary>
    /// 查找文件夹返回文件，查找不到则第一个同类型文件
    /// </summary>
    /// <param name="folderPath">查找文件的文件夹</param>
    /// <param name="fileName">文件名称</param>
    /// <param name="fileExtension">文件后缀</param>
    /// <param name="includeSubfolders">子文件查找</param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="Exception"></exception>
    public static string FindFileOrGetFirstWithExtension(string folderPath, string fileName, string fileExtension, bool includeSubfolders = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ArgumentException("参数不能为空或null");
            }

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");
            }

            if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }

            // 设置搜索选项
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 构建完整的目标文件路径
            string targetFilePath = Path.Combine(folderPath, fileName + fileExtension);

            // 检查目标文件是否存在
            if (File.Exists(targetFilePath))
            {
                return targetFilePath;
            }

            // 如果目标文件不存在，查找第一个具有相同扩展名的文件
            string[] filesWithExtension = Directory.GetFiles(folderPath, "*" + fileExtension, searchOption);

            if (filesWithExtension.Length > 0)
            {
                return filesWithExtension[0];
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException($"没有权限访问文件夹: {folderPath}");
        }
        catch (Exception ex)
        {
            throw new Exception($"搜索文件时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 无锁加载图片 + 尺寸优化 + (可选)灰度优化
    /// </summary>
    /// <param name="path">图片路径</param>
    /// <param name="decodeWidth">解码宽度，默认400</param>
    /// <param name="toGray">是否转为灰度图以极致节省内存</param>
    public static BitmapSource LoadBitmapNoLock(string path, int decodeWidth = 400, bool toGray = true)
    {
        if (!File.Exists(path)) return null;

        try
        {
            // 按尺寸优化加载
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = decodeWidth;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();

            if (toGray)
            {
                // 如果需要灰度，进行格式转换
                var grayBitmap = new FormatConvertedBitmap();
                grayBitmap.BeginInit();
                grayBitmap.Source = bitmap; // 源
                grayBitmap.DestinationFormat = PixelFormats.Gray8; // 转换为 8位灰度
                grayBitmap.EndInit();

                grayBitmap.Freeze(); // 冻结灰度图
                return grayBitmap;
            }
            else
            {
                bitmap.Freeze();
                return bitmap;
            }
        }
        catch
        {
            return null;
        }
    }
}