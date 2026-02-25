namespace Universal_Data_Converter.Services.Sql
{
    public class SqlDialectFactory
    {
        public ISqlDialectExporter GetExporter(string dialect)
        {
            return dialect?.ToLower() switch
            {
                "mysql" => new MySqlExporter(),
                "postgresql" => new PostgreSqlExporter(),
                "sqlserver" => new SqlServerExporter(),
                "sqlite" => new SqliteExporter(),
                _ => new SqliteExporter()
            };
        }
    }
}