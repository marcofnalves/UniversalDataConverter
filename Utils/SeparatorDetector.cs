using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Universal_Data_Converter.Utils
{
    public class SeparatorDetector
    {
        public string DetectFromFile(string filePath, Encoding encoding)
        {
            try
            {
                using (var reader = new StreamReader(filePath, encoding, true))
                {
                    var firstLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(firstLine)) return string.Empty;

                    return DetectFromString(firstLine);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public string DetectFromString(string text)
        {
            var separators = new Dictionary<char, int>
            {
                ['\t'] = 0,
                [','] = 0,
                [';'] = 0,
                ['|'] = 0
            };

            foreach (var sep in separators.Keys.ToList())
            {
                separators[sep] = text.Split(sep).Length;
            }

            var bestSep = separators.OrderByDescending(x => x.Value).FirstOrDefault();

            if (bestSep.Value > 1)
            {
                return bestSep.Key switch
                {
                    '\t' => "Tab",
                    ',' => ",",
                    ';' => ";",
                    '|' => "|",
                    _ => string.Empty
                };
            }

            return string.Empty;
        }
    }
}