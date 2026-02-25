using System.Data;
using System.Text;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Services.Analysis;

namespace Universal_Data_Converter.Services.Sql
{
    public class MySqlExporter : ISqlDialectExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            var analyzer = new TableAnalyzer();
            var analysis = analyzer.Analyze(dt);

            using var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8);

            writer.WriteLine($"CREATE TABLE `{options.SqlTableName}` (");

            var columns = new List<string>();

            foreach (DataColumn col in dt.Columns)
            {
                var type = "VARCHAR(255)";
                var nullable = analysis.RequiredColumns.Contains(col.ColumnName) ? "NOT NULL" : "NULL";

                columns.Add($"  `{col.ColumnName}` {type} {nullable}");
            }

            if (analysis.PrimaryKeyColumns.Any())
            {
                var pk = string.Join(", ", analysis.PrimaryKeyColumns.Select(c => $"`{c}`"));
                columns.Add($"  PRIMARY KEY ({pk})");
            }

            writer.WriteLine(string.Join(",\n", columns));
            writer.WriteLine(");\n");

            await writer.FlushAsync();
        }
    }
}