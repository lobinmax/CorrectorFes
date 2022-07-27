using System;
using System.IO;
using System.Windows.Forms;
using DevExpress.LookAndFeel;

namespace Корректор_ФЭС
{
    static class Program
    {
        public static string GetTemporyCatalog
        {
            get
            {
                var temporyCatalog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Templates)
                    , Application.CompanyName
                    , Application.ProductName);
                if (!Directory.Exists(temporyCatalog))
                {
                    Directory.CreateDirectory(temporyCatalog);
                }
                return temporyCatalog;
            }
        }
        public static string GetXsd_SKO115FZ_OPER => Path.Combine(Program.GetTemporyCatalog, "SKO115FZ_OPER.xsd");
        public static string GetXsd_data_types => Path.Combine(Program.GetTemporyCatalog, "data_types.xsd");

        [STAThread] static void Main()
        {
            UserLookAndFeel.Default.SkinName = "Caramel";
            DevExpress.UserSkins.BonusSkins.Register();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}
