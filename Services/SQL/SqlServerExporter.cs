using System.Data;
using System.Text;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Services.Analysis;

namespace Universal_Data_Converter.Services.Sql
{
    public class SqlServerExporter : ISqlDialectExporter
    {
        public async Task ExportAsync(DataTable dt, ConversionOptions options)
        {
            var analyzer = new TableAnalyzer();
            var analysis = analyzer.Analyze(dt);

            using var writer = new StreamWriter(options.OutputFile, false, Encoding.UTF8);

            writer.WriteLine($"IF OBJECT_ID('{options.SqlTableName}', 'U') IS NOT NULL DROP TABLE [{options.SqlTableName}];");
            writer.WriteLine($"CREATE TABLE [{options.SqlTableName}] (");

            var columns = new List<string>();

            foreach (DataColumn col in dt.Columns)
            {
                var type = DetectSqlServerType(col);
                var nullable = analysis.RequiredColumns.Contains(col.ColumnName) ? "NOT NULL" : "NULL";

                columns.Add($"  [{col.ColumnName}] {type} {nullable}");
            }

            if (analysis.PrimaryKeyColumns.Any())
            {
                var pk = string.Join(", ", analysis.PrimaryKeyColumns.Select(c => $"[{c}]"));
                columns.Add($"  CONSTRAINT [PK_{options.SqlTableName}] PRIMARY KEY ({pk})");
            }

            writer.WriteLine(string.Join(",\n", columns));
            writer.WriteLine(");\n");

            await WriteInserts(writer, dt, options);
            await writer.FlushAsync();
        }

        private string DetectSqlServerType(DataColumn col)
        {
            if (col.DataType == typeof(int))
                return "INT";

            if (col.DataType == typeof(long))
                return "BIGINT";

            if (col.DataType == typeof(decimal) || col.DataType == typeof(double))
                return "DECIMAL(18,2)";

            if (col.DataType == typeof(DateTime))
                return "DATETIME2";

            if (col.DataType == typeof(bool))
                return "BIT";

            return "NVARCHAR(255)";
        }

        private async Task WriteInserts(StreamWriter writer, DataTable dt, ConversionOptions options)
        {
            var columns = dt.Columns.Cast<DataColumn>().ToList();
            var columnNames = string.Join(", ", columns.Select(c => $"[{c.ColumnName}]"));

            foreach (DataRow row in dt.Rows)
            {
                var values = columns.Select(c => FormatValue(row[c])).ToList();
                await writer.WriteLineAsync(
                    $"INSERT INTO [{options.SqlTableName}] ({columnNames}) VALUES ({string.Join(", ", values)});");
            }

            await writer.WriteLineAsync();
        }

        private string FormatValue(object value)
        {
            if (value == DBNull.Value || value == null)
                return "NULL";

            if (value is DateTime date)
                return $"'{date:yyyy-MM-dd HH:mm:ss}'";

            if (value is bool b)
                return b ? "1" : "0";

            if (value is int || value is long || value is double || value is decimal)
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

            return $"'{value.ToString().Replace("'", "''")}'";
        }
    }
}