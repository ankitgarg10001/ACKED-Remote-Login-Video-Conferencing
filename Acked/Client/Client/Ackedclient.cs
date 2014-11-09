using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;

//outer libraries
using LumiSoft.Net.UDP;
using LumiSoft.Net.Codec;
using LumiSoft.Media.Wave;
using Microsoft.Expression.Encoder.Devices;
using Microsoft.Expression.Encoder.Live;
using Microsoft.Expression.Encoder;

namespace Ackedclient
{
    public partial class TClient : Form
    {
        public Socket serversocket; //The main client socket
        public string strName;      //Name by which the user logs into the room

        private byte[] byteData = new byte[1024];
        byte[] b;

        private bool m_IsSendingTest = false;
        private UdpServer VUdpServer = null;
        private WaveIn VWaveIn = null;
        private WaveOut VWaveOut = null;
        private int m_Codec = 0;
        private FileStream VRecordStream = null;
        private IPEndPoint VTargetEP = null;
        private System.Windows.Forms.Timer VTimer = null;
        public string chatwith;
        public string myip = null;
        public bool chaton = false;



        public TClient()
        {
            InitializeComponent();
            tabControl2.TabPages[0].Focus();
            txtMessage.Focus();
        }
        //public delegate void MyDelegate();
        //Broadcast the message typed by the user to everyone
        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                //Fill the info for the message to be send
                if (tabControl1.SelectedTab.Text == "Main")
                {
                    string s = "1MA" + strName + " : " + txtMessage.Text;
                    sendtoserver(s);
                }
                else
                {
                    string s = "1M" + tabControl1.SelectedTab.Text + " " + strName + " : " + txtMessage.Text;
                    sendtoserver(s);
                }
                txtMessage.Text = null;
                txtMessage.Focus();
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "Acked text chat: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                serversocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Acked: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //static MethodInvoker addtab = new MethodInvoker(addgrp);

        //static void addgrp()
        //{
        //    //tabControl1.TabPages.Add(cht[cht.Count - 1].tp);
        //    //tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];


        //}

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


        List<gchat> cht = new List<gchat>();

        public void textrec(string msgReceived)
        {
            if (msgReceived[0] == 'L')
            {
                msgReceived = msgReceived.Substring(1);
                if (msgReceived[0] == 'A')
                {
                    msgReceived = msgReceived.Substring(1);
                    string[] ppl = msgReceived.Split('*');
                    lstChatters.Items.Clear();
                    foreach (string word in ppl)
                    {
                        if (word.Length > 1)
                            lstChatters.Items.Add(word);
                    }
                }
                else
                {
                    string no = null;
                    int i = 0;
                    while (msgReceived[i] != ' ')
                    {
                        no += msgReceived[i];
                        i++;

                    }
                    msgReceived = msgReceived.Substring(i + 1);
                    string[] ppl = msgReceived.Split('*');

                    foreach (gchat g in cht)
                    {
                        if (g.name == no)
                        {

                            g.lb.Items.Clear();
                            foreach (string word in ppl)
                            {
                                if (word.Length > 1)
                                    g.lb.Items.Add(word);
                            }
                            break;
                        }

                    }
                }
            }
            else if (msgReceived[0] == 'M')
            {
                if (msgReceived[1] == 'A')
                {
                    txtChatBox.Text += "\r\n" + msgReceived.Substring(2);
                    if (lstChatters.Items.Count == 0)
                        sendtoserver("1L");
                    //tabControl1.TabPages[0].BackColor=tabControl1.TabPages.co
                }
                else
                {
                    string no = null;
                    int i = 1;
                    while (msgReceived[i] != ' ')
                    {
                        no += msgReceived[i];
                        i++;
                    }
                    //now no has group no for which msg has been sent
                    msgReceived = msgReceived.Substring(i + 1);//it should have only msg left by now
                    foreach (gchat g in cht)
                    {
                        if (g.name == no)
                        {

                            g.tb.Text += "\r\n" + msgReceived;
                            break;
                        }

                    }


                }
            }
            else if (msgReceived[0] == 'O')
            {
                msgReceived = msgReceived.Substring(1);


                cht.Add(new gchat(msgReceived));


                this.Invoke((MethodInvoker)delegate
                {
                    //perform on the UI thread
                    //this.Controls.Add(l);
                    tabControl1.TabPages.Add(cht[cht.Count - 1].tp);
                    tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                    tabControl2.SelectedTab = tabControl2.TabPages[0];

                });

            }
            else if (msgReceived[0] == 'T')
            {
                msgReceived = msgReceived.Substring(1);
                string no = "0";
                int i = 0;
                while (msgReceived[i] != ' ')
                {
                    no += msgReceived[i];
                    i++;
                }
                msgReceived = msgReceived.Substring(i + 1);
                string[] ip = msgReceived.Split('*');
                request popup = new request(ip, 't');
                DialogResult dialogresult = popup.ShowDialog();
                string snd;
                //  R : response
                if (dialogresult == DialogResult.OK)
                {
                    MessageBox.Show("connected to group: " + no);
                    snd = "1RY" + no.Substring(1);
                    sendtoserver(snd);


                    cht.Add(new gchat(no.Substring(1)));


                    this.Invoke((MethodInvoker)delegate
                    {
                        //perform on the UI thread
                        //this.Controls.Add(l);
                        tabControl1.TabPages.Add(cht[cht.Count - 1].tp);
                        tabControl1.SelectedTab = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                        tabControl2.SelectedTab = tabControl2.TabPages[0];

                    });

                }
                else if (dialogresult == DialogResult.Cancel)
                {
                    snd = "1RN";
                    sendtoserver(snd);
                    //msg += "N";
                    //Console.WriteLine("You clicked either Cancel or X button in the top right corner");
                }
                popup.Dispose();
            }

        }


