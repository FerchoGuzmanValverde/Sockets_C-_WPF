using System;
using System.Text;
using System.Windows;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.Json;

namespace CSharp_InterconnectionProccess_ClientServer_Sockets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Client
        //Array to receive bytes
        byte[] data = new byte[1024];
        Socket socketSend;
        Thread t1;
        byte flag = 0;

        public MainWindow()
        {
            InitializeComponent();
            btn_Send.IsEnabled = false;
            btn_Disconnect.IsEnabled = false;
            t1 = new Thread(thread);
            t1.Start();
        }

        private void thread()
        {
            while (true)
            {
                if(flag == 1)
                {
                    ReceiveDataFromServer();
                }
            }
        }

        private void ReceiveDataFromServer()
        {
            try
            {
                //Receive data from remote socket link
                int bytesReceived = socketSend.Receive(data);
                //Convert to string
                String msgReceived = Encoding.Unicode.GetString(data, 0, bytesReceived);
                //Read data while available
                while (socketSend.Available > 0)
                {
                    bytesReceived = socketSend.Receive(data);
                    msgReceived += Encoding.Unicode.GetString(data, 0, bytesReceived);
                }
                txtReceive.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render,
                    new Action(delegate ()
                    {
                        txtReceive.Text = "The server response: " + msgReceived;
                    }));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_ConnectServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Create socket permissions
                SocketPermission permission = new SocketPermission(
                    NetworkAccess.Connect,  //Conexion Permission
                    TransportType.Tcp,      //Type of conexion
                    "",                     //Get IP Address
                    SocketPermission.AllPorts //All ports
                    );

                //Makes sure the code have permission to access the socket
                permission.Demand();
                IPHostEntry host = Dns.GetHostEntry("");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint ipEP = new IPEndPoint(ipAddress, 11000);

                socketSend = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socketSend.NoDelay = false;
                socketSend.Connect(ipEP);
                txtb_Status.Text = "Socket connecting to..." + socketSend.RemoteEndPoint.ToString();
                btn_ConnectServer.IsEnabled = false;
                btn_Send.IsEnabled = true;
                btn_Disconnect.IsEnabled = false;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Disable socket
                socketSend.Shutdown(SocketShutdown.Both);
                socketSend.Close();
                btn_Disconnect.IsEnabled = false;
                btn_Send.IsEnabled = false;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Send a message
                string message = txt_Send.Text;
                byte[] msg = Encoding.Unicode.GetBytes(message);
                //We send data through the socket
                int bytesSent = socketSend.Send(msg);
                flag = 1;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
