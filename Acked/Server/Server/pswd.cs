using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ackedserver
{
    public partial class pswd : Form
    {
        public pswd()
        {
            InitializeComponent();
        }
        public string pass = "";
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                pass = textBox1.Text;
                this.DialogResult = DialogResult.OK;
                Close();

            }
            else
            {
                MessageBox.Show("Please select a Valid Password!!");
            }
        }
    }
}
