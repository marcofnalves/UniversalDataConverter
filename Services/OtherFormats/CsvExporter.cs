using CsvHelper;
using CsvHelper.Configuration;
using System.Data;
using System.Globalization;
using System.Text;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.OtherFormats
{
    public class CsvExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            var separator = options.CsvSeparator == "Tab" ? "\t" : options.CsvSeparator;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = separator,
                Encoding = Encoding.UTF8,
                HasHeaderRecord = true
            };

            using var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8);
            using var csv = new CsvWriter(writer, config);

            // Header
            foreach (DataColumn col in dt.Columns)
                csv.WriteField(col.ColumnName);

            csv.NextRecord();

            // Rows
            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col];
                    csv.WriteField(val == DBNull.Value ? null : val.ToString());
                }

                csv.NextRecord();
            }

            await writer.FlushAsync();
        }
    }
}