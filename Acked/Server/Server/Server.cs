//Default
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//mine
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;


namespace Ackedserver
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }

        public string pass = "";



        class ClientInfo
        {
            public int id;
            public Socket socket;   //Socket of the client
            public string strName;  //Name by which the user logged into the chat room
            public bool busy;

            public ClientInfo()
            {
                id = 0;
                socket = null;
                strName = null;
                busy = false;
            }

            public ClientInfo(int a, Socket b, string c)
            {
                id = a;
                socket = b;
                strName = c;
                busy = false;
            }

            public bool available()
            {
                if (busy)
                    return false;
                else
                    return true;
            }

        }


        class grp
        {
            public int id;
            public List<ClientInfo> mem = new List<ClientInfo>();

            public string getlist()
            {
                string s = "L" + id.ToString() + " ";
                foreach (ClientInfo c in mem)
                {
                    s += c.id + " " + c.strName + "**";
                }
                return s;
            }
        }

        List<grp> ppl = new List<grp>();
        List<grp> vc = new List<grp>();

        //The collection of all clients logged into the room (an array of type ClientInfo)
        List<ClientInfo> clientList = new List<ClientInfo>();




        Socket ctcp;
        byte[] byteData = new byte[128];
        byte[] message;

        private void Server_Load(object sender, EventArgs e)
        {
            TextBox.CheckForIllegalCrossThreadCalls = false;
            textBox1.Text = "<<Server Started>> \r\nPassword: " + pass;
            try
            {
                //We are using TCP sockets
                ctcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //Assign the any IP of the machine and listen on port number 1100
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

                //Bind and listen on the given address
                ctcp.Bind(ipEndPoint);
                ctcp.Listen(4);

                //Accept the incoming clients
                ctcp.BeginAccept(new AsyncCallback(OnAccept), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Acked server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            backgroundWorker1.RunWorkerAsync();


        }

        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = ctcp.EndAccept(ar);

                //Start listening for more clients
                ctcp.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from her
                //byteData = new byte[128];

                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Acked server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }








        public bool txtrecv(string msgReceived, Socket clientSocket)
        {
            if (msgReceived[0] == 'A')
            {

                ClientInfo clientInfo = new ClientInfo();
                clientInfo.socket = clientSocket;
                clientInfo.strName = msgReceived.Substring(1);
                if (clientList.Count == 0)
                {
                    clientInfo.id = 1;
                }
                else
                {
                    clientInfo.id = clientList[clientList.Count - 1].id + 1;
                }
                clientList.Add(clientInfo);

                //Set the text of the message that we will broadcast to all users
                string s = "<<<" + msgReceived.Substring(1) + " has joined the room>>>";
                textBox1.Text += "\r\n" + s;
                sendtoall(s);
                //sendlist();
            }


            else if (msgReceived[0] == 'X')
            //x->remove client entirely
            //x12->remove client from group 12 only
            {
                if (msgReceived.Length == 1)
                {
                    int nIndex = 0;
                    foreach (ClientInfo client in clientList)
                    {
                        if (client.socket == clientSocket)
                        {
                            string s = "<<< " + client.strName + " Has left the room >>>";
                            textBox1.Text += "\r\n" + s;
                            clientList.RemoveAt(nIndex);
                            foreach (grp g in ppl)
                            {
                                if (g.mem.Contains(client))
                                {
                                    g.mem.Remove(client);
                                    sendgrplst(g.id);
                                }
                            }
                            sendtoall(s);
                            break;
                        }
                        ++nIndex;
                    }

                    clientSocket.Close();
                    sendlist();
                    return false;
                }
                else
                {
                    int n = Convert.ToInt32(msgReceived.Substring(1));
                    foreach (grp g in ppl)
                    {
                        if (g.id == n)
                        {
                            for (int i = g.mem.Count - 1; i >= 0; i--)
                            {
                                if (g.mem[i].socket == clientSocket)
                                {

                                    string s = "<<< " + g.mem[i].strName + " Has left group" + n + " >>>";
                                    textBox1.Text += "\r\n" + s;
                                    g.mem.Remove(g.mem[i]);
                                    break;
                                }
                            }

                            //not necessary
                            //string s = "C" + g.id.ToString();
                            //message = Encoding.ASCII.GetBytes(s);

                            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
                            ////till here

                            sendgrplst(g.id);
                            break;
                        }
                    }
                }
            }
            else if (msgReceived[0] == 'L')
            {
                sendlist();
            }
            else if (msgReceived[0] == 'M')
            {
                if (msgReceived[1] == 'A')
                {
                    sendtoall(msgReceived.Substring(2));
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
                    multigp(msgReceived, Convert.ToInt32(no));
                }
            }
            else if (msgReceived[0] == 'T') //create new group
            {

                grp tmp = new grp();
                if (ppl.Count == 0)
                    tmp.id = 1;
                else
                    tmp.id = ppl[ppl.Count - 1].id + 1;
                ClientInfo ci = new ClientInfo();


                msgReceived = msgReceived.Substring(1);
                string[] lis = msgReceived.Split('*');
                string no, nam;
                string sendmsg = "1T" + tmp.id + " " + msgReceived;
                message = Encoding.ASCII.GetBytes(sendmsg);
                foreach (ClientInfo client in clientList)
                {
                    if (client.socket == clientSocket)
                    {

                        ci.id = client.id;
                        ci.socket = clientSocket;
                        ci.strName = client.strName;
                        ci.busy = client.busy;
                        tmp.mem.Add(ci);
                        ppl.Add(tmp);
                        byte[] mssg = Encoding.ASCII.GetBytes("1O" + tmp.id);
                        client.socket.BeginSend(mssg, 0, mssg.Length, SocketFlags.None, new AsyncCallback(OnSend), client.socket);
                        break;

                        //coz i've got corresponding no n name for this loop,i wont be getting them again


                    }
                }
                foreach (string s in lis)
                {
                    if (s.Length > 0)
                    {
                        no = s.Split(' ')[0];
                        nam = s.Split(' ')[1];
                        foreach (ClientInfo client in clientList)
                        {

                            if ((client.id == Convert.ToInt32(no)) && (client.strName == nam))
                            {
                                client.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), client.socket);
                                break;
                            }
                        }

                    }

                }

            }

            else if (msgReceived[0] == 'R')
            {
                if (msgReceived[1] == 'Y')
                {
                    int no = Convert.ToInt32(msgReceived.Substring(2));
                    //clientsocket accepted
                    bool found = false;
                    foreach (ClientInfo client in clientList)
                    {
                        if (client.socket == clientSocket)
                        {
                            found = true;
                            ClientInfo ci = new ClientInfo();
                            ci.id = client.id;
                            ci.socket = clientSocket;
                            ci.strName = client.strName;
                            ci.busy = client.busy;
                            foreach (grp g in ppl)
                            {
                                if (g.id == no)
                                {
                                    g.mem.Add(ci);
                                    multigp(ci.strName + " has joined The group ", no); //send everyone thet new user has been added
                                    textBox1.Text += "\r\n" + ci.strName + " has joined The group " + no.ToString();
                                    sendgrplst(no); //update group list

                                    break;
                                }

                            }
                        }
                        if (found)
                            break;
                    }
                }
                else
                {
                    //clientsocket rejected
                    textBox1.Text += "\r\n" + "Rejected The group request";

                }

            }
            else if (msgReceived == "")
            {
                message = Encoding.ASCII.GetBytes("resend");
                textBox1.Text += "\r\n" + "broken message recieved..sending request to retransmit";

                clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
            }


            return true;
        }

        public void voicrecv(string msgReceived, Socket clientSocket)
        {


            //#region case of new client added
            //if (msgReceived[0] == 'a')
            //{
            //    ClientInfo clientInfo = new ClientInfo();
            //    if (alaw.Count > 0)
            //        clientInfo.id = ((ClientInfo)alaw[alaw.Count - 1]).id + 1;
            //    else
            //        clientInfo.id = 1;

            //    clientInfo.socket = clientSocket;
            //    clientInfo.strName = msgReceived.Substring(1);
            //    clientInfo.busy = false;

            //    alaw.Add(clientInfo);
            //    sendlist('a');//send connected valid clients to client


            //    //sending ip adress recorded to client
            //    string s = "I";
            //    message = Encoding.ASCII.GetBytes(s + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())));
            //    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);



            //    textBox1.Text += "\r\n<<<< " + clientInfo.strName + " CONNECTED with alaw codec >>>>";

            //}

            //else if (msgReceived[0] == 'u')
            //{
            //    ClientInfo clientInfo = new ClientInfo();
            //    if (ulaw.Count > 0)
            //        clientInfo.id = ((ClientInfo)ulaw[ulaw.Count - 1]).id + 1;
            //    else
            //        clientInfo.id = 1;
            //    clientInfo.socket = clientSocket;
            //    clientInfo.strName = msgReceived.Substring(1);
            //    clientInfo.busy = false;


            //    alaw.Add(clientInfo);
            //    sendlist('a');//send connected valid clients to client


            //    //sending its ip adress recorded to client
            //    string s = "I";
            //    message = Encoding.ASCII.GetBytes(s + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())));
            //    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

            //    textBox1.Text += "\r\n<<<< " + clientInfo.strName + " CONNECTED with ulaw codec >>>>";
            //}
            //#endregion



            //if (msgReceived[0] == 'X')
            //{
            //    List<ClientInfo> temp = new List<ClientInfo>();

            //    //message = Encoding.ASCII.GetBytes("test");
            //    //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

            //    char typ = msgReceived[1];
            //    temp = clientList;
            //    //Socket sok;
            //    int nIndex = 0;
            //    foreach (ClientInfo client in temp)
            //    {
            //        if (client.socket == clientSocket)
            //        {

            //            temp.RemoveAt(nIndex);
            //            textBox1.Text += "\r\n<<<< " + client.strName + " left with " + typ + "law codec >>>>";

            //            //sok.Close();
            //            break;
            //        }
            //        ++nIndex;
            //    }


            //message = Encoding.ASCII.GetBytes("test");
            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

            //clientSocket.Close();
            //    sendlist(typ);//send connected valid clients to client
            //    return;

            //}


            if (msgReceived[0] == 'V')
            {
                //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
                //sendlist('a');
                char typ = msgReceived[1];
                msgReceived = msgReceived.Substring(2);

                string id = msgReceived.Split(' ')[0];
                string name = msgReceived.Split(' ')[1];
                ClientInfo a = new ClientInfo();
                List<ClientInfo> temp = new List<ClientInfo>();
                temp = clientList;
                //message = Encoding.ASCII.GetBytes("test");

                //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                int i = 0;

                foreach (ClientInfo c in temp)
                {
                    if (c.socket == clientSocket)
                    {
                        //message = Encoding.ASCII.GetBytes("test");
                        //c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                        //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);


                        temp[i].busy = true;
                        a = c;

                        //message = Encoding.ASCII.GetBytes("test");
                        //c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);


                        break;
                    }
                    ++i;

                }

                textBox1.Text += "\r\n<<<< " + a.id + " busy >>>>";

                i = 0;
                foreach (ClientInfo c in temp)
                {
                    if ((Convert.ToInt32(id) == c.id) && (name == c.strName))
                    {
                        if (c.busy)
                        {
                            string str = "2B";
                            message = Encoding.ASCII.GetBytes(str);
                            clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                        }
                        else
                        {

                            temp[i].busy = true;
                            //Request+req to+ +req from ip + +req from id+ +req from name
                            string str = "2V" + (IPAddress.Parse(((IPEndPoint)c.socket.RemoteEndPoint).Address.ToString())) + " " + (IPAddress.Parse(((IPEndPoint)a.socket.RemoteEndPoint).Address.ToString())) + " " + a.id.ToString() + " " + a.strName;
                            message = Encoding.ASCII.GetBytes(str);

                            textBox1.Text += "\r\n<<<< " + temp[i].id + " busy >>>>";
                            c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            textBox1.Text += "\r\n<<<< sending request from frnd1 to frnd2 >>>>";
                            break;
                        }
                    }
                    ++i;
                    //extract from remoteendpoint . address from each socket n send to other
                    //talk over port 1200

                }
                //sendlist(typ);

            }

            else if (msgReceived[0] == 'Q')
            {
                char typ = msgReceived[1];

                List<ClientInfo> temp = new List<ClientInfo>();

                temp = clientList;

                msgReceived = msgReceived.Substring(2);
                string no = msgReceived.Split(' ')[0];
                string name = msgReceived.Split(' ')[1];
                int i = 0;
                //making clientsocket available
                foreach (ClientInfo c in temp)
                {
                    if (c.socket == clientSocket)
                    {
                        temp[i].busy = false;
                        break;
                    }
                    ++i;

                }

                i = 0;
                foreach (ClientInfo c in temp)
                {
                    if ((Convert.ToInt32(no) == c.id) && (name == c.strName))
                    {
                        temp[i].busy = false;
                        string str = "2Q" + name;
                        message = Encoding.ASCII.GetBytes(str);

                        c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                        textBox1.Text += "\r\n<<<< frnd on ip" + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())) + "choose to disconnect from " + (IPAddress.Parse(((IPEndPoint)temp[i].socket.RemoteEndPoint).Address.ToString())) + " making both available again >>>>";
                        break;
                    }
                    ++i;

                }

                //sendlist(typ);

            }

            else if (msgReceived[0] == 'R')
            {
                char typ;
                if (msgReceived[1] == 'Y')
                {

                    //accepted
                    typ = msgReceived[2];

                    List<ClientInfo> temp = new List<ClientInfo>();

                    temp = clientList;

                    msgReceived = msgReceived.Substring(3);
                    string no = msgReceived.Split(' ')[2];
                    string name = msgReceived.Split(' ')[3];
                    textBox1.Text += "\r\n<<<< reply frm frnd 2=yes,making connectn >>>>";

                    foreach (ClientInfo c in temp)
                    {
                        if ((Convert.ToInt32(no) == c.id) && (name == c.strName))
                        {
                            string s = "2RY" + msgReceived;
                            message = Encoding.ASCII.GetBytes(s);
                            c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            break;
                        }

                    }



                }
                else if (msgReceived[1] == 'N')
                {
                    typ = msgReceived[2];
                    textBox1.Text += "\r\n<<<< reply frm frnd 2=no,making frnd 1 n 2 available again >>>>";

                    List<ClientInfo> temp = new List<ClientInfo>();

                    temp = clientList;
                    msgReceived = msgReceived.Substring(3);

                    string no = msgReceived.Split(' ')[2];
                    string name = msgReceived.Split(' ')[3];
                    int k = 0;
                    for (var x = 0; x < temp.Count; x++)
                    {
                        if ((Convert.ToInt32(no) == temp[x].id) && (name == temp[x].strName))
                        {

                            temp[x].busy = false;
                            textBox1.Text += "\r\n<<<< " + temp[x].id + " available again >>>>";
                            k++;
                            string s = "2RN" + msgReceived;
                            message = Encoding.ASCII.GetBytes(s);
                            temp[x].socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), temp[x].socket);
                        }
                        else if (temp[x].socket == clientSocket)
                        {
                            temp[x].busy = false;
                            textBox1.Text += "\r\n<<<< " + temp[x].id + " available again >>>>";
                            k++;

                        }
                        if (k > 1)
                            break;
                    }

                    //sendlist(typ);
                    //refused
                }
                textBox1.Text += "\r\n<<<< passing reply to frnd1 >>>>";

            }

        }



        public void filsnd(string msgReceived, Socket clientSocket)
        {
            if (msgReceived[0] == 'N')
            {
                msgReceived = msgReceived.Substring(1);

                string[] x = msgReceived.Split('*');
                string filename = x[x.Length - 1].Substring(1);
                int i = 0;
                string no, nam, msg;
                msg = "4N";
                foreach (ClientInfo c in clientList)
                {
                    if (c.socket == clientSocket)
                    {
                        msg += c.id + " " + c.strName + "**";
                        break;
                    }

                }
                msg += ":" + filename;
                for (i = 0; i < x.Length - 1; i++)
                {
                    if (x[i].Length > 1)
                    {
                        no = x[i].Split(' ')[0];
                        nam = x[i].Split(' ')[1];
                        //int j = 0;
                        foreach (ClientInfo c in clientList)
                        {
                            if (c.id == Convert.ToInt32(no) && c.strName == nam)
                            {
                                message = Encoding.ASCII.GetBytes(msg);
                                c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                                break;
                            }
                        }
                    }
                }
            }
            else if (msgReceived[0] == 'R')
            {
                if (msgReceived[1] == 'Y')
                {
                    msgReceived = msgReceived.Substring(2);
                    string msg = "4S" + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString()));
                    string no, nam;
                    no = msgReceived.Split(' ')[0];
                    nam = msgReceived.Split(' ')[1];

                    foreach (ClientInfo c in clientList)
                    {
                        if (c.id == Convert.ToInt32(no) && c.strName == nam)
                        {
                            message = Encoding.ASCII.GetBytes(msg);
                            c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                            break;
                        }
                    }

                }
                else
                {

                }

            }
        }

        public void vidrecv(string msgReceived, Socket clientSocket)
        {
            if (msgReceived[0] == 'V')
            {

                msgReceived = msgReceived.Substring(1);
                grp tmp = new grp();

                if (vc.Count == 0)
                    tmp.id = 1;
                else
                    tmp.id = vc[vc.Count - 1].id + 1;
                ClientInfo ci = new ClientInfo();

                string snd = "3V" + tmp.id + " ";
                string msg = "";
                int i = 0;
                foreach (ClientInfo c in clientList)
                {
                    if (c.socket == clientSocket)
                    {
                        msg += c.id + " " + c.strName + "**";
                        clientList[i].busy = true;
                        ci.socket = c.socket;
                        ci.strName = c.strName;
                        ci.id = c.id;
                        ci.busy = c.busy;
                        tmp.mem.Add(ci);
                        vc.Add(tmp);
                        string rep = "3O";
                        message = Encoding.ASCII.GetBytes(rep);
                        clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                        break;
                    }
                    i++;
                }

                string[] x = msgReceived.Split('*');
                string no, nam;
                foreach (string s in x)
                {
                    if (s.Length > 1)
                    {
                        no = s.Split(' ')[0];
                        nam = s.Split(' ')[1];
                        //int j = 0;
                        foreach (ClientInfo c in clientList)
                        {
                            if (c.id == Convert.ToInt32(no) && c.strName == nam)
                            {
                                if (!c.busy)
                                {
                                    msg += c.id + " " + c.strName + "**";
                                    //clientList[i].busy = true;
                                    break;
                                }
                                else
                                {
                                    string rep = "I" + c.id + " " + c.strName + " Is Busy right now..Please try again later!! ";
                                    message = Encoding.ASCII.GetBytes(rep);
                                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);
                                    break;
                                }
                            }
                        }
                    }
                }

                x = null;
                x = msg.Split('*');
                message = Encoding.ASCII.GetBytes(snd + msg);
                foreach (string s in x)
                {
                    if (s.Length > 1)
                    {
                        no = s.Split(' ')[0];
                        nam = s.Split(' ')[1];
                        int j = 0;
                        foreach (ClientInfo c in clientList)
                        {
                            if (c.id == Convert.ToInt32(no) && c.strName == nam)
                            {
                                if (c.socket == clientSocket)
                                {
                                    break;
                                }
                                else
                                {

                                    c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                                    break;
                                }
                            }
                            j++;
                        }
                    }




                }

            }

            else if (msgReceived[0] == 'S')
            {
                int i = 0;
                foreach (ClientInfo c in clientList)
                {
                    if (c.socket == clientSocket)
                    {
                        clientList[i].busy = false;
                        break;
                    }
                    i++;
                }


            }

            else if (msgReceived[0] == 'R')
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


                if (msgReceived[0] == 'Y')
                {
                    string msg = "";
                    string all = "3N";
                    ClientInfo ci = new ClientInfo();
                    i = 0;
                    foreach (ClientInfo c in clientList)
                    {
                        if (c.socket == clientSocket)
                        {
                            msg = "3N" + c.strName + " " + (IPAddress.Parse(((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString())) + "**";
                            clientList[i].busy = true;
                            ci.socket = c.socket;
                            ci.strName = c.strName;
                            ci.id = c.id;
                            ci.busy = c.busy;
                            break;
                        }
                        i++;
                    }


                    message = Encoding.ASCII.GetBytes(msg);
                    foreach (grp g in vc)
                    {
                        if (g.id == Convert.ToInt32(no))
                        {
                            foreach (ClientInfo c in g.mem)
                            {
                                c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                                all += c.strName + " " + (IPAddress.Parse(((IPEndPoint)c.socket.RemoteEndPoint).Address.ToString())) + "**";
                            }
                            g.mem.Add(ci);

                            message = Encoding.ASCII.GetBytes(all);
                            ci.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), ci.socket);
                            break;
                        }
                    }
                }



                else
                {
                    string k = "";
                    foreach (ClientInfo c in clientList)
                    {
                        if (c.socket == clientSocket)
                        {
                            k = "Irequest to " + c.strName + " rejected";
                        }
                    }
                    if (k != "")
                    {
                        foreach (grp g in vc)
                        {
                            if (g.id == Convert.ToInt32(no))
                            {
                                message = Encoding.ASCII.GetBytes(k);
                                g.mem[0].socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), g.mem[0].socket);
                                break;
                            }


                        }
                    }
                }
            }


        }



        byte ini = new byte();
        private void OnReceive(IAsyncResult ar)
        {

            bool r = true;

            Socket clientSocket = (Socket)ar.AsyncState;
            try
            {
                clientSocket.EndReceive(ar);
            }
            catch { }
            string msgReceived = Encoding.ASCII.GetString(byteData);
            msgReceived = msgReceived.Substring(0, msgReceived.IndexOf('\0'));

            for (int i = 0; i < msgReceived.Length; i++)
            {
                byteData[i] = ini;
            }

            // byteData = new byte[128];


            //message = Encoding.ASCII.GetBytes("test");
            //clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);



            #region Data manupulation

            try
            {
                if (msgReceived == "")
                {
                    textBox1.Text += "\r\n<<<< empty msg,resending msg >>>>";
                    string s = "resend";
                    message = Encoding.ASCII.GetBytes(s);
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);


                }
                else if (msgReceived == "test")
                {
                    //resend coz last sending failed
                    textBox1.Text += "\r\n<<<< Test >>>>";
                    message = Encoding.ASCII.GetBytes("test");
                    clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), clientSocket);

                    //serversocket.BeginSend(b, 0, b.Length, SocketFlags.None, new AsyncCallback(OnSend), serversocket);
                }

                else if (msgReceived[0] == '1')
                {
                    r = txtrecv(msgReceived.Substring(1), clientSocket);
                }

                else if (msgReceived[0] == '2')
                {
                    voicrecv(msgReceived.Substring(1), clientSocket);
                }
                else if (msgReceived[0] == '3')
                {
                    vidrecv(msgReceived.Substring(1), clientSocket);
                }
                else if (msgReceived[0] == '4')
                {
                    filsnd(msgReceived.Substring(1), clientSocket);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Acked server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ex.Message == "An existing connection was forcibly closed by the remote host")
                    r = false;
            }


            #endregion
            //ankitbyteData = new byte[128];
            if (r)
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
        }


        void sendlist()
        {
            string s;
            foreach (ClientInfo j in clientList)
            {

                s = "1LA";
                foreach (ClientInfo k in clientList)
                {
                    if (k.socket != j.socket)
                        s += k.id + " " + k.strName + "**";
                }
                message = Encoding.ASCII.GetBytes(s);

                j.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), j.socket);
            }


        }


        void sendgrplst(int num)  //send list to group no. 'num'
        {
            string s;
            foreach (grp p in ppl)
            {
                if (p.id == num)
                {
                    s = p.getlist();
                    s = "1" + s;
                    message = Encoding.ASCII.GetBytes(s);
                    foreach (ClientInfo j in p.mem)
                    {
                        j.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), j.socket);
                    }

                    break;
                }
            }


        }
        void sendtoall(string s)
        {
            message = Encoding.ASCII.GetBytes("1MA" + s);
            foreach (ClientInfo c in clientList)
            {
                c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
            }


        }

        public void multigp(string str, int no)
        {
            message = Encoding.ASCII.GetBytes("1M" + no + " " + str);//identifier+grpno+ +message
            foreach (grp g in ppl)
            {
                if (g.id == no)
                {
                    foreach (ClientInfo c in g.mem)
                    {
                        c.socket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(OnSend), c.socket);
                    }
                }

            }

        }




        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Sending Data from server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();

        }

        UdpClient udp = new UdpClient();
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, 1150);

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                udp.EnableBroadcast = true;
                while (true)
                {
                    string str4 = pass;

                    byte[] sendBytes4 = Encoding.ASCII.GetBytes(str4);



                    udp.Send(sendBytes4, sendBytes4.Length, groupEP);
                    Thread.Sleep(1000); // One second.
                }
            }
            catch { }

        }

    }
}
