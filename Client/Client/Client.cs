using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace 心跳机制
{
    public class Client
    {
        #region Member

        private TcpClient m_client;
        private BinaryReader m_br;
        private BinaryWriter m_bw;

        #endregion

        #region 构造函数
        public Client(string ip, int port)
        {
            m_client = new TcpClient();
            m_client.BeginConnect(IPAddress.Parse(ip), port, new AsyncCallback(HandleTcpClientConnected), m_client);
        }

        #endregion

        #region Method
        private void HandleTcpClientConnected(IAsyncResult ar)
        {
            m_client.EndConnect(ar);
            NetworkStream stream = m_client.GetStream();
            m_br = new BinaryReader(stream);
            m_bw = new BinaryWriter(stream);

            m_bw.Write("请求服务!");
            m_bw.Flush();

            Thread receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        private void ReceiveData()
        {
            while (true)
            {
                if (m_client.Available != 0)
                {
                    Console.WriteLine("接收到的信息：{0}", m_br.ReadString());
                }
            }
        }
        #endregion
    }
}
