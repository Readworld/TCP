using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 心跳机制
{
    class Program
    {
        /// <summary>
        /// 客户端离线提示
        /// </summary>
        /// <param name="clientInfo"></param>
        private static void ClientOffline(string clientIP,string clientPort)
        {
            Console.WriteLine(String.Format("客户端{0}离线，离线时间：\t{1}", clientIP + ":" + clientPort, DateTime.Now));
        }

        private static void ClientOnline(string clientIP,string clientPort)
        {
            Console.WriteLine(string.Format("客户端{0}上线，上线时间：\t{1}",clientIP + ":" + clientPort, DateTime.Now));
        }

        static void Main(string[] args)
        {
            Server server = new Server();

            // 订阅上线事件
            server.OnClientOnline += new ClientOnlineHandler(ClientOnline);

            // 订阅离线事件
            server.OnClientOffline += ClientOffline;

            // 开启服务器
            server.Start();

            // 开启心跳机制
            server.StartCheckAlive();

            Console.ReadLine();
        }
    }
}