        public void voicrec(string msgReceived)
        {
            if (msgReceived[0] == 'V')
            {
                msgReceived = msgReceived.Substring(1);
                string[] ip = msgReceived.Split(' ');
                /*
                 * own ip
                 * request from ip
                 * ip no
                 * ip name
                 * */
                string msg = "2R";

                //sendtoserver("test");

                request popup = new request(ip, 'a');
                DialogResult dialogresult = popup.ShowDialog();
                //  R : response
                if (dialogresult == DialogResult.OK)
                {
                    MessageBox.Show("connected to " + ip[1]);

                    startlisten();
                    startsend(ip[1]);

                    msg += "Y";
                    //Console.WriteLine("You clicked OK");

                    chatwith = null;
                    chatwith = ip[2] + ' ' + ip[3];
                    chaton = true;
                    button4.Text = "stop chat";
                    button4.Enabled = true;
                    //button7.Enabled = false;
                }
                else if (dialogresult == DialogResult.Cancel)
                {
                    msg += "N";
                    //Console.WriteLine("You clicked either Cancel or X button in the top right corner");
                }
                popup.Dispose();
                msg += "a" + msgReceived;

                sendtoserver(msg);
                //sendtoserver(msg);
                /*
                b = Encoding.ASCII.GetBytes(msg);

                //Send the message to the server
                serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                // serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/

            }

            else if (msgReceived[0] == 'B')
            {
                MessageBox.Show("The person you are trying to voice chat with is busy right now.. \n please try again later!!");


            }
            else if (msgReceived[0] == 'R')
            {
                if (msgReceived[1] == 'Y')
                {
                    msgReceived = msgReceived.Substring(2);
                    string[] ip = msgReceived.Split(' ');
                    startsend(ip[0]);
                    chaton = true;
                    button4.Text = "stop chat";
                    button4.Enabled = true;
                    //button7.Enabled = false;
                    MessageBox.Show("Request to" + ip[0] + " Accepted!!\r\nChat is Now on :)");

                }

                else
                {
                    msgReceived = msgReceived.Substring(2);
                    string[] ip = msgReceived.Split(' ');
                    MessageBox.Show("Request to" + ip[0] + " Rejected");
                    endlisten();
                    chatwith = null;
                    chaton = false;
                    button4.Text = "Talk";
                    button4.Enabled = true;
                    //button7.Enabled = true;
                    endsend();
                }
            }

            else if (msgReceived[0] == 'Q')
            {
                msgReceived = msgReceived.Substring(1);
                MessageBox.Show(msgReceived + " Left chat, Closing Your Connections.");

                chatwith = null;
                endsend();
                endlisten();
                chaton = false;
                button4.Text = "Talk";
                button4.Enabled = true;
                // button7.Enabled = true;
            }


        }

        FTServerCode obj;

        public void filshare(string msgReceived)
        {
            try
            {
                if (msgReceived[0] == 'N')
                {

                    msgReceived = msgReceived.Substring(1);
                    string[] ip = msgReceived.Split('*');

                    string msg = "4R";

                    //sendtoserver("test");

                    request popup = new request(ip, 'f');
                    DialogResult dialogresult = popup.ShowDialog();
                    FolderBrowserDialog fd = new FolderBrowserDialog();
                    //  R : response
                    if (dialogresult == DialogResult.OK)
                    {
                        msg += "Y" + ip[0];
                        sendtoserver(msg);

                        backgroundWorker1.RunWorkerAsync();

                        //button13.Text = "stop chat";
                    }
                    else if (dialogresult == DialogResult.Cancel)
                    {
                        msg += "N";
                        sendtoserver(msg);
                        //Console.WriteLine("You clicked either Cancel or X button in the top right corner");
                    }
                    popup.Dispose();



                }

                else if (msgReceived[0] == 'S')
                {
                    string ip = msgReceived.Substring(1);
                    FTClientCode.SendFile(sendfile, ip);
                }




            }
            catch { }

        }


        public void vidrec(string msgReceived)
        {

            if (msgReceived[0] == 'O')
            {
                startbrod();
                button13.Text = "stop chat";
            }
            else if (msgReceived[0] == 'V')
            {
                msgReceived = msgReceived.Substring(1);

                string no = "";
                int i = 0;
                while (msgReceived[i] != ' ')
                {
                    no += msgReceived[i];
                    i++;
                }
                msgReceived = msgReceived.Substring(i + 1);


                string[] ip = msgReceived.Split('*');

                string msg = "3R" + no + " ";

                //sendtoserver("test");

                request popup = new request(ip, 'v');
                DialogResult dialogresult = popup.ShowDialog();
                //  R : response
                if (dialogresult == DialogResult.OK)
                {
                    MessageBox.Show("Joined Video Conference");
                    startbrod();

                    msg += "Y";

                    button13.Text = "stop chat";
                }
                else if (dialogresult == DialogResult.Cancel)
                {
                    msg += "N";
                    //Console.WriteLine("You clicked either Cancel or X button in the top right corner");
                }
                popup.Dispose();
                msg += msgReceived;

                sendtoserver(msg);


            }
            else if (msgReceived[0] == 'N')
            {
                msgReceived = msgReceived.Substring(1);
                //add new ip to cam n init label
                //name ip**name ip**
                //use foreach loop
                string[] vi = msgReceived.Split('*');
                foreach (string k in vi)
                {
                    if (k.Length > 1)
                    {
                        addnewsrc(k.Split(' ')[0], k.Split(' ')[1]);
                    }
                }






            }




        }
        int camcount = 2;
        byte ini = new byte();

        public void addnewsrc(string nam, string id)
        {
            try
            {
                if (camcount == 2)
                {
                    axWindowsMediaPlayer2.URL = "mms://" + id + ":8500";
                    label15.Text = nam;
                    camcount++;
                    return;
                }
                else if (camcount == 3)
                {
                    axWindowsMediaPlayer3.URL = "mms://" + id + ":8500";
                    label16.Text = nam;
                    camcount++;
                    return;
                }
                else if (camcount == 4)
                {
                    axWindowsMediaPlayer4.URL = "mms://" + id + ":8500";
                    label17.Text = nam;
                    camcount++;
                    return;
                }
                else if (camcount == 5)
                {
                    axWindowsMediaPlayer5.URL = "mms://" + id + ":8500";
                    label18.Text = nam;
                    camcount++;
                    return;
                }
                else if (camcount == 6)
                {
                    axWindowsMediaPlayer6.URL = "mms://" + id + ":8500";
                    label19.Text = nam;
                    camcount++;
                    return;
                }
            }
            catch { MessageBox.Show("error adding " + nam + " to video"); }
        }

        private void OnReceive(IAsyncResult ar)
        {


            try
            {
                label1.Text = (Convert.ToInt32(label1.Text) - 1).ToString();
                serversocket.EndReceive(ar);
                string msgReceived = Encoding.ASCII.GetString(byteData);
                msgReceived = msgReceived.Substring(0, msgReceived.IndexOf('\0'));

                for (int i = 0; i < msgReceived.Length; i++)
                {
                    byteData[i] = ini;
                }

                if (msgReceived == "")
                {
                    MessageBox.Show("empty msg");

                }


                try
                {
                    if (msgReceived == "resend")
                    {
                        sendtoserver("");
                    }


                    else if (msgReceived == "test")
                    {
                        MessageBox.Show("test");
                    }
                    else if (msgReceived[0] == 'I')
                    {
                        try
                        {
                            MessageBox.Show(msgReceived.Substring(1));
                        }
                        catch { }
                    }

                    else if (msgReceived[0] == '1')
                    {
                        textrec(msgReceived.Substring(1));
                    }



                    else if (msgReceived[0] == '2')
                    {
                        voicrec(msgReceived.Substring(1));

                    }

                    else if (msgReceived[0] == '3')
                    {
                        vidrec(msgReceived.Substring(1));

                    }
                    else if (msgReceived[0] == '4')
                    {
                        filshare(msgReceived.Substring(1));

                    }

                }
                catch (Exception ex) { MessageBox.Show("error at client recieve" + ex); }
                serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                label1.Text = (Convert.ToInt32(label1.Text) + 1).ToString();

            }
            catch { }
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Acked : " + strName;

            TextBox.CheckForIllegalCrossThreadCalls = false;
            CheckedListBox.CheckForIllegalCrossThreadCalls = false;
            Button.CheckForIllegalCrossThreadCalls = false;
            TabControl.CheckForIllegalCrossThreadCalls = false;
            TabPage.CheckForIllegalCrossThreadCalls = false;
            ListBox.CheckForIllegalCrossThreadCalls = false;
            Label.CheckForIllegalCrossThreadCalls = false;
            AxWMPLib.AxWindowsMediaPlayer.CheckForIllegalCrossThreadCalls = false;

            try
            {
                loaddevices();

                loadvid();

                axWindowsMediaPlayer1.settings.mute = true; //make your player mute
                myip = "" + IPAddress.Parse(((IPEndPoint)serversocket.LocalEndPoint).Address.ToString());

            }
            catch { MessageBox.Show("error initializing devices, this client may not work..please logout and login again"); }
            //The user has logged into the system so we now request the server to send
            //the names of all users who are in the chat room
            byteData = new byte[1024];
            //Start listening to the data asynchronously
            serversocket.BeginReceive(byteData,
                                       0,
                                       byteData.Length,
                                       SocketFlags.None,
                                       new AsyncCallback(OnReceive),
                                       null);
            label1.Text = (Convert.ToInt32(label1.Text) + 1).ToString();
            //backgroundWorker1.RunWorkerAsync();
        }

        private void loadvid()
        {
            try
            {
                comboBox3.Items.Clear();
                foreach (EncoderDevice edv in EncoderDevices.FindDevices(EncoderDeviceType.Video))
                {
                    comboBox3.Items.Add(edv.Name);
                }
                comboBox6.Items.Clear();
                foreach (EncoderDevice eda in EncoderDevices.FindDevices(EncoderDeviceType.Audio))
                {
                    comboBox6.Items.Add(eda.Name);
                }
                if (comboBox3.Items.Count > 0)
                    comboBox3.SelectedIndex = 0;
                if (comboBox6.Items.Count > 0)
                    comboBox6.SelectedIndex = 0;
            }
            catch { }

            //axWindowsMediaPlayer1.uiMode = "mini";//none,full,invisible,mini

            //axWindowsMediaPlayer2.uiMode = "none";
            //axWindowsMediaPlayer3.uiMode = "none";
            //axWindowsMediaPlayer4.uiMode = "mini";


        }

        private void loaddevices()
        {
            //VWaveIn.Dispose();
            VWaveIn = null;
            // VWaveOut.Dispose();
            VWaveOut = null;
            comboBox1.Items.Clear();
            foreach (WavInDevice device in WaveIn.Devices)
            {
                comboBox1.Items.Add(device.Name);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            // Load output devices.
            comboBox2.Items.Clear();
            foreach (WavOutDevice device in WaveOut.Devices)
            {
                comboBox2.Items.Add(device.Name);
            }
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 0;
            }


            comboBox4.Items.Clear();
            foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
            {
                comboBox4.Items.Add(ip.ToString());
            }

            if (comboBox4.Items.Count > 0)
            {
                comboBox4.SelectedIndex = 0;
            }

            comboBox5.Items.Clear();
            foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
            {
                comboBox5.Items.Add(ip.ToString());
            }

            if (comboBox5.Items.Count > 0)
            {
                comboBox5.SelectedIndex = 0;
            }


            VTimer = new System.Windows.Forms.Timer();
            VTimer.Interval = 1000;
            VTimer.Tick += new EventHandler(VTimer_Tick);


        }


        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            if (txtMessage.Text.Length == 0)
                btnSend.Enabled = false;
            else
                btnSend.Enabled = true;
        }

        private void TClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to leave the chat room?", "Acked: " + strName,
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
            try
            {
                endlisten();
                endsend();
                StopJob();
                if (isbrodcast)
                {
                    endbrod();
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch { }
            try
            {
                //if (chaton == true)
                //{

                //    string Name = "2Qa" + chatwith;
                //    sendtoserver(Name);
                //    endsend();
                //    chatwith = null;
                //    chaton = false;
                //    button4.Text = "Talk";
                //    button7.Enabled = true;
                //    endlisten();
                //}
                sendtoserver("1X");
                serversocket.Close();
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Acked: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSend_Click(sender, null);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (lstChatters.CheckedIndices.Count > 0)
            {
                string msg;
                msg = null;
                foreach (string s in lstChatters.CheckedItems)
                {
                    msg += s + "**";
                }
                msg = "1T" + msg;
                sendtoserver(msg);
            }
            else
            {
                MessageBox.Show("please select someone to chat with first!!");
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text != "Main")
            {
                int i = 0;
                foreach (gchat x in cht)
                {
                    if (x.tp.Text == tabControl1.SelectedTab.Text) //determine tab to remove
                    {
                        break;
                    }
                    i++;
                }
                string str = "1X" + cht[i].name;
                sendtoserver(str);
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
                cht.Remove(cht[i]);
            }
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            //MessageBox.Show("Ankit"+tabControl1.SelectedTab.Text);
            if (tabControl1.SelectedTab.Text == "Main")
            {
                button2.Enabled = false;
            }
            else
                button2.Enabled = true;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (button12.Text == "Test")
            {
                m_IsSendingTest = true;
                button12.Text = "stop";
                button11.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button9.Enabled = false;
                start_work();
                tabPage2.Enabled = false;
                tabPage3.Enabled = false;
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                m_Codec = 0;
                VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
                VWaveOut = new WaveOut(WaveOut.Devices[comboBox1.SelectedIndex], 8000, 16, 1);
                VWaveIn.BufferFull += new BufferFullHandler(audio_BufferFull);
                VWaveIn.Start();

            }
            else
            {
                m_IsSendingTest = false;
                button12.Text = "Test";
                button11.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                button9.Enabled = true;
                end_work();
                try
                {
                    VWaveIn.Dispose();
                }
                catch { }

                VWaveOut = null;
            }
        }
        private void audio_BufferFull(byte[] buffer)
        {
            // Compress data.
            byte[] encodedData = null;
            if (m_Codec == 0)
            {
                encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
            }
            else if (m_Codec == 1)
            {
                encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
            }

            // We just sent buffer to target end point.

            if (m_IsSendingTest)
            {
                byte[] decodedData = null;
                if (m_Codec == 0)
                {
                    decodedData = G711.Decode_aLaw(encodedData, 0, encodedData.Length);
                }
                else if (m_Codec == 1)
                {
                    decodedData = G711.Decode_uLaw(encodedData, 0, encodedData.Length);
                }

                // We just play received packet.
                VWaveOut.Play(decodedData, 0, decodedData.Length);
                /* 
                VWaveOut.Play(buffer, 0, buffer.Length);
                */
            }
            else //sending to server
            {
                // We just sent buffer to target end point.
                VUdpServer.SendPacket(encodedData, 0, encodedData.Length, VTargetEP);
            }

        }

