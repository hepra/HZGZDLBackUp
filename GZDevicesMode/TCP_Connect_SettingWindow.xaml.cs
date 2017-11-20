using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using GZDL_DEV.DEL;
using Steema.TeeChart.WPF.Styles;
using Steema.TeeChart.WPF;
using System.Diagnostics;


namespace GZDevicesMode
{
    /// <summary>
    /// TCP_Connect_SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TCP_Connect_SettingWindow : Window
    {
        #region 变量
        public IPAddress aim_ip;
        private DispatcherTimer timer_connection = new DispatcherTimer();
        private DispatcherTimer timer_state = new DispatcherTimer();
        public Socket socket_Client;
        public Socket socket_Sever;
        public int port;
        Thread client_thread;
        public bool is_Sever_create_success = false;
        public bool is_Client_create_success = false;
        public delegate void recieve_message(byte[] data);
        public delegate void client_recieve_message(byte[] data);
        public delegate void UI_logic();
        public delegate void HandShake();
        public recieve_message Message_receive;
        public client_recieve_message Client_msg_rec;
        public UI_logic logic;
        public HandShake handshake;
        //服务器侦听对象  
        public TcpListener listener;
        string root_path = (System.AppDomain.CurrentDomain.BaseDirectory);
        //连接的客户端
        TcpClient send_message_client;
        public byte[] client_msg_rec = new byte[512];
        #endregion
        #region 构造函数
        public TCP_Connect_SettingWindow()
        {
            InitializeComponent();
            tbIP.Text = "192.168.201.227";
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                    if (AddressIP.Contains(".201."))
                    {
                        tbLocalIP.Text = AddressIP;
                        btnModifyIP.IsEnabled = false;
                        btnConnect.IsEnabled = true;
                    }
                }
            }
            if(tbLocalIP.Text == "")
            {
                btnModifyIP.IsEnabled = true;
                btnConnect.IsEnabled = false;
            }
            btnCancel.IsEnabled = false;
            timer_connection.Interval = TimeSpan.FromMilliseconds(2000);
            timer_connection.Tick += (EventHandler)delegate
            {
                if (!socket_Client.Connected)
                {
                    is_Client_create_success = false;
                    logic();
                    timer_connection.Stop();
                    MessageBox.Show(" 连接仪器失败!推荐解决方案:\r\n①  设置正确的IP地址\r\n②  确认网线正常连接", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (socket_Client.Connected)
                {
                    timer_connection.Stop();
                }
            };
            timer_state.Interval = TimeSpan.FromMilliseconds(1000);
            timer_state.Tag = 5;
            timer_state.Tick += (EventHandler)delegate
            {
                int flag = (int)timer_state.Tag;
                lbTCPSeverState.Content = "IP设置中...请等待("+flag+")";
                timer_state.Tag = flag - 1;
                if((int)timer_state.Tag == -1)
                {
                    timer_state.Stop();
                    timer_state.Tag = 5;
                    btnConnect_Click(this, null);
                }
            };
        }
        #endregion

        #region 局部函数
        //获取IP
        IPAddress get_ip_address(string ip_string)
        {
            try
            {
                return IPAddress.Parse(ip_string);
            }
            catch
            {
                MessageBox.Show("IP错误,请确认无误后重新连接!");
                return null;
            }

        }
        //获取端口号
        int get_port(string string_port)
        {
            try
            {
                return int.Parse(string_port); ;
            }
            catch
            {
                MessageBox.Show("端口错误,请输入正确端口号");
                return 0;
            }
        }
        #endregion

        #region 客户端发送 接受消息
        public int client_message_send(byte[] message)
        {
            try
            {
                socket_Client.Send(message);
            }
            catch
            {
                is_Client_create_success = false;
                logic();
                MessageBox.Show("抱歉,未检测到已连接仪器,无法执行此操作", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return -1;
            }
            return message.Length;
        }
        public byte[] client_message_receive()
        {
            byte[] data = new byte[1024];
            socket_Client.Receive(data);
            return data;
        }
        #endregion

        #region 服务器端发送 接受消息
        public int sever_message_send(byte[] message)
        {

            send_message_client.GetStream().Write(message, 0, message.Length);
            return message.Length;
        }
        #endregion

        #region UDP 服务器
        //定义UDPState类
        public class UdpState
        {
            public UdpClient udpClient;
            public IPEndPoint ipEndPoint;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
            public int counter = 0;
        }
        // 异步UDP类
        public class AsyncUdpSever
        {
            // 定义节点
            // public delegate void recieve_message(byte[] data);
            public recieve_message Message_receive;
            private IPEndPoint ipEndPoint = null;
            private IPEndPoint remoteEP = null;
            // 定义UDP发送和接收
            public UdpClient udpReceive = null;
            private UdpClient udpSend = null;
            // 定义端口
            UdpState udpReceiveState = null;
            UdpState udpSendState = null;
            // 异步状态同步
            private ManualResetEvent sendDone = new ManualResetEvent(false);
            private ManualResetEvent receiveDone = new ManualResetEvent(false);
            public AsyncUdpSever(string local_ip, int local_port, string remote_ip, int remote_port)
            {
                // 本机节点
                ipEndPoint = new IPEndPoint(IPAddress.Parse(local_ip), local_port);
                // 远程节点
                remoteEP = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], remote_port);
                // 实例化
                udpReceive = new UdpClient(ipEndPoint);
                // udpSend = new UdpClient();

                // 分别实例化udpSendState、udpReceiveState
                //接收
                udpReceiveState = new UdpState();
                udpReceiveState.udpClient = udpReceive;
                udpReceiveState.ipEndPoint = ipEndPoint;
                //发送
                //udpSendState = new UdpState();
                //udpSendState.udpClient = udpSend;
                //udpSendState.ipEndPoint = remoteEP;
            }

            #region 异步接收
            public void ReceiveMsg()
            {
                while (true)
                {
                    // 调用接收回调函数
                    Message_receive(udpReceive.Receive(ref udpReceiveState.ipEndPoint));
                }
            }
            // 接收回调函数
            private void ReceiveCallback(IAsyncResult iar)
            {
                UdpState udpReceiveState = iar.AsyncState as UdpState;
                if (iar.IsCompleted)
                {
                    Byte[] receiveBytes = udpReceiveState.udpClient.EndReceive(iar, ref udpReceiveState.ipEndPoint);
                }
            }

            #endregion

            #region 发送
            // 发送函数
            private void SendMsg()
            {
                udpSend.Connect(udpSendState.ipEndPoint);
                udpSendState.udpClient = udpSend;
                udpSendState.counter++;

                string message = string.Format("第{0}个UDP请求处理完成！", udpSendState.counter);
                Byte[] sendBytes = Encoding.Unicode.GetBytes(message);
                udpSend.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSendState);
                sendDone.WaitOne();
            }
            // 发送回调函数
            private void SendCallback(IAsyncResult iar)
            {
                UdpState udpState = iar.AsyncState as UdpState;
                Console.WriteLine("第{0}个请求处理完毕！", udpState.counter);
                Console.WriteLine("number of bytes sent: {0}", udpState.udpClient.EndSend(iar));
                sendDone.Set();
            }

