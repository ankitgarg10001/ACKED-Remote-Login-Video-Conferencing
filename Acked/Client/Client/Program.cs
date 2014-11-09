using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Ackedclient
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

            LoginForm loginForm = new LoginForm();

            // Application.Run(new request());
            Application.Run(loginForm);
            if (loginForm.DialogResult == DialogResult.OK)
            {
                
                TClient TClientForm = new TClient();
                TClientForm.serversocket = loginForm.serversocket;
                TClientForm.strName = loginForm.strName;

                TClientForm.ShowDialog();
            }

        }
    }
}