using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aoyi_TCPServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!DbManager.Connect("game", "127.0.0.1", 3306, "root", ""))//启动数据库，建立服务端电脑和数据库的连接
            {
                return;
            }
            NetManager.StartLoop(ServerConfig.TcpServerPort);
        }
    }
}