        private void button11_Click(object sender, EventArgs e)
        {
            loaddevices();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            m_Codec = 0;
            myip = "" + IPAddress.Parse(((IPEndPoint)serversocket.LocalEndPoint).Address.ToString());

            if (button4.Text == "Talk")
            {
                startlisten();
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (lstChatters.CheckedIndices.Count == 1)
                {
                    if ((lstChatters.CheckedItems[0].ToString().Length > 1) && (lstChatters.CheckedItems[0].ToString() != "No client available currently"))
                    {
                        //V+codec+requested ip
                        string Name = "2Va" + lstChatters.CheckedItems[0].ToString();

                        sendtoserver(Name);



                        //ankitbyteData = new byte[1024];
                        serversocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
                        label1.Text = (Convert.ToInt32(label1.Text) + 1).ToString();

                        chatwith = null;
                        chatwith = lstChatters.CheckedItems[0].ToString();

                        /*
                        b = null;
                        b = Encoding.ASCII.GetBytes(Name);

                        //Send the message to the server
                        serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);*/

                    }


                }
                else
                {
                    MessageBox.Show("Please select only 1 person to chat with !!");
                    return;
                }


            }

            else
            {
                //end_work();
                //endlisten();
                try
                {
                    string Name = "2Qa" + chatwith;
                    endlisten();
                    sendtoserver(Name);

                    endsend();
                    chatwith = null;
                    chaton = false;
                    button4.Text = "Talk";
                    button7.Enabled = true;
                }
                catch
                {
                    MessageBox.Show("error connecting/disconnecting");
                }
            }
        }

