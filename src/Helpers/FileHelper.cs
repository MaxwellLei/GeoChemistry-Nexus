using System;
using System.Data;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using Microsoft.Kiota.Serialization.Form;

public class FileHelper
{
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
}