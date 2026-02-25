using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using Newtonsoft.Json;
using Universal_Data_Converter.Models;
using Universal_Data_Converter.Utils;

namespace Universal_Data_Converter.Services
{
    public class FileReaderService
    {
        public FileReaderService()
        {
            // Registrar encoding para ExcelDataReader
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public DataTable? LoadSample(ConversionOptions options)
        {
            if (string.IsNullOrEmpty(options.InputFile))
                return null;

            var encoding = EncodingHelper.GetEncoding(options.Encoding);

            try
            {
                if (options.InputFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadJsonSample(options.InputFile, options.SampleSize, encoding);
                }
                else if (options.InputFile.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                         options.InputFile.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadExcelSample(options.InputFile, options.SampleSize);
                }
                else if (options.InputFile.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                {
                    // Implementação simplificada - Parquet exigiria biblioteca específica
                    return new DataTable();
                }
                else
                {
                    return LoadCsvSample(options, encoding);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading data: {ex.Message}", ex);
            }
        }

        public DataTable? LoadFullData(ConversionOptions options)
        {
            if (string.IsNullOrEmpty(options.InputFile))
                return null;

            var encoding = EncodingHelper.GetEncoding(options.Encoding);

            try
            {
                if (options.InputFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadJsonFull(options.InputFile, encoding);
                }
                else if (options.InputFile.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                         options.InputFile.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadExcelFull(options.InputFile);
                }
                else if (options.InputFile.EndsWith(".parquet", StringComparison.OrdinalIgnoreCase))
                {
                    return new DataTable();
                }
                else
                {
                    return LoadCsvFull(options, encoding);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading data: {ex.Message}", ex);
            }
        }

        private DataTable? LoadJsonSample(string filePath, int sampleSize, Encoding encoding)
        {
            var json = File.ReadAllText(filePath, encoding);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return ConvertJsonToDataTable(data?.Take(sampleSize).ToList());
        }

        private DataTable? LoadJsonFull(string filePath, Encoding encoding)
        {
            var json = File.ReadAllText(filePath, encoding);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return ConvertJsonToDataTable(data);
        }

        private DataTable ConvertJsonToDataTable(List<Dictionary<string, object>>? data)
        {
            var dt = new DataTable();
            if (data == null || data.Count == 0) return dt;

            // Criar colunas baseado no primeiro item
            foreach (var key in data[0].Keys)
            {
                dt.Columns.Add(key, typeof(string));
            }

            // Adicionar dados
            foreach (var item in data)
            {
                var row = dt.NewRow();
                foreach (var kvp in item)
                {
                    row[kvp.Key] = kvp.Value?.ToString() ?? "";
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        private DataTable? LoadExcelSample(string filePath, int sampleSize)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    UseColumnDataType = true,
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count > 0)
                {
                    var dt = result.Tables[0];
                    if (dt.Rows.Count > sampleSize)
                    {
                        var sampledDt = dt.Clone();
                        for (int i = 0; i < sampleSize; i++)
                        {
                            sampledDt.ImportRow(dt.Rows[i]);
                        }
                        return sampledDt;
                    }
                    return dt;
                }
            }

            return new DataTable();
        }

        private DataTable? LoadExcelFull(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    UseColumnDataType = true,
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                if (result.Tables.Count > 0)
                {
                    return result.Tables[0];
                }
            }

            return new DataTable();
        }

        private DataTable? LoadCsvSample(ConversionOptions options, Encoding encoding)
        {
            var sep = GetEffectiveSeparator(options.Separator);
            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = sep,
                Encoding = encoding,
                HasHeaderRecord = true,
                DetectDelimiter = options.Separator == "AUTO",
                DetectDelimiterValues = new[] { ",", ";", "\t", "|" }
            };

            using (var reader = new StreamReader(options.InputFile, encoding))
            using (var csv = new CsvReader(reader, config))
            {
                using (var dr = new CsvDataReader(csv))
                {
                    var dt = new DataTable();
                    dt.Load(dr);

                    if (dt.Rows.Count > options.SampleSize)
                    {
                        var sampledDt = dt.Clone();
                        for (int i = 0; i < options.SampleSize; i++)
                        {
                            sampledDt.ImportRow(dt.Rows[i]);
                        }
                        return sampledDt;
                    }
                    return dt;
                }
            }
        }

        private DataTable? LoadCsvFull(ConversionOptions options, Encoding encoding)
        {
            var sep = GetEffectiveSeparator(options.Separator);
            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = sep,
                Encoding = encoding,
                HasHeaderRecord = true,
                DetectDelimiter = options.Separator == "AUTO",
                DetectDelimiterValues = new[] { ",", ";", "\t", "|" }
            };

            using (var reader = new StreamReader(options.InputFile, encoding))
            using (var csv = new CsvReader(reader, config))
            {
                using (var dr = new CsvDataReader(csv))
                {
                    var dt = new DataTable();
                    dt.Load(dr);
                    return dt;
                }
            }
        }

        private string GetEffectiveSeparator(string separator)
        {
            return separator switch
            {
                "Tab" => "\t",
                "AUTO" => ",", // será detectado automaticamente
                _ => separator
            };
        }
    }
}