        private void VWaveIn_BufferFull(byte[] buffer)
        {
            // Compress data. 
            byte[] encodedData = null;
            if (m_Codec == 0)
            {
                encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
            }
            else if (m_Codec == 1)
            {
                encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
            }

            // We just sent buffer to target end point.

            VUdpServer.SendPacket(encodedData, 0, encodedData.Length, VTargetEP);
        }

        private void VUdpServer_PacketReceived(UdpPacket_eArgs e)
        {
            // Decompress data.
            byte[] decodedData = null;
            if (m_Codec == 0)
            {
                decodedData = G711.Decode_aLaw(e.Data, 0, e.Data.Length);
            }
            else if (m_Codec == 1)
            {
                decodedData = G711.Decode_uLaw(e.Data, 0, e.Data.Length);
            }

            // We just play received packet.
            VWaveOut.Play(decodedData, 0, decodedData.Length);

            // Record if recoring enabled.
            if (VRecordStream != null)
            {
                VRecordStream.Write(decodedData, 0, decodedData.Length);
            }
        }

        private void VTimer_Tick(object sender, EventArgs e)
        {
            VPacketsReceived.Text = VUdpServer.PacketsReceived.ToString();
            VBytesReceived.Text = VUdpServer.BytesReceived.ToString();
            VPacketsSent.Text = VUdpServer.PacketsSent.ToString();
            VBytesSent.Text = VUdpServer.BytesSent.ToString();
        }



