using System;
using System.Windows.Forms;

namespace WinFixPro
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Global exception trapping for rock-solid stability
            Application.ThreadException += delegate (object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show("An unexpected error occurred:\n" + e.Exception.Message,
                    "WinFixPro Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e)
            {
                Exception ex = e.ExceptionObject as Exception;
                string msg = ex != null ? ex.Message : "Unknown fatal error";
                MessageBox.Show("Fatal system error:\n" + msg,
                    "WinFixPro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
