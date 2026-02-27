using System;
using System.Collections.Generic;
using System.Text;

namespace Universal_Data_Converter.Models
{
    public class XmlReaderOptions
    {
        public bool UseHeaderRow { get; set; } = true;
        public bool DetectDataTypes { get; set; } = true;
        public string? RecordPath { get; set; } // Caminho XPath opcional para os registros
        public bool IncludeAttributes { get; set; } = true;
        public bool IncludeElementNames { get; set; } = true;
    }
}
