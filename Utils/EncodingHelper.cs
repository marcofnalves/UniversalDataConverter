using System.Text;

namespace Universal_Data_Converter.Utils
{
    public static class EncodingHelper
    {
        public static Encoding GetEncoding(string encodingName)
        {
            return encodingName?.ToLower() switch
            {
                "utf-8" => Encoding.UTF8,
                "latin-1" => Encoding.Latin1,
                "iso-8859-1" => Encoding.GetEncoding("iso-8859-1"),
                "cp1252" => Encoding.GetEncoding(1252),
                _ => Encoding.UTF8
            };
        }

        public static string[] SupportedEncodings => new[]
        {
            "utf-8",
            "latin-1",
            "iso-8859-1",
            "cp1252"
        };
    }
}