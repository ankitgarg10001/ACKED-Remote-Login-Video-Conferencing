using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ackedclient
{
    public partial class request : Form
    {
        int sec = 10;
        public request()
        {
            InitializeComponent();
        }

        public request(string[] s,char c)
        {
            InitializeComponent();
            if (c == 't')
            {
                this.Size = new System.Drawing.Size(300, 283);
                label2.Text = "Text Chat request From :\r\n";
                foreach (string k in s)
                {
                    if (k.Length > 0)
                        label2.Text += k + "\r\n";
                }
            }
            else if (c == 'a')
            {
                this.Size = new System.Drawing.Size(300, 183);
                label2.Text = "Voice Chat request From :\r\n";
                label2.Text += "Ip : " + s[1] + "\r\nNo : " + s[2] + "\r\nName : " + s[3];
        
            }

            else if (c == 'v')
            {
                this.Size = new System.Drawing.Size(300, 250);
                label2.Text = "Video Chat request From :\r\n";

                label2.Text += s[0] + "\r\n";
                label2.Text = "For Group :\r\n";
                foreach (string x in s)
                {

                    label2.Text += x + "\r\n";
                }

            }
            else if (c == 'f')
            {
                this.Size = new System.Drawing.Size(300, 150);

                label2.Text = "File Recieve" + s[2] + "\r\nrequest From :\r\n";

                label2.Text += s[0]+ "\r\n";
                
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sec > 0)
            {
                sec--;
                label1.Text = sec + " seconds to reply";
            }
            else
            {
                this.Close();
            }
        }

        private void request_Load(object sender, EventArgs e)
        {
            button2.Focus();
        }
    }
}
