using System.Data;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services
{
    public class DataCleanerService
    {
        /// <summary>
        /// Remove colunas que estão completamente vazias (todos os valores são NULL ou vazios)
        /// </summary>
        public DataTable RemoveEmptyColumns(DataTable dt, ConversionOptions options, out List<string> removedColumns)
        {
            removedColumns = new List<string>();

            if (!options.RemoveEmptyColumns || dt == null || dt.Rows.Count == 0)
                return dt;

            var columnsToRemove = new List<DataColumn>();

            foreach (DataColumn col in dt.Columns)
            {
                if (IsColumnCompletelyEmpty(dt, col))
                {
                    columnsToRemove.Add(col);
                    removedColumns.Add(col.ColumnName);
                }
            }

            foreach (var col in columnsToRemove)
            {
                dt.Columns.Remove(col);
            }

            return dt;
        }

        /// <summary>
        /// Versão sem parâmetro out (para uso simples)
        /// </summary>
        public DataTable RemoveEmptyColumns(DataTable dt, ConversionOptions options)
        {
            return RemoveEmptyColumns(dt, options, out _);
        }

        /// <summary>
        /// Verifica se uma coluna está completamente vazia
        /// </summary>
        private bool IsColumnCompletelyEmpty(DataTable dt, DataColumn column)
        {
            foreach (DataRow row in dt.Rows)
            {
                var value = row[column];

                // Se encontrar qualquer valor não nulo/não vazio, a coluna NÃO é vazia
                if (value != null && value != DBNull.Value)
                {
                    string? strValue = value.ToString();
                    if (!string.IsNullOrWhiteSpace(strValue))
                    {
                        return false; // Encontrou um valor válido
                    }
                }
            }

            // Se chegou aqui, todos os valores são nulos ou vazios
            return true;
        }

        /// <summary>
        /// Obtém estatísticas sobre valores nulos por coluna
        /// </summary>
        public Dictionary<string, NullStats> AnalyzeNullColumns(DataTable dt)
        {
            var stats = new Dictionary<string, NullStats>();

            foreach (DataColumn col in dt.Columns)
            {
                int totalRows = dt.Rows.Count;
                int nullCount = 0;
                int emptyCount = 0;
                int validCount = 0;

                foreach (DataRow row in dt.Rows)
                {
                    var value = row[col];

                    if (value == null || value == DBNull.Value)
                    {
                        nullCount++;
                    }
                    else
                    {
                        string? strValue = value.ToString();
                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            emptyCount++;
                        }
                        else
                        {
                            validCount++;
                        }
                    }
                }

                stats[col.ColumnName] = new NullStats
                {
                    ColumnName = col.ColumnName,
                    TotalRows = totalRows,
                    NullCount = nullCount,
                    EmptyCount = emptyCount,
                    ValidCount = validCount,
                    IsCompletelyEmpty = (nullCount + emptyCount) == totalRows,
                    NullPercentage = (double)(nullCount + emptyCount) / totalRows * 100
                };
            }

            return stats;
        }
    }

    public class NullStats
    {
        public string ColumnName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int NullCount { get; set; }
        public int EmptyCount { get; set; }
        public int ValidCount { get; set; }
        public bool IsCompletelyEmpty { get; set; }
        public double NullPercentage { get; set; }

        public override string ToString()
        {
            if (IsCompletelyEmpty)
                return $"📊 {ColumnName}: COMPLETAMENTE VAZIA ({TotalRows} registros nulos/vazios)";

            return $"📊 {ColumnName}: {ValidCount} válidos, {NullCount} nulos, {EmptyCount} vazios ({NullPercentage:F1}% vazio)";
        }
    }
}