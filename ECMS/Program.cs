using System;
using System.Windows.Forms;

namespace ECMS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Login -> Main window. After "Logout" the login window appears again.
            while (true)
            {
                using (frmLogin login = new frmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        break;   // user pressed Exit or closed the login window
                }

                using (frmMain main = new frmMain())
                {
                    Application.Run(main);

                    if (!main.LogoutRequested)
                        break;   // main window closed normally -> end the application
                }
            }
        }
    }
}