        private void start_work()
        {
            //button1.Enabled = true;
            button11.Enabled = false;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            //tabPage2.Enabled = false;
            //tabControl1.TabIndex(1).
        }

        private void end_work()
        {
            button11.Enabled = true;
            button12.Enabled = true;
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            tabPage2.Enabled = true;
            tabPage3.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (button5.Text == "Listen")
            {
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (comboBox4.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select Your Ip address !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                myip = comboBox4.Text;
                m_Codec = 0;
                startlisten();
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                button12.Enabled = false;
                button11.Enabled = false;
                tabPage3.Enabled = false;
                button5.Text = "stop listen";
                textBox3.Enabled = true;
                button6.Enabled = true;
                button4.Enabled = false;
            }
            else
            {
                endlisten();
                button5.Text = "Listen";

                button6.Text = "Transmit";


                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                //comboBox3.Enabled = true;
                button12.Enabled = true;
                button11.Enabled = true;
                tabPage3.Enabled = true;
                textBox3.Enabled = false;
                button6.Enabled = false;
                button4.Enabled = true;
            }
        }

        public void startlisten()
        {
            #region start recieving



            VWaveOut = new WaveOut(WaveOut.Devices[comboBox2.SelectedIndex], 8000, 16, 1);

            VUdpServer = new UdpServer();
            VUdpServer.Bindings = new IPEndPoint[] { new IPEndPoint(IPAddress.Parse(myip), 1200) };
            VUdpServer.PacketReceived += new PacketReceivedHandler(VUdpServer_PacketReceived);
            VUdpServer.Start();
            VTimer.Start();
            //return;
            #endregion

        }

        public void endlisten()
        {
            try
            {
                VUdpServer.Dispose();
                VUdpServer = null;

                VWaveOut.Dispose();
                VWaveOut = null;

                if (VRecordStream != null)
                {
                    VRecordStream.Dispose();
                    VRecordStream = null;
                }

                VTimer.Stop();
                endsend();
            }
            catch { }
            //VTimer = null;


        }

        public void startsend(string s)
        {

            try
            {
                VTargetEP = new IPEndPoint(IPAddress.Parse(s), 1200);
            }
            catch
            {
                MessageBox.Show(this, "Invalid target IP address recieved", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
            VWaveIn.BufferFull += new BufferFullHandler(VWaveIn_BufferFull);
            VWaveIn.Start();

        }

        public void endsend()
        {
            try
            {
                VWaveIn.Dispose();
                VWaveIn = null;
            }
            catch { }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (button6.Text == "Transmit")
            {
                try
                {
                    startsend(textBox3.Text);
                    textBox3.Enabled = false;
                    button6.Text = "stop trans.";
                }
                catch { }
            }
            else
            {
                endsend();
                textBox3.Enabled = true;
                button6.Text = "Transmit";
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            sendtoserver("test");
        }

        List<IPEndPoint> gpcst = new List<IPEndPoint>();
        private void button9_Click(object sender, EventArgs e)
        {

            if (button9.Text == "Listen")
            {
                if (comboBox1.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select input device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select output device !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (comboBox4.SelectedIndex == -1)
                {
                    MessageBox.Show(this, "Please select Your Ip address !", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                myip = comboBox4.Text;
                m_Codec = 0;
                startlisten();
                comboBox1.Enabled = false;
                comboBox2.Enabled = false;
                //comboBox3.Enabled = false;
                button12.Enabled = false;
                button11.Enabled = false;
                tabPage2.Enabled = false;
                //tabPage3.Enabled = false;
                comboBox5.Enabled = false;
                textBox4.Enabled = true;
                button8.Enabled = true;
                button9.Text = "stop listen";
                button4.Enabled = false;
            }
            else
            {
                endlisten();
                button9.Text = "Listen";
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                //comboBox3.Enabled = true;
                button12.Enabled = true;
                button11.Enabled = true;
                tabPage2.Enabled = true;
                comboBox5.Enabled = true;
                textBox4.Enabled = false;
                button8.Enabled = false;
                button4.Enabled = true;
                gpcst.Clear();
                checkedListBox1.Items.Clear();
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {

            IPEndPoint x;
            try
            {
                x = new IPEndPoint(IPAddress.Parse(textBox4.Text), 1200);
            }
            catch
            {
                MessageBox.Show(this, "Invalid target IP address recieved", "Error:", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (checkedListBox1.Items.Contains(x.Address.ToString()) == false)
            {

                checkedListBox1.Items.Add(x.Address.ToString());
                gpcst.Add(x);
            }

            if (gpcst.Count == 1)
            {
                try
                {
                    button10.Enabled = true;
                    VWaveIn = new WaveIn(WaveIn.Devices[comboBox1.SelectedIndex], 8000, 16, 1, 400);
                    VWaveIn.BufferFull += new BufferFullHandler(gpcst_BufferFull);
                    VWaveIn.Start();
                }
                catch { }
            }

        }

        private void gpcst_BufferFull(byte[] buffer)
        {
            // Compress data. 
            if (gpcst.Count > 0)
            {
                byte[] encodedData = null;
                if (m_Codec == 0)
                {
                    encodedData = G711.Encode_aLaw(buffer, 0, buffer.Length);
                }
                else if (m_Codec == 1)
                {
                    encodedData = G711.Encode_uLaw(buffer, 0, buffer.Length);
                }

                // We just sent buffer to target end point.
                foreach (IPEndPoint k in gpcst)
                {
                    VUdpServer.SendPacket(encodedData, 0, encodedData.Length, k);
                }
            }
            else
            {
                VWaveIn.Stop();
                VWaveIn = null;
                checkedListBox1.Items.Clear();
                gpcst.Clear();
                button10.Enabled = false;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            IPEndPoint temp;
            try
            {
                foreach (string s in checkedListBox1.CheckedItems)
                {
                    temp = new IPEndPoint(IPAddress.Parse(s), 1200);

                    gpcst.Remove(temp);
                }
            }
            catch { }

            while ((checkedListBox1.Items.Count >= 0) && (checkedListBox1.CheckedItems.Count > 0))
            {

                checkedListBox1.Items.Remove(checkedListBox1.CheckedItems[0]);
            }
            if (gpcst.Count < 1)
            {
                try
                {
                    VWaveIn.Stop();
                    VWaveIn = null;
                    checkedListBox1.Items.Clear();
                    gpcst.Clear();
                    button10.Enabled = false;
                }
                catch { }
            }
        }

        private LiveJob job;
        private LiveDeviceSource _deviceSource;
        bool isbrodcast = false;
        //private bool _bStartedRecording = false;
        PullBroadcastPublishFormat format = new PullBroadcastPublishFormat();



        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Start preview")
            {
                button3.Enabled = false;
                EncoderDevice video = null;
                EncoderDevice audio = null;

                StopJob();
                GetSelectedVideoAndAudioDevices(out video, out audio);

                if (video == null)
                {
                    return;
                }

                // Starts new job for preview window
                job = new LiveJob();

                // Checks for a/v devices
                if (video != null && audio != null)
                {
                    // Create a new device source. We use the first audio and video devices on the system
                    _deviceSource = job.AddDeviceSource(video, audio);
                    // No
                    // Setup the video resolution and frame rate of the video device
                    // NOTE: Of course, the resolution and frame rate you specify must be supported by the device!
                    // NOTE2: May be not all video devices support this call, and so it just doesn't work, as if you don't call it (no error is raised)
                    // NOTE3: As a workaround, if the .PickBestVideoFormat method doesn't work, you could force the resolution in the 
                    //        following instructions (called few lines belows): 'panelVideoPreview.Size=' and '_job.OutputFormat.VideoProfile.Size=' 
                    //        to be the one you choosed (640, 480).

                    if (comboBox3.SelectedItem.ToString().EndsWith("(VFW)", StringComparison.OrdinalIgnoreCase))
                    {
                        // Yes
                        //MessageBox.Show("here");
                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VfwFormatDialog))
                        {
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VfwFormatDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VfwSourceDialog))
                        {
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VfwSourceDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VfwDisplayDialog))
                        {
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VfwDisplayDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                    }
                    else
                    {
                        // No
                        //MessageBox.Show("here1");
                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VideoCapturePinDialog))
                        {
                            // MessageBox.Show("here2");
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VideoCapturePinDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VideoCaptureDialog))
                        {
                            // MessageBox.Show("here3");
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VideoCaptureDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VideoCrossbarDialog))
                        {
                            //MessageBox.Show("here4");
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VideoCrossbarDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VideoPreviewPinDialog))
                        {
                            // MessageBox.Show("here5");
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VideoPreviewPinDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }

                        if (_deviceSource.IsDialogSupported(ConfigurationDialog.VideoSecondCrossbarDialog))
                        {
                            //MessageBox.Show("here6");
                            _deviceSource.ShowConfigurationDialog(ConfigurationDialog.VideoSecondCrossbarDialog, (new HandleRef(pictureBox1, pictureBox1.Handle)));
                        }
                    }


                    // Get the properties of the device video
                    SourceProperties sp = _deviceSource.SourcePropertiesSnapshot();

                    // Resize the preview panel to match the video device resolution set
                    //panelVideoPreview.Size = new Size(sp.Size.Width, sp.Size.Height);

                    // Setup the output video resolution file as the preview
                    job.OutputFormat.VideoProfile.Size = new Size(sp.Size.Width, sp.Size.Height);

                    // Display the video device properties set
                    //toolStripStatusLabel1.Text = sp.Size.Width.ToString() + "x" + sp.Size.Height.ToString() + "  " + sp.FrameRate.ToString() + " fps";

                    // Sets preview window to winform panel hosted by xaml window
                    /***********************to start preview in picturebox1**************************/
                    _deviceSource.PreviewWindow = new PreviewWindow(new HandleRef(pictureBox1, pictureBox1.Handle));


                    job.ActivateSource(_deviceSource);
                    button3.Text = "Stop n Apply";

                    button3.Enabled = true;
                }

            }
            else
            {
                button3.Enabled = false;
                if (isbrodcast)
                {
                    job.ApplyPreset(LivePresets.VC1512kDSL16x9);
                    format.BroadcastPort = 8500;
                    format.MaximumNumberOfConnections = 6;
                    job.PublishFormats.Add(format);
                    // Starts encoding
                    job.StartEncoding();
                    axWindowsMediaPlayer1.URL = "mms://localhost:8500";

                }
                else
                {
                    StopJob();
                }
                button3.Text = "Start preview";

                button3.Enabled = true;
            }
        }

        public void endbrod()
        {
            try
            {
                StopJob();
                isbrodcast = false;

                axWindowsMediaPlayer2.URL = null;
                axWindowsMediaPlayer3.URL = null;
                axWindowsMediaPlayer4.URL = null;
                axWindowsMediaPlayer5.URL = null;
                axWindowsMediaPlayer6.URL = null;
                camcount = 2;
                label15.Text = "";
                label16.Text = "";
                label17.Text = "";
                label18.Text = "";
                label19.Text = "";

                job.PublishFormats.Remove(format);
                job = null;

                sendtoserver("3S");

            }
            catch { }
        }

        public void startbrod()
        {
            EncoderDevice video = null;
            EncoderDevice audio = null;

            GetSelectedVideoAndAudioDevices(out video, out audio);
            StopJob();

            if (video == null)
            {
                return;
            }

            job = new LiveJob();

            _deviceSource = job.AddDeviceSource(video, audio);
            job.ActivateSource(_deviceSource);

            //_deviceSource.PreviewWindow = new PreviewWindow(new HandleRef(pictureBox1, pictureBox1.Handle));

            // Finds and applys a smooth streaming preset        
            // job.ApplyPreset(LivePresets.VC1512kDSL16x9);
            job.ApplyPreset(LivePresets.VC1512kDSL16x9);

            // Creates the publishing format for the job
            format.BroadcastPort = 8500;
            format.MaximumNumberOfConnections = 8;

            // Adds the publishing format to the job
            job.PublishFormats.Add(format);

            // Starts encoding
            job.StartEncoding();
            axWindowsMediaPlayer1.URL = "mms://localhost:8500";
            isbrodcast = true;

        }


        private void GetSelectedVideoAndAudioDevices(out EncoderDevice video, out EncoderDevice audio)
        {
            video = null;
            audio = null;

            if (comboBox3.SelectedIndex < 0 || comboBox6.SelectedIndex < 0)
            {
                MessageBox.Show("No Video and Audio capture devices have been selected.\nSelect an audio and video devices from the listboxes and try again.", "Warning");
                return;
            }

            // Get the selected video device            
            foreach (EncoderDevice edv in EncoderDevices.FindDevices(EncoderDeviceType.Video))
            {
                if (String.Compare(edv.Name, comboBox3.SelectedItem.ToString()) == 0)
                {
                    video = edv;
                    break;
                }
            }

            // Get the selected audio device            
            foreach (EncoderDevice eda in EncoderDevices.FindDevices(EncoderDeviceType.Audio))
            {
                if (String.Compare(eda.Name, comboBox6.SelectedItem.ToString()) == 0)
                {
                    audio = eda;
                    break;
                }
            }
        }

        void StopJob()
        {
            // Has the Job already been created ?
            try
            {
                if (job != null)
                {
                    // Yes
                    // Is it capturing ?
                    //if (_job.IsCapturing)

                    job.StopEncoding();

                    _deviceSource.PreviewWindow = null;
                    // Remove the Device Source and destroy the job
                    job.RemoveDeviceSource(_deviceSource);

                    // Destroy the device source
                    _deviceSource = null;
                }
            }
            catch { }
        }




        private void button13_Click(object sender, EventArgs e)
        {
            if (button13.Text == "Start chat")
            {
                if (lstChatters.CheckedIndices.Count > 0 && lstChatters.CheckedIndices.Count < 6)
                {
                    string msg;
                    msg = null;
                    foreach (string s in lstChatters.CheckedItems)
                    {
                        msg += s + "**";
                    }
                    msg = "3V" + msg;
                    sendtoserver(msg);
                    button13.Text = "stop chat";
                }
                else
                {
                    MessageBox.Show("Clients must be between 1 and 5!!");
                }

            }
            else
            {

                endbrod();
                button13.Text = "Start chat";
            }
        }
        string sendfile = "";
        private void button14_Click(object sender, EventArgs e)
        {
            if (lstChatters.CheckedIndices.Count > 0)
            {

                FileDialog fDg = new OpenFileDialog();

                if (fDg.ShowDialog() == DialogResult.OK)
                {
                    string msg = "4N";
                    foreach (string s in lstChatters.CheckedItems)
                    {
                        msg += s + "**";
                    }

                    string filePath = "";
                    string fileName = fDg.FileName.Replace("\\", "/");

                    while (fileName.IndexOf("/") > -1)
                    {
                        filePath += fileName.Substring(0, fileName.IndexOf("/") + 1);

                        fileName = fileName.Substring(fileName.IndexOf("/") + 1);
                    }

                    sendfile = filePath + fileName;
                    msg += ":" + fileName;
                    sendtoserver(msg);
                    //FTClientCode.SendFile(fDg.FileName, msg, serversocket);
                }
            }
            else
            {
                MessageBox.Show("Please select someone atleast :)");
            }

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!FTServerCode.on)
                {
                    obj = new FTServerCode();
                    obj.StartServer();
                    FTServerCode.on = true;
                }
            }
            catch { }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                FTServerCode.receivedPath = fd.SelectedPath;
            }

            //backgroundWorker1.CancelAsync();
        }

