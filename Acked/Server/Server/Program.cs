using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Ackedserver
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            pswd p = new pswd();
            Application.Run(p);

            if (p.DialogResult == DialogResult.OK)
            {
                Server srvr = new Server();
                srvr.pass = p.pass;
                srvr.ShowDialog();

            }

            //Application.Run(new Server());
        }
    }
}
