using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace 心跳机制
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client("127.0.0.1", 500);

            Console.ReadLine();
        }
    }
}