        private void Achat_Click(object sender, EventArgs e)
        {
            if (isbrodcast)
            {
                button4.Enabled = false;
                button5.Enabled = false;
                button9.Enabled = false;
                button12.Enabled = false;

            }
            else
            {
                if (button4.Text != "Talk")
                {
                    button4.Enabled = true;
                    button5.Enabled = false;
                    button9.Enabled = false;
                    button12.Enabled = false;
                }
                else if (button5.Text != "Listen")
                {

                    button4.Enabled = false;
                    button5.Enabled = true;
                    button9.Enabled = false;
                    button12.Enabled = false;
                }
                else if (button9.Text != "Listen")
                {


                    button4.Enabled = false;
                    button5.Enabled = false;
                    button9.Enabled = true;
                    button12.Enabled = false;
                }
                else if (button12.Text != "Test")
                {


                    button4.Enabled = false;
                    button5.Enabled = false;
                    button9.Enabled = false;
                    button12.Enabled = true;
                }
                else
                {
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button9.Enabled = true;
                    button12.Enabled = true;
                }
            }
        }

        private void Vchat_Click(object sender, EventArgs e)
        {
            if (chaton)
            {
                button13.Enabled = false;
                button3.Enabled = false;

            }
            else
            {

                button13.Enabled = true;
                button3.Enabled = true;
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }



    }


