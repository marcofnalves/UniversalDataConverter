using System;
using System.Data;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.OtherFormats
{
    public class ExcelExporter
    {
        public Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            // Headers
            for (int i = 0; i < dt.Columns.Count; i++)
                worksheet.Cell(1, i + 1).Value = dt.Columns[i].ColumnName;

            // Data
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    var obj = dt.Rows[r][c];
                    var cell = worksheet.Cell(r + 2, c + 1);

                    // Use XLCellValue.FromObject to safely convert null/DBNull and other types into XLCellValue.
                    // This avoids unboxing a possibly-null value (CS8605).
                    cell.Value = obj == DBNull.Value || obj == null
                        ? XLCellValue.FromObject(null)
                        : XLCellValue.FromObject(obj);
                }
            }

            workbook.SaveAs(options.OutputFile);

            return Task.CompletedTask;
        }
    }
}