using System.Data;
using System.Text;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.OtherFormats
{
    public class MarkdownExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            using var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8);

            await writer.WriteLineAsync($"# {options.HtmlTitle}\n");
            await writer.WriteLineAsync($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

            // Header
            await writer.WriteAsync("|");
            foreach (DataColumn col in dt.Columns)
                await writer.WriteAsync($" {col.ColumnName.Replace("|", "\\|")} |");

            await writer.WriteLineAsync();

            // Separator
            await writer.WriteAsync("|");
            foreach (DataColumn col in dt.Columns)
                await writer.WriteAsync(" --- |");

            await writer.WriteLineAsync();

            // Rows
            foreach (DataRow row in dt.Rows)
            {
                await writer.WriteAsync("|");
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col] == DBNull.Value ? "" : row[col].ToString();
                    await writer.WriteAsync($" {val?.Replace("|", "\\|")} |");
                }
                await writer.WriteLineAsync();
            }

            await writer.FlushAsync();
        }
    }
}