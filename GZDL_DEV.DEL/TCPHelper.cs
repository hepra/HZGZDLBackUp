using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GZDL_DEV.DEL
{
   public class TCPHelper
    {
            //服务器侦听对象  
            public TcpListener listener;
            //客户端列表  
            public static TcpClient[] ClientList = new TcpClient[5];
            //已连接的客户端数  
            private int ClientsNum = 0;
            public delegate void MSGRecevie(NetworkStream iostream);
            public MSGRecevie msgrecvive;
            //构造方法  
            public TCPHelper(string ip, int port)
            {
                this.listener = new TcpListener(new IPEndPoint(IPAddress.Parse(ip), port));
                listener.Start();
            }
            //异步侦听  
            public void DoBeginAccept()
            {

                //开始从客户端监听连接  
                System.Windows.MessageBox.Show("服务器开启成功,等待连接...");
                //接收连接  
                //开始准备接入新的连接，一旦有新连接尝试则调用回调函数DoAcceptTcpCliet  
                listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClient), listener);

            }
            //处理客户端的连接  
            public void DoAcceptTcpClient(IAsyncResult iar)
            {
                //还原原始的TcpListner对象  
                this.listener = (TcpListener)iar.AsyncState;
                //完成连接的动作，并返回新的TcpClient  
                TcpClient client = this.listener.EndAcceptTcpClient(iar);
                System.Windows.MessageBox.Show(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString() + "连接成功");
                //更新链接入的客户端数量，将客户端保存至已连接客户端列表中  
                ClientList[ClientsNum++] = client;
                //获取输入输出流  
                NetworkStream iostream = client.GetStream();
                msgrecvive(iostream);
            }
        } 
}
