using System.Data;
using System.Text;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.OtherFormats
{
    public class HtmlExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            using var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8);

            await writer.WriteLineAsync("<!DOCTYPE html>");
            await writer.WriteLineAsync("<html><head>");
            await writer.WriteLineAsync("<meta charset='UTF-8'>");
            await writer.WriteLineAsync($"<title>{options.HtmlTitle}</title>");
            await writer.WriteLineAsync("<style>");
            await writer.WriteLineAsync("body{font-family:Arial;margin:20px}");
            await writer.WriteLineAsync("table{border-collapse:collapse;width:100%}");
            await writer.WriteLineAsync("th,td{border:1px solid #ddd;padding:6px}");
            await writer.WriteLineAsync("th{background:#333;color:#fff}");
            await writer.WriteLineAsync("tr:nth-child(even){background:#f2f2f2}");
            await writer.WriteLineAsync("</style>");
            await writer.WriteLineAsync("</head><body>");

            await writer.WriteLineAsync($"<h1>{options.HtmlTitle}</h1>");
            await writer.WriteLineAsync($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            await writer.WriteLineAsync("<table>");
            await writer.WriteLineAsync("<thead><tr>");

            foreach (DataColumn col in dt.Columns)
                await writer.WriteLineAsync($"<th>{System.Net.WebUtility.HtmlEncode(col.ColumnName)}</th>");

            await writer.WriteLineAsync("</tr></thead><tbody>");

            foreach (DataRow row in dt.Rows)
            {
                await writer.WriteLineAsync("<tr>");
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col] == DBNull.Value ? "" : row[col].ToString();
                    await writer.WriteLineAsync($"<td>{System.Net.WebUtility.HtmlEncode(val)}</td>");
                }
                await writer.WriteLineAsync("</tr>");
            }

            await writer.WriteLineAsync("</tbody></table>");
            await writer.WriteLineAsync("</body></html>");

            await writer.FlushAsync();
        }
    }
}