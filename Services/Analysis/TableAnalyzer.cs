using System.Data;

namespace Universal_Data_Converter.Services.Analysis
{
    public class TableAnalyzer
    {
        public TableAnalysis Analyze(DataTable dt)
        {
            var analysis = new TableAnalysis();

            DetectPrimaryKeys(dt, analysis);
            DetectUniqueColumns(dt, analysis);
            DetectRequiredColumns(dt, analysis);
            SuggestIndexes(dt, analysis);

            return analysis;
        }

        private void DetectPrimaryKeys(DataTable dt, TableAnalysis analysis)
        {
            foreach (DataColumn col in dt.Columns)
            {
                var values = dt.AsEnumerable()
                    .Select(r => r[col]?.ToString())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                if (values.Count == dt.Rows.Count &&
                    values.Distinct().Count() == dt.Rows.Count)
                {
                    analysis.PrimaryKeyColumns.Add(col.ColumnName);
                    break;
                }
            }
        }

        private void DetectUniqueColumns(DataTable dt, TableAnalysis analysis)
        {
            foreach (DataColumn col in dt.Columns)
            {
                var values = dt.AsEnumerable()
                    .Select(r => r[col]?.ToString())
                    .ToList();

                if (values.Distinct().Count() == dt.Rows.Count)
                {
                    analysis.UniqueColumns.Add(col.ColumnName);
                }
            }
        }

        private void DetectRequiredColumns(DataTable dt, TableAnalysis analysis)
        {
            foreach (DataColumn col in dt.Columns)
            {
                var nullCount = dt.AsEnumerable()
                    .Count(r => r[col] == DBNull.Value || string.IsNullOrEmpty(r[col]?.ToString()));

                if (nullCount == 0)
                {
                    analysis.RequiredColumns.Add(col.ColumnName);
                }
            }
        }

        private void SuggestIndexes(DataTable dt, TableAnalysis analysis)
        {
            foreach (DataColumn col in dt.Columns)
            {
                var distinct = dt.AsEnumerable()
                    .Select(r => r[col]?.ToString())
                    .Distinct()
                    .Count();

                if (distinct > dt.Rows.Count * 0.7)
                {
                    analysis.SuggestedIndexes.Add(col.ColumnName);
                }
            }
        }
    }
}