            #endregion
        }
        //创建UDP服务器
        AsyncUdpSever aus;

        static CancellationTokenSource uploadCancellationTokenSource;
        public static void CreateUDPSever(string local_ip, int local_port, string remote_ip, int remote_port, recieve_message recieve)
        {
            AsyncUdpSever aus = new AsyncUdpSever(local_ip, local_port, remote_ip, remote_port);
            aus.Message_receive = recieve;
            Thread t = new Thread(new ThreadStart(aus.ReceiveMsg));
            t.Priority = ThreadPriority.Highest;
            t.IsBackground = true;
            t.Start();
        }
        //static Socket server;
        //public static void CreateTCPSever(string local_ip, int local_port, string remote_ip, int remote_port,recieve_message recieve)
        // {
        //     server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //     IPEndPoint iep = new IPEndPoint(IPAddress.Parse(local_ip), local_port);  
        //     server.Bind(iep);
        //     server.Listen(20);
        //     Thread tcpThread = new Thread(new ThreadStart(TcpListen));
        //     tcpThread.Start();  
        // }
        //static  Thread ClientThread; 
        public void Dispose()
        {
            aus.udpReceive.Close();
            Application.Current.Shutdown();
        }
        #endregion

        #region TCP服务器
        void creat_TCPSever(string local_ip, int local_port, string remote_ip, int remote_port, recieve_message recieve)
        {
            TCPHelper(local_ip, local_port);
        }
        public void TCPHelper(string ip, int port)
        {
            try
            {
                //创建一个新的Socket,这里我们使用最常用的基于TCP的Stream Socket（流式套接字）
                socket_Sever = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //将该socket绑定到主机上面的某个端口
                socket_Sever.Bind(new IPEndPoint(get_ip_address(ip), port));
                //启动监听，并且设置一个最大的队列长度
                socket_Sever.Listen(4);
                //开始接受客户端连接请求
                socket_Sever.BeginAccept(new AsyncCallback(ClientAccepted), socket_Sever);
            }
            catch (Exception error)
            {
                lbTCPSeverState.Content = "服务器开启失败";
                lbTCPSeverState.Foreground = Brushes.Red;
                MessageBox.Show("服务器开启失败:" + error.Message);
            }
        }
        public Socket client;
        private void ClientAccepted(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            client = socket.EndAccept(ar);
            is_end = false;
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), client);
            socket.BeginAccept(new AsyncCallback(ClientAccepted), socket_Sever);
        }
        static byte[] buffer = new byte[1212];
        public bool is_end = false;
        public void ReceiveMessage(IAsyncResult ar)
        {
            var socket = ar.AsyncState as Socket;
            var length = socket.EndReceive(ar);
            if (!is_end)
            {
                Message_receive(buffer);
                //接收下一个消息(因为这是一个递归的调用，所以这样就可以一直接收消息了）
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), socket);
            }
            else
            {
                client.Disconnect(false);
                socket.Close();
            }
        }
        #endregion

        #region 客户端
        //创建客户端
        public void create_TCPClient()
        {
            // 获得文本框中的IP对象；  
            aim_ip = get_ip_address(tbIP.Text.Trim());
            if (aim_ip == null)
            {
                return;
            }
            port = get_port(tbPort.Text.Trim());
            // 创建包含ip和端口号的网络节点对象；  
            IPEndPoint endPoint = new IPEndPoint(aim_ip, port);
            socket_Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.RemoteEndPoint = endPoint;
            e.UserToken = socket_Client;
            e.Completed += (EventHandler<SocketAsyncEventArgs>)delegate
            {
                if (socket_Client.Connected)
                {
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        handshake();
                        //MessageBox.Show("连接成功! ", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        is_Client_create_success = true;
                        btnCancel.IsEnabled = true;
                        btnConnect.IsEnabled = false;
                        logic();
                        client_thread = new Thread(RecMsg);
                        client_thread.IsBackground = true;
                        client_thread.Start();
                        this.Hide();
                    }));
                }
            };
            try
            {
                
                if(is_Sever_create_success)
                {
                    socket_Client.ConnectAsync(e);
                    timer_connection.Start();
                }
            }
            catch (Exception error)
            {
                is_Client_create_success = false;
                logic();
                return;
            }
        }
        //客户端消息接受
        void RecMsg()
        {
            while (true)
            {
                try
                {
                    socket_Client.Receive(client_msg_rec); // 接收数据，并返回数据的长度； 
                    Client_msg_rec(client_msg_rec);
                }
                catch (SocketException se)
                {
                    is_Client_create_success = false;
                    logic();
                    return;
                }
                catch (Exception e)
                {
                    is_Client_create_success = false;
                    logic();
                    return;
                }
            }
        }
        #endregion

        #region UI
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        public void Create_Sever()
        {
            string AddressIP = string.Empty;
            foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ipAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = ipAddress.ToString();
                    if (AddressIP.Contains(".201."))
                    {
                        tbLocalIP.Text = AddressIP;
                        btnModifyIP.IsEnabled = false;
                        btnConnect.IsEnabled = true;
                        break;
                    }
                }
            }
            //如果本地IP没有201网段 提醒用户 设置IP 或者点击一键修改IP按钮
            if (tbLocalIP.Text.Trim() == "")
            {
                MessageBox.Show("请点击[一键设置本地IP]修改IP再连接仪器!\r\n或者手动讲IP设置为:192.168.201.2到220 后启动软件");
                timer_connection.Stop();
                btnModifyIP.IsEnabled = true;
                btnConnect.IsEnabled = false;
                lbTCPSeverState.Content = "服务器开启失败";
                lbTCPSeverState.Foreground = Brushes.Red;
                tbLocalIP.IsEnabled = true;
                is_Sever_create_success = false;
                try
                {
                    this.Show();
                }
                catch
                {

                }
                return;
            }
            else
            {
                // CreateUDPSever(tbLocalIP.Text.Trim(), 13623, tbIP.Text.Trim(), get_port(tbPort.Text.Trim()), Message_receive);
                is_Sever_create_success = true;
                creat_TCPSever(tbLocalIP.Text.Trim(), 13623, tbIP.Text.Trim(), get_port(tbPort.Text.Trim()), Message_receive);
                lbTCPSeverState.Content = "TCP服务器开启成功";
                lbTCPSeverState.Foreground = Brushes.Green;
            }
        }
        public void Sever_Reastart()
        {
          
        }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!is_Sever_create_success)
            {
                Create_Sever();
            }
			create_TCPClient();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("是否要断开与目标服务器：" + socket_Client.RemoteEndPoint.ToString() + "连接", "提示", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                socket_Client.Close();
                btnConnect.IsEnabled = true;
                btnCancel.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();

        }
        #endregion
        private void btnModifyIP_Click(object sender, RoutedEventArgs e)
        {
          if( RunCmd2("1"))
          {
              btnModifyIP.IsEnabled = false;
              btnConnect.IsEnabled = true;
              lbTCPSeverState.Foreground = Brushes.Red;
              timer_state.Start();
          }
        }
        bool RunCmd2(string cmdStr)
        {
            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo.WorkingDirectory = root_path + "WordModel";
                proc.StartInfo.FileName = "ChangeIP.bat";
                //proc.StartInfo.Arguments = string.Format("10");//this is argument
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
               // proc.StandardInput.AutoFlush = true;
                proc.WaitForExit();
                proc.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
