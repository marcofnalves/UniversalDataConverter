using Newtonsoft.Json;
using System.Data;
using System.Text;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.OtherFormats
{
    public class JsonExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col];
                    dict[col.ColumnName] = val == DBNull.Value ? null : val;
                }

                list.Add(dict);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = options.JsonIndent ? Formatting.Indented : Formatting.None
            };

            var json = JsonConvert.SerializeObject(list, settings);

            await File.WriteAllTextAsync(options.OutputFile, json, Encoding.UTF8);
        }
    }
}