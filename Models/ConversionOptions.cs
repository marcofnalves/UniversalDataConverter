using System;
using System.Collections.Generic;
using System.Text;

namespace Universal_Data_Converter.Models
{
    public class ConversionOptions
    {
        // Arquivos
        public string InputFile { get; set; } = string.Empty;
        public string OutputFile { get; set; } = string.Empty;
        public string OutputFormat { get; set; } = "xlsx";

        // Leitura
        public int MaxRows { get; set; } = 200000;
        public int SampleSize { get; set; } = 500;
        public string Separator { get; set; } = "AUTO";
        public string Encoding { get; set; } = "utf-8";

        // Smart typing
        public bool UseSmartTypes { get; set; } = true;

        public bool RemoveEmptyColumns { get; set; } = false;

        // SQL
        public string SqlTableName { get; set; } = "dados";
        public string SqlDialect { get; set; } = "sqlite";

        // JSON
        public string JsonOrient { get; set; } = "records";
        public bool JsonIndent { get; set; } = true;
        public bool JsonPreserveTypes { get; set; } = true;

        // CSV
        public string CsvSeparator { get; set; } = ";";
        public bool CsvIncludeIndex { get; set; } = false;

        // Parquet
        public string ParquetCompression { get; set; } = "snappy";

        // Excel
        public string ExcelSheetName { get; set; } = "dados";
        public bool ExcelIncludeIndex { get; set; } = false;
        public bool ExcelInferTypes { get; set; } = true;

        // HTML
        public string HtmlTitle { get; set; } = "Dados Exportados";

        // Markdown
        public bool MarkdownIncludeIndex { get; set; } = false;
    }
}
