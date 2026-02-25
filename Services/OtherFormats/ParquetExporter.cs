using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Universal_Data_Converter.Services
{
    public class ParquetExporter
    {
        public async Task ExportAsync(DataTable dt, string outputPath, string compression)
        {
            if (dt == null || dt.Columns.Count == 0)
                throw new Exception("No data to export.");

            var fields = new List<Field>();

            foreach (System.Data.DataColumn col in dt.Columns)
            {
                fields.Add(new DataField<string>(col.ColumnName));
            }

            // Fully qualify Schema to avoid CS0246 when 'Schema' is not resolved
            var schema = new Parquet.Schema.ParquetSchema(fields);

            using (Stream fileStream = File.Create(outputPath))
            using (var writer = await ParquetWriter.CreateAsync(schema, fileStream))
            {
                writer.CompressionMethod = GetCompression(compression);

                using (ParquetRowGroupWriter groupWriter = writer.CreateRowGroup())
                {
                    foreach (System.Data.DataColumn column in dt.Columns)
                    {
                        var field = (DataField<string>)schema.Fields
                            .First(f => f.Name == column.ColumnName);

                        var columnData = dt.Rows
                            .Cast<DataRow>()
                            .Select(r => r[column]?.ToString())
                            .ToArray();

                        var parquetColumn = new Parquet.Data.DataColumn(field, columnData);
                        await groupWriter.WriteColumnAsync(parquetColumn);
                    }
                }
            }
        }

        private CompressionMethod GetCompression(string compression)
        {
            return compression?.ToLower() switch
            {
                "gzip" => CompressionMethod.Gzip,
                "brotli" => CompressionMethod.Brotli,
                "snappy" => CompressionMethod.Snappy,
                _ => CompressionMethod.None
            };
        }
    }
}