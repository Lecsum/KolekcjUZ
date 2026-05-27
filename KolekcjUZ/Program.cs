using System;
using System.Windows.Forms;

namespace DynamicDatabaseApp // Tu wpisz nazwę swojego projektu
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // TUTAJ MUSI BYĆ MainForm zamiast Form1:
            Application.Run(new MainForm());
        }
    }
}