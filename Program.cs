using System.Text;

namespace Universal_Data_Converter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Registrar code pages para suporte a encoding como iso-8859-1
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}