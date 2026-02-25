using System.Data;
using Universal_Data_Converter.Models;

namespace Universal_Data_Converter.Services.Sql
{
    public interface ISqlDialectExporter
    {
        Task ExportAsync(DataTable dt, ConversionOptions options);
    }
}