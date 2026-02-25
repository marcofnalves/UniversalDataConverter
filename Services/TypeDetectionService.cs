using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services
{
    public class TypeDetectionService
    {
        public List<DataTypeInfo> AnalyzeDataTypes(DataTable dt)
        {
            var results = new List<DataTypeInfo>();

            foreach (DataColumn col in dt.Columns)
            {
                var info = new DataTypeInfo
                {
                    ColumnName = col.ColumnName,
                    CurrentType = col.DataType.Name,
                    DetectedType = DetectColumnType(dt, col),
                    NonNullCount = dt.AsEnumerable().Count(r => !r.IsNull(col)),
                    NullCount = dt.Rows.Count - dt.AsEnumerable().Count(r => !r.IsNull(col)),
                    UniqueCount = dt.AsEnumerable()
                        .Select(r => r[col]?.ToString())
                        .Distinct()
                        .Count()
                };

                // Amostra de valores
                info.SampleValues = dt.AsEnumerable()
                    .Take(5)
                    .Select(r => r[col]?.ToString() ?? "")
                    .ToList();

                results.Add(info);
            }

            return results;
        }

        public DataTable ApplySmartTyping(DataTable sourceDt)
        {
            var newDt = new DataTable();

            foreach (DataColumn col in sourceDt.Columns)
            {
                var detectedType = DetectColumnType(sourceDt, col);
                var newCol = CreateTypedColumn(col.ColumnName, detectedType);
                newDt.Columns.Add(newCol);
            }

            // Transferir dados com conversão
            foreach (DataRow sourceRow in sourceDt.Rows)
            {
                var newRow = newDt.NewRow();
                int colIndex = 0;

                foreach (DataColumn col in sourceDt.Columns)
                {
                    var value = sourceRow[col];
                    var detectedType = DetectColumnType(sourceDt, col);

                    try
                    {
                        newRow[colIndex] = ConvertValue(value, detectedType);
                    }
                    catch
                    {
                        newRow[colIndex] = DBNull.Value;
                    }

                    colIndex++;
                }

                newDt.Rows.Add(newRow);
            }

            return newDt;
        }

        public DataTable SampleDataTable(DataTable source, int sampleSize)
        {
            if (source.Rows.Count <= sampleSize)
                return source;

            var sampled = source.Clone();
            var random = new Random(42);
            var indices = Enumerable.Range(0, source.Rows.Count)
                .OrderBy(x => random.Next())
                .Take(sampleSize)
                .ToList();

            foreach (var idx in indices)
            {
                sampled.ImportRow(source.Rows[idx]);
            }

            return sampled;
        }

        private string DetectColumnType(DataTable dt, DataColumn column)
        {
            var values = dt.AsEnumerable()
                .Select(r => r[column]?.ToString())
                .Where(v => !string.IsNullOrEmpty(v))
                .Take(1000)
                .ToList();

            if (values.Count == 0) return "string";

            // Teste numérico
            int numericCount = 0;
            int intCount = 0;
            foreach (var val in values)
            {
                if (long.TryParse(val, out _))
                {
                    numericCount++;
                    intCount++;
                }
                else if (double.TryParse(val, out _))
                {
                    numericCount++;
                }
            }

            if (numericCount > values.Count * 0.8)
            {
                return intCount == numericCount ? "int" : "double";
            }

            // Teste data
            var datePatterns = new[]
            {
                @"^\d{4}-\d{2}-\d{2}$",
                @"^\d{2}/\d{2}/\d{4}$",
                @"^\d{4}/\d{2}/\d{2}$",
                @"^\d{2}-\d{2}-\d{4}$"
            };

            int dateCount = 0;
            foreach (var val in values)
            {
                if (string.IsNullOrEmpty(val)) continue;

                foreach (var pattern in datePatterns)
                {
                    if (Regex.IsMatch(val, pattern))
                    {
                        if (DateTime.TryParse(val, out _))
                        {
                            dateCount++;
                            break;
                        }
                    }
                }
            }

            if (dateCount > values.Count * 0.8)
            {
                return "datetime";
            }

            // Teste booleano
            var boolValues = new HashSet<string> { "true", "false", "yes", "no", "1", "0", "sim", "não" };
            var uniqueValues = new HashSet<string>(values.Select(v => v?.ToLower() ?? ""));
            if (uniqueValues.IsSubsetOf(boolValues))
            {
                return "bool";
            }

            // Categoria se poucos valores únicos
            if (uniqueValues.Count < values.Count * 0.05 && uniqueValues.Count < 50)
            {
                return "category";
            }

            return "string";
        }

        private DataColumn CreateTypedColumn(string name, string type)
        {
            return type switch
            {
                "int" => new DataColumn(name, typeof(int)),
                "double" => new DataColumn(name, typeof(double)),
                "datetime" => new DataColumn(name, typeof(DateTime)),
                "bool" => new DataColumn(name, typeof(bool)),
                "category" => new DataColumn(name, typeof(string)),
                _ => new DataColumn(name, typeof(string))
            };
        }

        private object ConvertValue(object value, string targetType)
        {
            if (value == DBNull.Value) return DBNull.Value;

            var strValue = value.ToString();

            return targetType switch
            {
                "int" => int.TryParse(strValue, out var intVal) ? intVal : DBNull.Value,
                "double" => double.TryParse(strValue, out var dblVal) ? dblVal : DBNull.Value,
                "datetime" => DateTime.TryParse(strValue, out var dateVal) ? dateVal : DBNull.Value,
                "bool" => ParseBoolean(strValue),
                _ => strValue ?? ""
            };
        }

        private object ParseBoolean(string? value)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;

            var lower = value.ToLower().Trim();
            return lower switch
            {
                "true" or "yes" or "sim" or "1" => true,
                "false" or "no" or "não" or "0" => false,
                _ => DBNull.Value
            };
        }
    }
}