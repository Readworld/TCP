using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace 心跳机制
{
    public class ClientItem
    {
        #region Member
        private static readonly Object objlock = typeof(ClientItem);
        #endregion

        #region 构造函数
        public ClientItem(TcpClient client, string ip, string port)
        {
            this.Address = ip;
            this.Port = port;
            this.Client = client;
            this.IsAvailable = true;
        }
        #endregion

        #region Property
        public string Address { get; set; }
        public string Port { get; set; }
        public bool IsAvailable { get; set; }
        public TcpClient Client { get; set; }
        #endregion
    }
}