    class gchat
    {
        public TabPage tp = new TabPage();
        public string name { set; get; }//to store unique group no
        public TextBox tb = new TextBox();
        public int unread;
        public ListBox lb = new ListBox();
        public gchat()
        {
            name = null;
            tb.Text = null;
            tb.ScrollBars = ScrollBars.Both;
            tb.Size = new System.Drawing.Size(575, 310);
            tb.BackColor = System.Drawing.SystemColors.Control;
            tb.ReadOnly = true;
            tb.Multiline = true;
            tb.Location = new Point(3, 3);

            lb.Location = new Point(583, 6);
            lb.Size = new System.Drawing.Size(105, 303);



            unread = 0;
            lb.Items.Clear();
            tp.SuspendLayout();
            tp.Controls.Add(tb);
            tp.Controls.Add(lb);
            tp.ResumeLayout();
        }

        public gchat(String nam)
        {
            name = nam;
            tp.Text = name;
            tb.ScrollBars = ScrollBars.Both;
            tb.Size = new System.Drawing.Size(575, 310);
            tb.BackColor = System.Drawing.SystemColors.Control;
            tb.ReadOnly = true;
            tb.Multiline = true;
            tb.Location = new Point(3, 3);

            lb.Location = new Point(583, 6);
            lb.Size = new System.Drawing.Size(105, 303);


            unread = 0;
            lb.Items.Clear();
            tp.SuspendLayout();
            tp.Controls.Add(tb);
            tp.Controls.Add(lb);
            tp.ResumeLayout();
        }

    }

