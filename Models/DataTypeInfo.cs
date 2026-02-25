using System;
using System.Collections.Generic;
using System.Text;

namespace Universal_Data_Converter.Models
{
    public class DataTypeInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string CurrentType { get; set; } = string.Empty;
        public string DetectedType { get; set; } = string.Empty;
        public int NonNullCount { get; set; }
        public int NullCount { get; set; }
        public int UniqueCount { get; set; }
        public List<string> SampleValues { get; set; } = new List<string>();
    }

    public class TypeDetectionResult
    {
        public string DetectedType { get; set; } = "string";
        public double Confidence { get; set; }
        public List<object> ConvertedValues { get; set; } = new List<object>();
    }
}
