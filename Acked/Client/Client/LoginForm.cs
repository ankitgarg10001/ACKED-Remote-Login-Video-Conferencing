using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Ackedclient
{
    public partial class LoginForm : Form
    {
        public Socket serversocket;
        public string strName;
        byte[] b;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(txtName.Text.Contains("*"))
            {
                MessageBox.Show("please select other name without '*'");
                return;
            }

            try
            {
                serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPAddress ipAddress = IPAddress.Parse(txtServerIP.Text);
                //Server is listening on port 1000
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 1000);

                //Connect to the server
                serversocket.BeginConnect(ipEndPoint, new AsyncCallback(OnConnect), null);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
             } 
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                serversocket.EndSend(ar);
                strName = txtName.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                serversocket.EndConnect(ar);

                //We are connected so we login into the server
                //Data msgToSend = new Data ();
                //msgToSend.cmdCommand = Command.Login;
                //msgToSend.strName = txtName.Text;
                //msgToSend.strMessage = null;

                //byte[] b = msgToSend.ToByte ();

                string str;
                str = "1A" + txtName.Text;
                sendtoserver(str);


                //Send the message to the server
                //serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }


        void sendtoserver(string msg)
        {
            if (msg != "")
            {
                b = Encoding.ASCII.GetBytes(msg);
            }
            //else re-send whatever there is in bytearray again
            //Send the message to the server
            serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), null);


        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (txtName.Text.Length > 0 && txtServerIP.Text.Length > 0)
                btnOK.Enabled = true;
            else
                btnOK.Enabled = false;
        }

        private void txtServerIP_TextChanged(object sender, EventArgs e)
        {
            if (txtName.Text.Length > 0 && txtServerIP.Text.Length > 0)
                btnOK.Enabled = true;
            else
                btnOK.Enabled = false;
        }

        string pass = "";

        private void button1_Click(object sender, EventArgs e)
        {
            pass = txtServerIP.Text;
            try
            {
                UdpClient udp = new UdpClient(groupEP);
                udp.BeginReceive(new AsyncCallback(recieve), udp);
            }
            catch { }
        }
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 1150);



        private void recieve(IAsyncResult ar)
        {
            byte[] receiveBytes = ((UdpClient)ar.AsyncState).EndReceive(ar, ref groupEP);
            string ip = groupEP.Address.ToString();
            string returnData = Encoding.ASCII.GetString(receiveBytes);

            System.Threading.Thread.Sleep(1000);
            if (returnData.Length > 0 && pass == returnData)
            {
                txtServerIP.Text = ip;
                return;
            }
            else
            {
                try
                {
                    UdpClient udp = new UdpClient(groupEP);
                    udp.BeginReceive(new AsyncCallback(recieve), udp);
                }
                catch { }
            }
        }
    }
}