    class FTClientCode
    {

        public static string curMsg = "Idle";

        public static void SendFile(string fileName, string ip)
        {
            try
            {
                //IPAddress ipAddress = IPAddress.Parse(((IPEndPoint)s.RemoteEndPoint).Address.ToString());
                //IPEndPoint ipEnd = new IPEndPoint(ipAddress, 2110);
                //Socket clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint ipEnd = new IPEndPoint(ipAddress, 2110);
                Socket clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);



                string filePath = "";

                fileName = fileName.Replace("\\", "/");
                while (fileName.IndexOf("/") > -1)
                {
                    filePath += fileName.Substring(0, fileName.IndexOf("/") + 1);
                    fileName = fileName.Substring(fileName.IndexOf("/") + 1);
                }


                byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);
                curMsg = "Buffering ...";
                byte[] fileData = File.ReadAllBytes(filePath + fileName);
                //chk filesize of filedata
                byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);



                fileNameLen.CopyTo(clientData, 0);
                fileNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileNameByte.Length);
                System.Threading.Thread.Sleep(1000);

                clientSock.Connect(ipEnd);

                clientSock.Send(clientData);

                clientSock.Close();
                MessageBox.Show("file sent to : " + ip);
                //again();
                //FTClientCode.SendFile(fDg.FileName);


            }
            catch (Exception ex)
            {
                if (ex.Message == "No connection could be made because the target machine actively refused it")
                    curMsg = "File Sending fail. Because server not running.";
                else
                    curMsg = "File Sending fail." + ex.Message;
                MessageBox.Show("ERROR DURING FILE : " + curMsg);
            }

        }
    }

    class FTServerCode
    {
        IPEndPoint ipEnd;
        Socket sock;
        public static bool on = false;
        public FTServerCode()
        {
            ipEnd = new IPEndPoint(IPAddress.Any, 2110);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            sock.Bind(ipEnd);
        }
        public static string receivedPath = "D:\\";
        public static string curMsg = "Stopped";

        public void StartServer()
        {
            try
            {
                curMsg = "Starting...";
                sock.Listen(1);
                curMsg = "Running and waiting to receive file.";
                Socket clientSock = sock.Accept();


                byte[] clientData = new byte[1024 * 5000];//1024*5000 for file+1024 byte(1kb) extra for name n all

                int receivedBytesLen = clientSock.Receive(clientData);
                curMsg = "Receiving data...";
                
                int fileNameLen = BitConverter.ToInt32(clientData, 0);
                string fileName = Encoding.ASCII.GetString(clientData, 4, fileNameLen);

                BinaryWriter bWrite = new BinaryWriter(File.Open(receivedPath + "/" + fileName, FileMode.Append)); ;
                bWrite.Write(clientData, 4 + fileNameLen, receivedBytesLen - 4 - fileNameLen);
                System.Threading.Thread.Sleep(1000);

                curMsg = "Saving file...";

                bWrite.Close();
                //clientSock.Close();
                curMsg = "Recieved & Saved file at "+receivedPath ;

            }
            catch (Exception ex)
            {
                curMsg = "File Receving error." + ex;
            }
            finally
            {
                MessageBox.Show(curMsg);
                StartServer();
            }
        }


    }


}