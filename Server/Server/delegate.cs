using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 心跳机制
{
    // 客户端离线委托
    public delegate void ClientOfflineHander(string address,string port);

    // 客户端上限委托
    public delegate void ClientOnlineHandler(string address,string port);
}
