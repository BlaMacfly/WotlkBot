using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WotlkBotGui
{
    internal static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles(); 
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "WotlkBotGui Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
