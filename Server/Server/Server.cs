using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.IO;

namespace 心跳机制
{
    public class Server
    {
        #region Member
        public event ClientOnlineHandler OnClientOnline;        // 上线事件，public 类型支持被订阅
        public event ClientOfflineHander OnClientOffline;       // 离线事件

        private TcpListener m_listener;                         // 服务器
        private bool isListening;                               // IF Listening,FALSE IS Listening,TRUE IS NOT Listening
        private List<ClientItem> m_clientList;                  // 客户端集合
        private Queue<ClientItem> m_removeQueue;                // 离线客户端队列

        #endregion

        #region 构造函数
        public Server()
        {
            isListening = true;   // 默认启动监听
            m_listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 500);
            m_clientList = new List<ClientItem>();
            m_removeQueue = new Queue<ClientItem>();
        }
        #endregion

        #region Method

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            m_listener.Start();

            // 启动服务端keep-alive
            m_listener.Server.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
            m_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            m_listener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), m_listener);

            Console.WriteLine("服务器已启动");
        }

        /// <summary>
        /// 监听新客户端回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void HandleTcpClientAccepted(IAsyncResult ar)
        {
            if (isListening)
            {
                TcpClient client = m_listener.EndAcceptTcpClient(ar);

                // 启动客户端keep-alive
                // 尝试是否是客户端和服务端任意一端设置就可以了
                // 经验证在服务端和客户端任一端设置后，均可实现心跳机制，此处在服务端设置
                //client.Client.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
                //client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                string clientPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

                if (OnClientOnline != null)  // 判断是否已被订阅
                    OnClientOnline(clientIP, clientPort);  // 触发上线事件 
             
                // 添加新客户端到客户端序列
                ClientItem item = new ClientItem(client, clientIP, clientPort);
                m_clientList.Add(item);

                Thread receiveThread = new Thread(ReceiveData);
                receiveThread.IsBackground = true;
                receiveThread.Start(item);

                m_listener.BeginAcceptTcpClient(HandleTcpClientAccepted,m_listener);   // 继续开始监听
            }
        }

        /// <summary>
        /// 接收客户端数据
        /// </summary>
        /// <param name="clientItem">客户端类</param>
        private void ReceiveData(object clientItem)
        {
            string receiveString = null;
            ClientItem clientItemObj = clientItem as ClientItem;
            TcpClient client = clientItemObj.Client;

            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            string clientPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

            NetworkStream stream = client.GetStream();
            BinaryReader br = new BinaryReader(stream);

            // 利用ClientItem.IsAvailable属性管理接收数据线程的关闭
            while (true)
            {
                lock (clientItemObj)
                {
                    if (!clientItemObj.IsAvailable)
                        break;

                    try
                    {
                        if (client.Available != 0)      // 属性Available 不能应用在失效的连接Client
                        {
                            receiveString = br.ReadString();
                            Console.WriteLine("接收到客户端:{0}的请求:{1}", clientIP + ":" + clientPort, receiveString);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("接收客户端信息出错：{0}", ex.Message);
                    }
                }

                Thread.Sleep(20);        // 休眠20ms，释放lock，将clientItemObj交给CheckAlive线程
            }
        }

        /*
         * keep-alive如果使用windows默认，2个小时发送一次心跳;
         * 读数频率以分钟作为单位的，设置的keep-alive每3秒发送一次;
         * 如果对方没有响应，每0.5秒后发送一次确认，如果连续3次没有回应，连接会自动变成Not TcpState.Established.
         * */
        /// <summary>
        /// IOControl设置
        /// </summary>
        /// <returns></returns>
        private byte[] GetKeepAliveData()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));  //keep-alive间隔
            BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);// 尝试间隔
            return inOptionValues;
        }

        /*
         * 再尝试一下若用poll来判断呢，有博客称反应不及时
         * */
        /// <summary>
        /// 检查客户端连接是否有效
        /// </summary>
        /// <returns>FALSE IF NOT CONNECTED ELSE TRUE</returns>
        public bool isClientConnected(TcpClient ClientSocket)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation c in tcpConnections)
            {
                TcpState stateOfConnection = c.State;

                if (c.LocalEndPoint.Equals(ClientSocket.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(ClientSocket.Client.RemoteEndPoint))
                {
                    if (stateOfConnection == TcpState.Established)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        // 开启监测
        public void StartCheckAlive()
        {
            Thread th = new Thread(new ThreadStart(CheckAlive));
            th.IsBackground = true;
            th.Start();
            Console.WriteLine("CheckAlive线程已启动");
        }

        // 检查连接是否有效
        private void CheckAlive()
        {
            Thread.Sleep(1000);
            while (isListening)
            {
                try
                {
                    lock (m_clientList)
                    {
                        foreach (ClientItem item in m_clientList)
                        {
                            //if (item.Client.Client.Poll(500, System.Net.Sockets.SelectMode.SelectRead) && (item.Client.Client.Available == 0))

                            if (!isClientConnected(item.Client))
                            {
                                m_removeQueue.Enqueue(item);
                                continue;
                            }
                        }
                        while (m_removeQueue.Count > 0)
                        {
                            ClientItem item = m_removeQueue.Dequeue();
                            m_clientList.Remove(item);
                            try
                            {
                                //Console.WriteLine("关闭客户端连接");
                                lock (item)
                                {
                                    item.Client.Close();           // 关闭该客户端
                                    item.IsAvailable = false;      // 告知其它线程此Client已失效
                                }  
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("关闭客户端连接失败", ex);
                            }
                            //Console.WriteLine("CheckAlive移除链接：" + item.Address + ":" + item.Port);

                            if (OnClientOffline != null)
                                OnClientOffline(item.Address,item.Port);    // 触发离线事件,向各订阅者发布离线消息
                        }//while
                    }//lock
                }
                catch (Exception e)
                {
                    Console.WriteLine("CheckAlive异常.", e);
                }

                Thread.Sleep(500);
            }// while
        }

        #endregion
    }
}
