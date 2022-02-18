using System;
using System.Text;
using System.Threading;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Management;

namespace ServerSocketConnection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Server
        SocketPermission permission;
        Socket socketListener;
        IPEndPoint ipEP;
        Socket handler;

        private TextBox txtAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            txtAux.SelectionChanged += tbAux_SelectionChanged;

            btn_StartServer.IsEnabled = true;
            btnStartListening.IsEnabled = false;
            btnSend.IsEnabled = false;
            btn_CloseConextion.IsEnabled = false;
        }

        private void tbAux_SelectionChanged(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                txtReceived.Text = txtAux.Text;
            });
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Convert byte array to string
                string str = txtSend.Text;
                byte[] byteData = Encoding.Unicode.GetBytes(str);
                //Send data
                handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), handler);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void SendCallBack(IAsyncResult ar)
        {
            try
            {
                //Socket to send data
                Socket otherHandler = (Socket)ar.AsyncState;
                int bytesSended = otherHandler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to Client", bytesSended);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_StartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Create socket permissions
                permission = new SocketPermission(
                    NetworkAccess.Accept,       //Conextion Permission
                    TransportType.Tcp,          //Type of transport
                    "",                         //Obtains IP Address
                    SocketPermission.AllPorts   //All Ports
                    );

                //Socket Listener
                socketListener = null;
                //Ask permission
                permission.Demand();
                IPHostEntry host = Dns.GetHostEntry("");
                IPAddress ipAddress = host.AddressList[0];
                ipEP = new IPEndPoint(ipAddress, 11000);
                //Create a new socket listener
                socketListener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //Associate socket with IP End Point
                socketListener.Bind(ipEP);
                txtb_Status.Text = "Server On!";

                btn_StartServer.IsEnabled = false;
                btnStartListening.IsEnabled = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Put the socket to listen
                socketListener.Listen(10);
                //Start asynchronous operation to accept the call
                AsyncCallback aCallBack = new AsyncCallback(AcceptCallBack);
                socketListener.BeginAccept(aCallBack, socketListener);
                txtb_Status.Text = "Server listening at..." + ipEP.Address + " port: " + ipEP.Port;

                btnStartListening.IsEnabled = false;
                btnSend.IsEnabled = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void AcceptCallBack(IAsyncResult ar)
        {
            Socket listener = null;
            //New socket to manage communication with remote host
            Socket localHandler = null;
            try
            {
                //Receiving byte array
                byte[] buffer = new byte[1024];
                //Get socket from listener
                listener = (Socket)ar.AsyncState;
                localHandler = listener.EndAccept(ar);
                localHandler.NoDelay = false;
                //Creates an object array to pass data
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = localHandler;
                //Start data reception
                localHandler.BeginReceive(
                    buffer,
                    0,
                    buffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(ReceiveCallBack),
                    obj
                    );
                //Start Asynchronous operation to start the call
                AsyncCallback aCallBack = new AsyncCallback(AcceptCallBack);
                listener.BeginAccept(aCallBack, listener);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                //Create an object with infor
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;
                //Byte array to receive
                byte[] buffer = (byte[])obj[0];
                //Socket to handle the communication
                handler = (Socket)obj[1];
                //Received Message
                string msgReceived = string.Empty;
                int bytesReceived = handler.EndReceive(ar);
                if(bytesReceived > 0)
                {
                    msgReceived += Encoding.Unicode.GetString(buffer, 0, bytesReceived);
                    //If the message contains "@fin", end connection
                    if (msgReceived.IndexOf("@fin") > -1)
                    {
                        string str = msgReceived.Substring(0, msgReceived.LastIndexOf("@fin"));
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                        {
                            txtAux.Text = "Read " + str.Length * 2 + " client bytes. \n Data: " + str;
                        });
                    }
                    else
                    {
                        //Continue receiving data
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;
                        handler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), obj);
                    }
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate
                    {
                        txtAux.Text = msgReceived;
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_CloseConextion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (socketListener.Connected)
                {
                    socketListener.Shutdown(SocketShutdown.Both);
                    socketListener.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


    }
}
