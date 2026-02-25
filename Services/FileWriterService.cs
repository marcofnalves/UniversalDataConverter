using System.Data;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Services.Analysis;
using Universal_Data_Converter.Services.OtherFormats;
using Universal_Data_Converter.Services.Sql;

namespace Universal_Data_Converter.Services
{
    public class FileWriterService
    {
        private readonly SqlDialectFactory _sqlFactory;
        private readonly JsonExporter _jsonExporter;
        private readonly CsvExporter _csvExporter;
        private readonly HtmlExporter _htmlExporter;
        private readonly MarkdownExporter _markdownExporter;

        public FileWriterService()
        {
            _sqlFactory = new SqlDialectFactory();
            _jsonExporter = new JsonExporter();
            _csvExporter = new CsvExporter();
            _htmlExporter = new HtmlExporter();
            _markdownExporter = new MarkdownExporter();
        }

        public async Task ExportData(DataTable dt, ConversionOptions options, string inputFile)
        {
            switch (options.OutputFormat.ToLower())
            {
                case "sql":
                    var exporter = _sqlFactory.GetExporter(options.SqlDialect);
                    await exporter.ExportAsync(dt, options);
                    break;

                case "json":
                    await _jsonExporter.ExportAsync(dt, options);
                    break;

                case "csv":
                    await _csvExporter.ExportAsync(dt, options);
                    break;

                case "html":
                    await _htmlExporter.ExportAsync(dt, options);
                    break;

                case "md":
                    await _markdownExporter.ExportAsync(dt, options);
                    break;

                default:
                    await _csvExporter.ExportAsync(dt, options);
                    break;
            }
        }
    }
}