using System;
using System.Windows.Forms;

namespace RcmdWindows
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var trayApp = new TrayApplication())
            {
                Application.Run();
            }
        }
    }
}
