using System;
using System.Windows.Forms;

namespace UnityModManagerNet.Updater
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new UpdaterForm();
            Application.Run(form);
            //form.DoFormClosed();
        }
    }
}
