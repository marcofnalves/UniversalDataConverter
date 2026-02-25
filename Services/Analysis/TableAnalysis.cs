namespace Universal_Data_Converter.Services.Analysis
{
    public class TableAnalysis
    {
        public List<string> PrimaryKeyColumns { get; set; } = new();
        public List<string> UniqueColumns { get; set; } = new();
        public List<string> RequiredColumns { get; set; } = new();
        public List<string> SuggestedIndexes { get; set; } = new();
        public List<string> IdentityColumns { get; set; } = new();
        public Dictionary<string, string> ForeignKeys { get; set; } = new();
    }
}